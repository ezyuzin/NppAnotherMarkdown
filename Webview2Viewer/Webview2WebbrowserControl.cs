using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using PanelCommon;
using Webview2Viewer.Web;

namespace Webview2Viewer
{
  public class Webview2WebbrowserControl : IWebbrowserControl, IDisposable
  {
    public EventHandler<DocumentContentChanged> DocumentChanged { get; set; }
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

    public Task InitializeAsync(int zoomLevel)
    {
      lock (_webViewInitLock) {
        if (_webViewInit == null) {
          _webViewInit = InitializeWebViewAsync(zoomLevel);
        }
      }
      return _webViewInit;
    }

    private async Task InitializeWebViewAsync(int zoomLevel)
    {
      var cacheDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), CONFIG_FOLDER_NAME, "webview2");
      
      var webView = new Microsoft.Web.WebView2.WinForms.WebView2();
      var opt = new CoreWebView2EnvironmentOptions();
      var webEnvironment = await CoreWebView2Environment.CreateAsync(null, cacheDir, opt);
      await webView.EnsureCoreWebView2Async(webEnvironment);

      webView.AccessibleName = "webView";
      webView.Name = "webView";
      webView.ZoomFactor = ConvertToZoomFactor(zoomLevel);
      webView.Source = new Uri("about:blank", UriKind.Absolute);
      webView.Location = new Point(1, 27);
      webView.Size = new Size(800, 424);
      webView.Dock = DockStyle.Fill;
      webView.TabIndex = 0;
      webView.NavigationStarting += OnWebBrowser_NavigationStarting;
      webView.ZoomFactor = ConvertToZoomFactor(zoomLevel);
      webView.CoreWebView2.WebResourceRequested += CoreWebView2_WebResourceRequested;

      _webView = webView;
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

    public async Task SetContent(string content, string documentPath, string assetsPath, string cssFile, bool syncView)
    {
      var fs = _webServices.OfType<LocalFileService>().FirstOrDefault();

      var reload = (fs == null);
      reload = reload || (fs.DocumentPath != documentPath);
      reload = reload || (_assetPath != assetsPath);
      reload = reload || (_cssFile != cssFile);
      reload = reload || (_syncView != syncView);

      if (_assetPath != assetsPath) {
        await ExecuteWebviewAction(() => {
          _webView.CoreWebView2.SetVirtualHostNameToFolderMapping("assets.example", assetsPath, CoreWebView2HostResourceAccessKind.Allow);
          return Task.CompletedTask;
        });
        _assetPath = assetsPath;
      }

      if (fs == null) {
        fs = new LocalFileService(_webView.CoreWebView2.Environment, "local.example");
        _webServices.Add(fs);
        fs.DocumentChanged += (e, args) => DocumentChanged?.Invoke(e, args);
        await ExecuteWebviewAction(() => {
          _webView.CoreWebView2.AddWebResourceRequestedFilter($"http://{fs.Hostname}/*", CoreWebView2WebResourceContext.All);
          return Task.CompletedTask;
        });
      }

      var baseDir = Path.GetDirectoryName(documentPath);
      var replaceFileMapping = "file://" + baseDir;
      content = content.Replace(replaceFileMapping, $"http://{fs.Hostname}");
      fs.SetContent(documentPath, content);

      if (reload) {
        _cssFile = cssFile;
        _syncView = syncView;

        var loader = File.ReadAllText(assetsPath + "/loader.html");
        cssFile = cssFile.Replace("\\", "/");
        assetsPath = assetsPath.Replace("\\", "/");

        if (cssFile.StartsWith(assetsPath + "/")) {
          cssFile = cssFile.Substring((assetsPath).Length + 1);
          loader = loader.Replace("http://assets.example/markdown/__CSS_NAME__", "http://assets.example/" + HttpUtility2.UrlPathEncode(cssFile));
        }
        else {
          loader = loader.Replace("http://assets.example/markdown/__CSS_NAME__", $"http://{fs.Hostname}/" + HttpUtility2.PathToUri(cssFile));
        }

        loader = loader.Replace("__BASE_URL__", HttpUtility2.PathToUri(baseDir));
        loader = loader.Replace("__CSS_NAME__", cssFile);
        loader = loader.Replace("__WITH_LINE_MARKER__", syncView ? "true" : "false");
        loader = loader.Replace("__MD_FILENAME__", fs.DocumentUri.Substring(1));

        await ExecuteWebviewAction(() => {
          _webView.NavigateToString(loader);
          return Task.CompletedTask;
        });
      }
      else {
        fs.SetContent(documentPath, content);
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
        return Task.CompletedTask;
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

    private bool _syncView;
    private List<IWebService> _webServices = new List<IWebService>();
  }
}
