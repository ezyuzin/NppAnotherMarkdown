using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using AnotherMarkdown.Entities;
using PanelCommon;
using Webview2Viewer;

namespace AnotherMarkdown.Forms
{
  public partial class MarkdownPreviewForm : Form, IViewerInterface
  {
    public EventHandler<DocumentContentChanged> OnDocumentContentChanged { get; set; }

    public static MarkdownPreviewForm InitViewer(Settings settings, ActionRef<Message> wndProcCallback)
    {
      return new MarkdownPreviewForm(settings, wndProcCallback);
    }

    private MarkdownPreviewForm(Settings settings, ActionRef<Message> wndProcCallback)
    {
      InitializeComponent();

      _defaultAssetsPath = (Path.GetDirectoryName(Assembly.GetAssembly(GetType()).Location) + "/assets").Replace("\\", "/");
      _wndProcCallback = wndProcCallback;
      _settings = settings;

      var webview = new Webview2WebbrowserControl();
      webview.DocumentChanged += (e, args) => {
        OnDocumentContentChanged?.Invoke(this, args);
      };
      webview.StatusTextChangedAction = (status) => {
        toolStripStatusLabel1.Text = status;
      };

      _webviewInitTask = webview
        .InitializeAsync(settings.ZoomLevel)
        .ContinueWith(t => {
          panel1.Controls.Clear();
          webview.AddToHost(panel1);
          panel1.Visible = true;
          _webView = webview;
        });
    }

    public void UpdateSettings(Settings settings)
    {
      _settings = settings;

      var isDarkModeEnabled = settings.IsDarkModeEnabled;
      if (isDarkModeEnabled) {
        tbPreview.BackColor = Color.Black;
        statusStrip2.BackColor = Color.Black;
        toolStripStatusLabel1.ForeColor = Color.White;
      }
      else {
        tbPreview.BackColor = SystemColors.Control;
        statusStrip2.BackColor = SystemColors.Control;
        toolStripStatusLabel1.ForeColor = SystemColors.ControlText;
      }

      tbPreview.Visible = settings.ShowToolbar;
      statusStrip2.Visible = settings.ShowStatusbar;
    }


    public void RenderMarkdown(string currentText, string filepath)
    {
      if ((_renderTask == null || _renderTask.IsCompleted)) {
        var cssFile = _settings.IsDarkModeEnabled ? _settings.CssDarkModeFileName : _settings.CssFileName;
        if (!File.Exists(cssFile)) {
          cssFile = _settings.IsDarkModeEnabled ? Settings.DefaultDarkModeCssFile : Settings.DefaultCssFile;
        }

        var context = TaskScheduler.FromCurrentSynchronizationContext();
        _renderTask = new Task(async () => {
          try {
            var assetsPath = (!string.IsNullOrEmpty(_settings.AssetsPath) && Directory.Exists(_settings.AssetsPath))
              ? _settings.AssetsPath
              : _defaultAssetsPath;

            var withSyncView = (_settings.SyncViewWithCaretPosition || _settings.SyncViewWithFirstVisibleLine);

            await _webviewInitTask;
            await _webView.SetContent(currentText, filepath, assetsPath, cssFile, withSyncView);
            await _webView.SetZoomLevel(_settings.ZoomLevel);
          }
          catch (Exception) { }
        });
        _renderTask.Start(context);
      }
    }

    public void ScrollToElementWithLineNo(int lineNo)
    {
      if (_webView != null) {
        _webView.ScrollToElementWithLineNo((int) lineNo);
      }
    }

    protected override void WndProc(ref Message m)
    {
      _wndProcCallback(ref m);
      base.WndProc(ref m);
    }

    public bool IsValidFileExtension(string filename)
    {
      if (_settings.AllowAllExtensions) {
        return true;
      }

      var currentExtension = Path.GetExtension(filename).ToLower();
      var matchExtensionList = false;
      try {
        matchExtensionList = _settings.SupportedFileExt.Split(',').Any(ext => ext != null && currentExtension.Equals("." + ext.Trim().ToLower()));
      }
      catch (Exception) { }

      return matchExtensionList;
    }

    private Task _webviewInitTask;
    private Task _renderTask;
    private Settings _settings;
    private Webview2WebbrowserControl _webView;
    private ActionRef<Message> _wndProcCallback;
    private string _defaultAssetsPath;
  }
}
