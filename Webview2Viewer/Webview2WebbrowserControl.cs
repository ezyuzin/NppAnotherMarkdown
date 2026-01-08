using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Newtonsoft.Json;
using PanelCommon;
using Webview2Viewer.Web;

namespace Webview2Viewer
{
  public class Webview2WebbrowserControl : IDisposable
  {
    public Action<string> StatusTextChangedAction { get; set; }
    public Action RenderingDoneAction { get; set; }

    public Webview2WebbrowserControl()
    {
      _webView = null;
    }

    public void Dispose()
    {
      _webView?.Dispose();
      _webView = null;
    }

    public Task InitializeAsync(ISettings settings, IEventDispatcher eventDispatcher)
    {
      lock (_webViewInitLock) {
        if (_webViewInit == null) {
          _settings = settings;
          _on = eventDispatcher;
          _webViewInit = InitializeWebViewAsync();
        }
      }
      return _webViewInit;
    }

    private async Task InitializeWebViewAsync()
    {
      var cacheDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), CONFIG_FOLDER_NAME, "webview2");

      var webView = new Microsoft.Web.WebView2.WinForms.WebView2();
      var opt = new CoreWebView2EnvironmentOptions();
      var webEnvironment = await CoreWebView2Environment.CreateAsync(null, cacheDir, opt);
      await webView.EnsureCoreWebView2Async(webEnvironment);

      webView.AccessibleName = "webView";
      webView.Name = "webView";
      webView.Source = new Uri("about:blank", UriKind.Absolute);
      webView.Location = new Point(1, 27);
      webView.Size = new Size(800, 424);
      webView.Dock = DockStyle.Fill;
      webView.TabIndex = 0;
      webView.NavigationStarting += OnWebBrowser_NavigationStarting;
      webView.ZoomFactor = ConvertToZoomFactor(_settings.ZoomLevel);
      webView.CoreWebView2.WebResourceRequested += CoreWebView2_WebResourceRequested;
      _webView = webView;

      var fs = new LocalFileService(webEnvironment, "local.example", _on);
      AddWebService(fs);

      var api = new ApiService(webEnvironment, "api.example", _on);
      AddWebService(api);
    }

    private void AddWebService(IWebService webService)
    {
      _webView.CoreWebView2.AddWebResourceRequestedFilter($"http://{webService.Hostname}/*", CoreWebView2WebResourceContext.All);
      _webServices.Add(webService);
    }

    private void CoreWebView2_WebResourceRequested(object sender, CoreWebView2WebResourceRequestedEventArgs e)
    {
      var uri = new Uri(e.Request.Uri);
      foreach(var webservice in _webServices) {
        if (webservice.Hostname == uri.DnsSafeHost) {
          if (webservice.HandleRequest(e)) {
            return;
          }
        }
      }
    }

    public void AddToHost(Control host)
    {
      host.Controls.Add(_webView);
    }

    public void ScrollToElementWithLineNo(int lineNo)
    {
      if (lineNo <= 0) {
        lineNo = 0;
      }
      ExecuteWebviewAction(() => _webView.ExecuteScriptAsync($"window.scrollToLine({lineNo})")).Wait();
    }

    public async Task SetContent(string content, string documentPath)
    {
      var fs = _webServices.OfType<LocalFileService>().First();

      var assetsPath = (!string.IsNullOrEmpty(_settings.AssetsPath) && Directory.Exists(_settings.AssetsPath))
        ? _settings.AssetsPath
        : _settings.DefaultAssetPath;

      var cssFile = _settings.IsDarkModeEnabled ? _settings.CssDarkModeFileName : _settings.CssFileName;
      if (!File.Exists(cssFile)) {
        cssFile = _settings.IsDarkModeEnabled ? _settings.DefaultDarkModeCssFile : _settings.DefaultCssFile;
      }
      var lineMark = (_settings.SyncViewWithFirstVisibleLine || _settings.SyncViewWithCaretPosition);


      var reload = (_documentPath != documentPath);
      reload = reload || (_assetPath != assetsPath);
      reload = reload || (_cssFile != cssFile);
      reload = reload || (_lineMark != lineMark);
      reload = reload || (_trackFirstLine != _settings.SyncViewWithFirstVisibleLine);

      if (_assetPath != assetsPath) {
        await ExecuteWebviewAction(() => {
          _webView.CoreWebView2.SetVirtualHostNameToFolderMapping("assets.example", assetsPath, CoreWebView2HostResourceAccessKind.Allow);
        });
        _assetPath = assetsPath;
      }

      var baseDir = Path.GetDirectoryName(documentPath);
      var replaceFileMapping = "file://" + baseDir;
      content = content.Replace(replaceFileMapping, $"http://{fs.Hostname}");
      fs.SetContent(documentPath, content);

      if (reload) {
        _documentPath = documentPath;
        _cssFile = cssFile;
        _lineMark = lineMark;
        _trackFirstLine = _settings.SyncViewWithFirstVisibleLine;

        var loader = File.ReadAllText(assetsPath + "/loader.html");
        cssFile = cssFile.Replace("\\", "/");
        assetsPath = assetsPath.Replace("\\", "/");

        if (cssFile.StartsWith(assetsPath + "/")) {
          cssFile = cssFile.Substring((assetsPath).Length + 1);
          cssFile = "http://assets.example/" + HttpUtility2.UrlPathEncode(cssFile);
        }
        else {
          cssFile = $"http://{fs.Hostname}/" + HttpUtility2.PathToUri(cssFile);
        }

        loader = loader.Replace("__BASE_URL__", HttpUtility2.PathToUri(baseDir));
        loader = loader.Replace("__OPTIONS__", JsonConvert.SerializeObject(new {
          document = "http://local.example" + fs.DocumentUri,
          css = cssFile,
          lineMark = (_settings.SyncViewWithFirstVisibleLine || _settings.SyncViewWithCaretPosition),
          trackFirstLine = _settings.SyncViewWithFirstVisibleLine,

        }));

        await ExecuteWebviewAction(() => _webView.NavigateToString(loader));
        await SetZoomLevel(_settings.ZoomLevel);
        
      }
      else {
        await ExecuteWebviewAction(() => _webView.ExecuteScriptAsync("window.contentChanged();"));
      }
    }

    public async Task SetZoomLevel(int zoomLevel)
    {
      double zoomFactor = ConvertToZoomFactor(zoomLevel);
      await ExecuteWebviewAction(() => {
        if (_webView.ZoomFactor != zoomFactor) {
          _webView.ZoomFactor = zoomFactor;
        }
      });
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

    private async Task ExecuteWebviewAction(Action action)
    {
      try {
        if (_webViewInit != null) {
          await _webViewInit;
          _webView.Invoke(action);
        }
      }
      catch (Exception) { }
    }

    private async Task ExecuteWebviewAction(Func<Task> action)
    {
      try {
        if (_webViewInit != null) {
          await _webViewInit;
          Task task = null;
          var asyncResult = _webView.BeginInvoke(new Action(() => {
            task = action();
          }));
          var _ = task.ContinueWith(t => _webView.EndInvoke(asyncResult));
        }
      }
      catch (Exception) { }
    }

    const string CONFIG_FOLDER_NAME = "AnotherMarkdown";

    private Microsoft.Web.WebView2.WinForms.WebView2 _webView;
    private object _webViewInitLock = new object();
    private Task _webViewInit;
    private string _cssFile;
    private string _assetPath;
    private ISettings _settings;

    private string _documentPath;
    private bool _lineMark;
    private bool _trackFirstLine;
    private List<IWebService> _webServices = new List<IWebService>();
    private IEventDispatcher _on;
  }
}
