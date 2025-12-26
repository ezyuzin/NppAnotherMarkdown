using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using PanelCommon;

namespace Webview2Viewer
{
  public class Webview2WebbrowserControl : IWebbrowserControl
  {
    public Action<string> StatusTextChangedAction { get; set; }
    public Action RenderingDoneAction { get; set; }

    public Webview2WebbrowserControl()
    {
      _webView = null;
    }

    public async void Initialize(int zoomLevel)
    {
      var cacheDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), CONFIG_FOLDER_NAME, "webview2");
      //var props = new Microsoft.Web.WebView2.WinForms.CoreWebView2CreationProperties();
      //props.UserDataFolder = cacheDir;
      //props.AdditionalBrowserArguments = "--disable-web-security --allow-file-access-from-files --allow-file-access";
      _webView = new Microsoft.Web.WebView2.WinForms.WebView2();
      var opt = new CoreWebView2EnvironmentOptions();
      _environment = await CoreWebView2Environment.CreateAsync(null, cacheDir, opt);
      await _webView.EnsureCoreWebView2Async(_environment);

      _webView.AccessibleName = "webView";
      _webView.Name = "webView";
      _webView.ZoomFactor = ConvertToZoomFactor(zoomLevel);
      _webView.Source = new Uri("about:blank", UriKind.Absolute);
      _webView.Location = new Point(1, 27);
      _webView.Size = new Size(800, 424);
      _webView.Dock = DockStyle.Fill;
      _webView.TabIndex = 0;
      _webView.NavigationStarting += OnWebBrowser_NavigationStarting;
      _webView.NavigationCompleted += WebView_NavigationCompleted;
      _webView.ZoomFactor = ConvertToZoomFactor(zoomLevel);

      _webViewInitialized = true;
    }

    public void AddToHost(Control host)
    {
      host.Controls.Add(_webView);
    }

    private void WebView_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
    {
    }

    /*public async Task SetScreenshot(PictureBox pictureBox)
    {
        pictureBox.Image = null;
        if (!webViewInitialized) return;
        var ms = new MemoryStream();
        await webView.CoreWebView2.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png, ms);
        var screenshot = new Bitmap(ms);
        pictureBox.Image = screenshot;
        pictureBox.Visible = true;
    }*/

    public Bitmap MakeScreenshot()
    {
      return null;
    }

    public void PrepareContentUpdate(bool preserveVerticalScrollPosition)
    {
      if (!_webViewInitialized) {
        return;
      }
    }

    public void ScrollToElementWithLineNo(int lineNo)
    {
      if (lineNo <= 0) {
        lineNo = 0;
      }
      ExecuteWebviewAction(new Action(async () => {
        var script = _scrollScript.Replace("__LINE__", $"LINE{lineNo}");
        await _webView.ExecuteScriptAsync(script);
      }));
    }

    private string UrlPathEncode(string path)
    {
      path = HttpUtility.UrlPathEncode(path);
      path = path.Replace("+", "%2B");
      return path;
    }

    private string ConvertPathToLocalUri(string path)
    {
      path = Path.GetFullPath(path).Replace("\\", "/");
      return UrlPathEncode(Regex.Replace(path, @"^(\w):\/", "disk$1/"));
    }

    public void SetContent(string content, string documentPath, string assetsPath, string cssFile, bool syncView)
    {
      if (!_webViewInitialized) {
        return;
      }

      var baseDir = Path.GetDirectoryName(documentPath);
      var replaceFileMapping = "file://" + baseDir;

      _content = Encoding.UTF8.GetBytes(content.Replace(replaceFileMapping, "http://local.example"));

      var reload = (_documentPath != documentPath);
      reload = reload || (_assetPath != assetsPath);
      reload = reload || (_cssFile != cssFile);
      reload = reload || (_syncView != syncView);

      if (reload) {
        _documentUri = "/" + ConvertPathToLocalUri(documentPath);
        _documentPath = documentPath;
        _cssFile = cssFile;
        _syncView = syncView;

        ExecuteWebviewAction(new Action(() => {
          if (_assetPath != assetsPath) {
            _webView.CoreWebView2.SetVirtualHostNameToFolderMapping("assets.example", assetsPath, CoreWebView2HostResourceAccessKind.Allow);
            _assetPath = assetsPath;
          }
          if (!_webResourceHandlerInitialized) {
            _webView.CoreWebView2.AddWebResourceRequestedFilter("http://local.example/*", CoreWebView2WebResourceContext.All);
            _webView.CoreWebView2.WebResourceRequested += WebResourceRequested;
            _webResourceHandlerInitialized = true;
          }
        }));

        ExecuteWebviewAction(new Action(() => {
          content = File.ReadAllText(assetsPath + "/markdown/loader.html");
          // compability with totalcomander markdown viewer plugin
          cssFile = cssFile.Replace("\\", "/");
          assetsPath = assetsPath.Replace("\\", "/");

          if (cssFile.StartsWith(assetsPath + "/")) {
            cssFile = cssFile.Substring((assetsPath).Length + 1);
            content = content.Replace("http://assets.example/markdown/__CSS_NAME__", "http://assets.example/" + UrlPathEncode(cssFile));
          }
          else {
            content = content.Replace("http://assets.example/markdown/__CSS_NAME__", "http://local.example/" + ConvertPathToLocalUri(cssFile));
          }
          content = content.Replace("__BASE_URL__", ConvertPathToLocalUri(baseDir));
          content = content.Replace("__CSS_NAME__", cssFile);
          content = content.Replace("__WITH_LINE_MARKER__", syncView ? "true" : "false");
          content = content.Replace("__MD_FILENAME__", _documentUri.Substring(1));
          _webView.NavigateToString(content);
        }));
      }
      else {
        ExecuteWebviewAction(new Action(async () => {
          await _webView.ExecuteScriptAsync("window.contentChanged();");
        }));
      }
    }

    private void WebResourceRequested(object sender, CoreWebView2WebResourceRequestedEventArgs e)
    {
      if (e.Response != null) {
        return;
      }
      var uri = new Uri(e.Request.Uri);
      var charset = "";
      if (uri.DnsSafeHost == "local.example") {
        byte[] bytes = null;
        if (_documentUri.Equals(uri.AbsolutePath, StringComparison.InvariantCultureIgnoreCase)) {
          bytes = _content;
          charset = "; charset=utf-8";
        }
        else {
          var path = HttpUtility.UrlDecode(uri.AbsolutePath);
          path = Regex.Replace(path, @"^\/disk(\w)\/", "$1:/");
          if (File.Exists(path)) {
            bytes = File.ReadAllBytes(path);
          }
        }

        if (bytes != null) {
          if (WebResource.MimeTypes.TryGetValue(Path.GetExtension(uri.AbsolutePath), out var contentType) == false) {
            contentType = "application/octet-stream";
          }
          var stream = new MemoryStream(bytes);
          e.Response = _webView.CoreWebView2.Environment.CreateWebResourceResponse(stream, 200, "OK",
            $"Content-Type: {contentType}{charset}\r\n" +
            $"Access-Control-Allow-Origin: *"
          );
          return;
        }
        else {
          var stream = new MemoryStream();
          e.Response = _webView.CoreWebView2.Environment.CreateWebResourceResponse(stream, 404, "NotFound", $"");
          return;
        }
      }
    }

    public void SetZoomLevel(int zoomLevel)
    {
      double zoomFactor = ConvertToZoomFactor(zoomLevel);
      ExecuteWebviewAction(new Action(() => {
        if (_webView.ZoomFactor != zoomFactor) {
          _webView.ZoomFactor = zoomFactor;
        }
      }));
    }

    private double ConvertToZoomFactor(int zoomLevel)
    {
      double zoomFactor = Convert.ToDouble(zoomLevel) / 100;
      return zoomFactor;
    }

    void OnWebBrowser_NavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
    {
      if (e.Uri.ToString().StartsWith("about:blank")) {
        e.Cancel = true;
      }
      else if (!e.Uri.ToString().StartsWith("data:")) {
        e.Cancel = true;
        var p = new Process();
        var navUri = new Uri(e.Uri);
        if (navUri.DnsSafeHost == "local.example") {
          return;
        }
        p.StartInfo = new ProcessStartInfo(e.Uri);
        p.Start();
      }
    }

    public string GetRenderingEngineName()
    {
      return "EDGE";
    }

    private void ExecuteWebviewAction(Action action)
    {
      try {
        _webView.Invoke(action);
      }
      catch (Exception ex) { }
    }

    const string CONFIG_FOLDER_NAME = "MarkdownPanel";
    const string _scrollScript = @"
var spacer = document.getElementById('spacer');
if (spacer) {
  spacer.parentElement.removeChild(spacer);
}

var element = document.getElementById('__LINE__');
var rect = element.getBoundingClientRect();
var elementTop = rect.top + window.pageYOffset;

var requiredScrollTop = elementTop;
var maxScrollTop = document.documentElement.scrollHeight - window.innerHeight;
if (requiredScrollTop > maxScrollTop) {
  var extraHeight = requiredScrollTop - maxScrollTop;
  var spacer = document.createElement('div');
  spacer.id = 'spacer';
  spacer.style.height = extraHeight + 'px';
  spacer.style.width = '1px';
  spacer.style.pointerEvents = 'none';

  document.body.appendChild(spacer);
}

window.scrollTo({
  top: requiredScrollTop,
  behavior: 'smooth'
});
";

    private Microsoft.Web.WebView2.WinForms.WebView2 _webView;
    private bool _webViewInitialized = false;
    private bool _webResourceHandlerInitialized = false;
    private byte[] _content;
    private string _cssFile;
    private string _assetPath;
    private string _documentPath;
    private string _documentUri;
    private bool _syncView;

    private CoreWebView2Environment _environment = null;
  }
}
