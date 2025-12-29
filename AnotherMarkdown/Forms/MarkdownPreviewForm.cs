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

    private MarkdownPreviewForm(Settings settings, ActionRef<Message> wndProcCallback)
    {
      InitializeComponent();

      _defaultAssetsPath = (Path.GetDirectoryName(Assembly.GetAssembly(GetType()).Location) + "/assets").Replace("\\", "/");
      _wndProcCallback = wndProcCallback;
      _settings = settings;
      panel1.Visible = true;

      InitRenderingEngine(settings);
    }

    public void UpdateSettings(Settings settings)
    {
      _settings = settings;

      var isDarkModeEnabled = settings.IsDarkModeEnabled;
      if (isDarkModeEnabled) {
        tbPreview.BackColor = Color.Black;
        btnSaveHtml.ForeColor = Color.White;
        statusStrip2.BackColor = Color.Black;
        toolStripStatusLabel1.ForeColor = Color.White;
      }
      else {
        tbPreview.BackColor = SystemColors.Control;
        btnSaveHtml.ForeColor = SystemColors.ControlText;
        statusStrip2.BackColor = SystemColors.Control;
        toolStripStatusLabel1.ForeColor = SystemColors.ControlText;
      }

      tbPreview.Visible = settings.ShowToolbar;
      statusStrip2.Visible = settings.ShowStatusbar;

      if (webbrowserControl != null) {
        if (webbrowserControl.GetRenderingEngineName() != settings.RenderingEngine) {
          InitRenderingEngine(settings);
        }
      }
    }

    public static IViewerInterface InitViewer(Settings settings, ActionRef<Message> wndProcCallback)
    {
      return new MarkdownPreviewForm(settings, wndProcCallback);
    }

    private void InitRenderingEngine(Settings settings)
    {
      panel1.Controls.Clear();

      if (webview2Instance == null) {
        webbrowserControl = new Webview2WebbrowserControl();
        webbrowserControl.Initialize(settings.ZoomLevel);
        webbrowserControl.DocumentChanged += (e, args) => {
          OnDocumentContentChanged?.Invoke(this, args);
        };

        webview2Instance = webbrowserControl;
      }
      else {
        webbrowserControl = webview2Instance;
      }

      webbrowserControl.AddToHost(panel1);
      webbrowserControl.RenderingDoneAction = () => {
        HideScreenshotAndShowBrowser();
      };
      webbrowserControl.StatusTextChangedAction = (status) => {
        toolStripStatusLabel1.Text = status;
      };
    }

    //private RenderResult RenderHtmlInternal(string currentText, string filepath)
    //{
    //  var defaultBodyStyle = "";
    //  var markdownStyleContent = GetCssContent(filepath);
    //  if (!IsValidFileExtension(currentFilePath)) {
    //    var invalidExtensionMessageBody = string.Format(MSG_NO_SUPPORTED_FILE_EXT, Path.GetFileName(filepath), settings.SupportedFileExt);
    //    var invalidExtensionMessage = string.Format(
    //      DEFAULT_HTML_BASE,
    //      Path.GetFileName(filepath),
    //      markdownStyleContent,
    //      defaultBodyStyle,
    //      invalidExtensionMessageBody);
    //    return new RenderResult(invalidExtensionMessage, invalidExtensionMessage, invalidExtensionMessageBody, markdownStyleContent);
    //  }
    //  var markdownHtmlBrowser = string.Format(DEFAULT_HTML_BASE, Path.GetFileName(filepath), markdownStyleContent, defaultBodyStyle, resultForBrowser);
    //  var markdownHtmlFileExport = string.Format(
    //    DEFAULT_HTML_BASE,
    //    Path.GetFileName(filepath),
    //    markdownStyleContent,
    //    defaultBodyStyle,
    //    resultForExport);
    //  return new RenderResult(markdownHtmlBrowser, markdownHtmlFileExport, resultForBrowser, markdownStyleContent);
    //}

    public void RenderMarkdown(string currentText, string filepath)
    {
      if (_renderTask == null || _renderTask.IsCompleted) {
        MakeAndDisplayScreenShot();

        var cssFile = _settings.IsDarkModeEnabled ? _settings.CssDarkModeFileName : _settings.CssFileName;
        if (!File.Exists(cssFile)) {
          cssFile = _settings.IsDarkModeEnabled ? Settings.DefaultDarkModeCssFile : Settings.DefaultCssFile;
        }

        var context = TaskScheduler.FromCurrentSynchronizationContext();
        _renderTask = new Task(() => {
          try {
            var assetsPath = (!string.IsNullOrEmpty(_settings.AssetsPath) && Directory.Exists(_settings.AssetsPath))
              ? _settings.AssetsPath
              : _defaultAssetsPath;

            var withSyncView = (_settings.SyncViewWithCaretPosition || _settings.SyncViewWithFirstVisibleLine);

            webbrowserControl.SetContent(currentText, currentFilePath, assetsPath, cssFile, withSyncView);
            webbrowserControl.SetZoomLevel(_settings.ZoomLevel);
          }
          catch (Exception) { }
        });
        _renderTask.Start(context);
      }
    }

    /// <summary>
    /// Makes and displays a screenshot of the current browser content to prevent it from flickering  while loading
    /// updated content
    /// </summary>
    private void MakeAndDisplayScreenShot()
    {
      Bitmap bm = webbrowserControl.MakeScreenshot();
      if (bm != null) {
        pictureBoxScreenshot.Image = bm;
        pictureBoxScreenshot.Visible = true;
      }
    }

    private void HideScreenshotAndShowBrowser()
    {
      if (pictureBoxScreenshot.Image != null) {
        pictureBoxScreenshot.Visible = false;
        pictureBoxScreenshot.Image = null;
      }
    }

    public void ScrollToElementWithLineNo(int lineNo)
    {
      webbrowserControl.ScrollToElementWithLineNo((int)lineNo);
    }

    protected override void WndProc(ref Message m)
    {
      _wndProcCallback(ref m);

      //Continue the processing, as we only toggle
      base.WndProc(ref m);
    }

    private void btnSaveHtml_Click(object sender, EventArgs e)
    {
      using (SaveFileDialog saveFileDialog = new SaveFileDialog()) {
        saveFileDialog.Filter = "html files (*.html, *.htm)|*.html;*.htm|All files (*.*)|*.*";
        saveFileDialog.RestoreDirectory = true;
        saveFileDialog.InitialDirectory = Path.GetDirectoryName(currentFilePath);
        saveFileDialog.FileName = Path.GetFileNameWithoutExtension(currentFilePath);
        if (saveFileDialog.ShowDialog() == DialogResult.OK) {
          writeHtmlContentToFile(saveFileDialog.FileName);
        }
      }
    }

    private void writeHtmlContentToFile(string filename)
    {
      if (!string.IsNullOrEmpty(filename)) {
        File.WriteAllText(filename, htmlContentForExport);
      }
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

    public void SetMarkdownFilePath(string filepath)
    {
      currentFilePath = filepath;
    }

    const string DEFAULT_HTML_BASE = @"<!DOCTYPE html>
<html>
    <head>                    
        <meta http-equiv=""X-UA-Compatible"" content=""IE=edge""></meta>
        <meta http-equiv=""content-type"" content=""text/html; charset=utf-8""></meta>
        <title>{0}</title>
        <style type=""text/css"">
        {1}
        </style>
    </head>
    <body class=""markdown-body"" style=""{2}"">
    {3}
    </body>
</html>
";
    const string MSG_NO_SUPPORTED_FILE_EXT = "<h3>The current file <u>{0}</u> has no valid Markdown file extension.</h3><div>Valid file extensions:{1}</div>";

    private Task _renderTask;
    private string htmlContentForExport;
    private Settings _settings;
    private string currentFilePath;
    private IWebbrowserControl webbrowserControl;
    private IWebbrowserControl webview2Instance;
    private ActionRef<Message> _wndProcCallback;
    private string _defaultAssetsPath;
  }
}
