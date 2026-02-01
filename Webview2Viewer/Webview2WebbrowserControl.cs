using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PanelCommon;
using Webview2Viewer.Web;
using WebView2 = Microsoft.Web.WebView2.WinForms.WebView2;

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

    public void Initialize(ISettings settings, IEventDispatcher eventDispatcher)
    {
      lock (_webViewInitLock) {
        if (_webView == null) {
          _settings = settings;
          _on = eventDispatcher;
          _webView = InitializeWebViewAsync();
        }
      }
    }

    private async Task<WebView2> InitializeWebViewAsync()
    {
      var cacheDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), CONFIG_FOLDER_NAME, "webview2");

      var webView = new WebView2();
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

      var fs = new LocalFileService(webEnvironment, "local.example", _on);
      AddWebService(webView, fs);

      var api = new ApiService(webEnvironment, "api.example", _on);
      AddWebService(webView, api);
      return webView;
    }

    private void AddWebService(WebView2 webView, IWebService webService)
    {
      webView.CoreWebView2.AddWebResourceRequestedFilter($"http://{webService.Hostname}/*", CoreWebView2WebResourceContext.All);
      _webServices.Add(webService);
    }

    private void CoreWebView2_WebResourceRequested(object sender, CoreWebView2WebResourceRequestedEventArgs e)
    {
      var uri = new Uri(e.Request.Uri);
      foreach (var webservice in _webServices) {
        if (webservice.Hostname == uri.DnsSafeHost) {
          if (webservice.HandleRequest(e)) {
            return;
          }
        }
      }
    }

    public void AddToHost(Control host)
    {
      ExecuteWebviewAction((webView) => {
        host.Controls.Add(webView);
      });
    }

    public async Task ScrollToElementWithLineNo(int lineNo)
    {
      if (lineNo <= 0) {
        lineNo = 0;
      }
      await ExecuteWebviewActionAsync((webView) => webView.ExecuteScriptAsync($"window.scrollToLine({lineNo})"));
    }

    public async Task SetContentAsync(string content, string documentPath)
    {
      await _webView;
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
      reload = reload || (_enabledMarkdownPlugins != string.Join(",", _settings.EnabledMarkdownPlugins));

      if (_assetPath != assetsPath) {
        await ExecuteWebviewActionAsync((webView) => {
          webView.CoreWebView2.SetVirtualHostNameToFolderMapping("assets.example", assetsPath, CoreWebView2HostResourceAccessKind.Allow);
        });
        _assetPath = assetsPath;
      }

      var baseDir = Path.GetDirectoryName(documentPath);
      var replaceFileMapping = "file://" + baseDir;
      content = content.Replace(replaceFileMapping, $"http://{fs.Hostname}");
      fs.SetContent(documentPath, content);

      if (!reload) {
        await ExecuteWebviewActionAsync((webView) => webView.ExecuteScriptAsync("window.contentChanged();"));
        return;
      }

      if (!string.IsNullOrEmpty(_documentPath) && _documentPath != documentPath) {
        await ExecuteWebviewActionAsync(async (webView) => {
          var value = await webView.ExecuteScriptAsync("window.pageYOffset");
          _preservePosition[_documentPath] = (int) double.Parse(value, CultureInfo.InvariantCulture);
        });
      }

      _documentPath = documentPath;
      _cssFile = cssFile;
      _lineMark = lineMark;
      _trackFirstLine = _settings.SyncViewWithFirstVisibleLine;
      _enabledMarkdownPlugins = string.Join(",", _settings.EnabledMarkdownPlugins);

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
      var options = new JObject {
        ["document"] = "http://local.example" + fs.DocumentUri
      };

      if (documentPath.EndsWith(".md")) {
        options["css"] = cssFile;
        options["lineMark"] = (_settings.SyncViewWithFirstVisibleLine || _settings.SyncViewWithCaretPosition);
        options["trackFirstLine"] = _settings.SyncViewWithFirstVisibleLine;
        if (_preservePosition.TryGetValue(_documentPath, out var pageYOffset)) {
          options["pageYOffset"] = pageYOffset;
        }
        options["md.extensions"] = JToken.FromObject(_settings.EnabledMarkdownPlugins);
      }

      loader = loader.Replace("__OPTIONS__", JsonConvert.SerializeObject(options));

      await ExecuteWebviewActionAsync((webView) => webView.NavigateToString(loader));
      await SetZoomLevel(_settings.ZoomLevel);
    }

    public async Task SetZoomLevel(int zoomLevel)
    {
      double zoomFactor = ConvertToZoomFactor(zoomLevel);
      await ExecuteWebviewActionAsync((webView) => {
        if (webView.ZoomFactor != zoomFactor) {
          webView.ZoomFactor = zoomFactor;
        }
      });
    }

    private double ConvertToZoomFactor(int zoomLevel) => Convert.ToDouble(zoomLevel) / 100;

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
          if (_on.Navigate != null && navUri.AbsolutePath.EndsWith(".md")) {
            var path = HttpUtility2.UriToPath(navUri.AbsolutePath);
            if (File.Exists(path)) {
              _on.Navigate(this, new NavigateToEvent { Filename = path });
            }
          }
          return;
        }
        p.StartInfo = new ProcessStartInfo(e.Uri);
        p.Start();
      }
    }

    private void ExecuteWebviewAction(Func<WebView2, Task> action)
    {
      var asyncTask = new Task(async () => {
        await ExecuteWebviewActionAsync(action);
      });

      asyncTask.Start(TaskScheduler.FromCurrentSynchronizationContext());
      asyncTask.Wait();
    }

    private void ExecuteWebviewAction(Action<WebView2> action)
    {
      var asyncTask = new Task(async () => {
        await ExecuteWebviewActionAsync(action);
      });

      asyncTask.Start(TaskScheduler.FromCurrentSynchronizationContext());
      asyncTask.Wait();
    }

    private async Task ExecuteWebviewActionAsync(Action<WebView2> action)
    {
      try {
        if (_webView != null) {
          var webView = await _webView;
          webView.Invoke(new Action(() => action(webView)));
        }
      }
      catch (Exception) { }
    }

    private async Task ExecuteWebviewActionAsync(Func<WebView2, Task> action)
    {
      try {
        if (_webView != null) {
          var webView = await _webView;
          var tcs = new TaskCompletionSource<bool>();
          var asyncResult = webView.BeginInvoke(new Action(() => {
            try {
              var task = action(webView);
              task.ContinueWith(t => {
                tcs.SetResult(true);
              });
            }
            catch (Exception ex) {
              tcs.SetException(ex);
            }
          }));
          await tcs.Task;
          webView.EndInvoke(asyncResult);
        }
      }
      catch (Exception) { }
    }

    const string CONFIG_FOLDER_NAME = "AnotherMarkdown";

    private Task<WebView2> _webView;
    private object _webViewInitLock = new object();
    private string _cssFile;
    private string _assetPath;
    private ISettings _settings;

    private string _documentPath;
    private bool _lineMark;
    private bool _trackFirstLine;
    private string _enabledMarkdownPlugins;

    private Dictionary<string, double> _preservePosition = new Dictionary<string, double>();
    private List<IWebService> _webServices = new List<IWebService>();
    private IEventDispatcher _on;
  }
}
