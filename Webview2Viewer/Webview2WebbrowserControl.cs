using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Newtonsoft.Json;
using PanelCommon;

namespace Webview2Viewer
{
  public class Webview2WebbrowserControl : IWebbrowserControl
  {
    public EventHandler<DocumentContentChanged> DocumentChanged { get; set; }
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

    private void WebView_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e) { }
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

    private string UriToPath(string path)
    {
      if (path == string.Empty) {
        return path;
      }
      path = HttpUtility.UrlDecode(path);
      return Regex.Replace(path, @"^\/disk(\w)\/", "$1:/");
    }

    private string PathToUri(string path)
    {
      if (path == string.Empty) {
        return path;
      }
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
        _documentUri = "/" + PathToUri(documentPath);
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
          content = File.ReadAllText(assetsPath + "/loader.html");
          // compability with totalcomander markdown viewer plugin
          cssFile = cssFile.Replace("\\", "/");
          assetsPath = assetsPath.Replace("\\", "/");

          if (cssFile.StartsWith(assetsPath + "/")) {
            cssFile = cssFile.Substring((assetsPath).Length + 1);
            content = content.Replace("http://assets.example/markdown/__CSS_NAME__", "http://assets.example/" + UrlPathEncode(cssFile));
          }
          else {
            content = content.Replace("http://assets.example/markdown/__CSS_NAME__", "http://local.example/" + PathToUri(cssFile));
          }
          content = content.Replace("__BASE_URL__", PathToUri(baseDir));
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
      if (e.Request.Method == "GET") {
        var uri = new Uri(e.Request.Uri);
        if (uri.DnsSafeHost == "local.example") {
          HttpGetContent(e, uri);
        }
      }
      else if (e.Request.Method == "PUT") {
        var uri = new Uri(e.Request.Uri);
        if (uri.DnsSafeHost == "local.example") {
          HttpPutContent(e, uri);
        }
      }
      else if (e.Request.Method == "OPTIONS") {
        var uri = new Uri(e.Request.Uri);
        if (uri.DnsSafeHost == "local.example") {
          HttpOptionsContent(e, uri);
        }
      }
    }

    private void HttpPutContent(CoreWebView2WebResourceRequestedEventArgs e, Uri requestUri)
    {
      if (!_documentUri.Equals(requestUri.AbsolutePath, StringComparison.InvariantCultureIgnoreCase)) {
        Error404(e);
        return;
      }

      var headers = new List<string> {
        "Access-Control-Allow-Origin: *"
      };

      string requestBody;
      using (var reader = new StreamReader(e.Request.Content, Encoding.UTF8)) {
        requestBody = reader.ReadToEnd();
      }

      e.Response = _webView.CoreWebView2
        .Environment
        .CreateWebResourceResponse(new MemoryStream(), 200, "OK", string.Join("\r\n", headers));

      new Task(() => DocumentChanged?.Invoke(this, new DocumentContentChanged {
        Content = requestBody
      })).Start();
    }

    private void HttpGetContent(CoreWebView2WebResourceRequestedEventArgs e, Uri requestUri)
    {
      var charset = "";
      byte[] bytes = null;
      var headers = new List<string> {
        "Access-Control-Allow-Origin: *"
      };

      if (Regex.IsMatch(requestUri.AbsolutePath, @"\*\.(\*|.+)$")) {
        var path = UriToPath(requestUri.AbsolutePath);
        var mask = Path.GetFileName(path);

        var dir = Path.GetDirectoryName(path);
        if (Directory.Exists(dir)) {
          var files = Directory.GetFiles(dir, mask, SearchOption.TopDirectoryOnly)
            .Select(li => new { name = Path.GetFileName(li), type = "file" })
            .ToArray();

          Json(e, headers, files);
          return;
        }
        Error404(e);
        return;
      }

      if (_documentUri.Equals(requestUri.AbsolutePath, StringComparison.InvariantCultureIgnoreCase)) {
        bytes = _content;
        charset = "; charset=utf-8";
      }
      else {
        var path = UriToPath(requestUri.AbsolutePath);
        if (File.Exists(path)) {
          bytes = File.ReadAllBytes(path);
        }
      }

      if (bytes == null) {
        Error404(e);
        return;
      }

      if (WebResource.MimeTypes.TryGetValue(Path.GetExtension(requestUri.AbsolutePath), out var contentType)) {
        headers.Add($"Content-Type: {contentType}{charset}");
      }
      else {
        headers.Add($"Content-Type: application/octet-stream");
      }

      e.Response = _webView.CoreWebView2
        .Environment
        .CreateWebResourceResponse(new MemoryStream(bytes), 200, "OK", string.Join("\r\n", headers));
    }

    private void Json<TModel>(CoreWebView2WebResourceRequestedEventArgs e, List<string> headers, TModel data)
    {
      headers.Add("Content-Type: application/json; charset=utf-8");
      e.Response = _webView.CoreWebView2
        .Environment
        .CreateWebResourceResponse(ToJson(data), 200, "OK", string.Join("\r\n", headers));
    }

    private static MemoryStream ToJson<TData>(TData files, MemoryStream stream = null)
    {
      if (stream == null) {
        stream = new MemoryStream();
      }
      var serializer = new JsonSerializer();
      using (var writter = new StreamWriter(stream, Encoding.UTF8, 2048, true)) {
        serializer.Serialize(writter, files);
        writter.Flush();
      }
      return stream;
    }

    private void Error404(CoreWebView2WebResourceRequestedEventArgs e)
    {
      e.Response = _webView.CoreWebView2.Environment.CreateWebResourceResponse(new MemoryStream(), 404, "NotFound", $"");
    }

    private void HttpOptionsContent(CoreWebView2WebResourceRequestedEventArgs e, Uri uri)
    {
      var charset = "";
      var headers = new List<string> {
        "Access-Control-Allow-Origin: *",
        "Access-Control-Allow-Headers: *"
      };

      if (_documentUri.Equals(uri.AbsolutePath, StringComparison.InvariantCultureIgnoreCase)) {
        charset = "; charset=utf-8";
        headers.Add("Access-Control-Allow-Methods: GET, PUT, OPTIONS");
      }
      else {
        var path = HttpUtility.UrlDecode(uri.AbsolutePath);
        path = Regex.Replace(path, @"^\/disk(\w)\/", "$1:/");
        if (!File.Exists(path)) {
          Error404(e);
          return;
        }
        headers.Add("Access-Control-Allow-Methods: GET, OPTIONS");
      }

      if (WebResource.MimeTypes.TryGetValue(Path.GetExtension(uri.AbsolutePath), out var contentType) == false) {
        contentType = "application/octet-stream";
      }
      headers.Add($"Content-Type: {contentType}{charset}");

      e.Response = _webView.CoreWebView2.Environment
        .CreateWebResourceResponse(new MemoryStream(), 204, "OK", string.Join("\r\n", headers));
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
      catch (Exception) { }
    }

    const string CONFIG_FOLDER_NAME = "MarkdownPanel";
    const string _scrollScript = @"
var element = document.getElementById('__LINE__');
if (!element) {
  return;
}
var spacer = document.getElementById('spacer');
if (spacer) {
  spacer.parentElement.removeChild(spacer);
}
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
