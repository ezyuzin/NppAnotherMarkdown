using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using AnotherMarkdown.Entities;
using Webview2Viewer;

namespace AnotherMarkdown.Forms
{
  public partial class MarkdownPreviewForm : Form, IViewerInterface
  {
    public EventDispatcher OnEvent { get; set; }

    public static MarkdownPreviewForm InitViewer(Settings settings, ActionRef<Message> wndProcCallback)
    {
      return new MarkdownPreviewForm(settings, wndProcCallback);
    }

    private MarkdownPreviewForm(Settings settings, ActionRef<Message> wndProcCallback)
    {
      InitializeComponent();

      OnEvent = new EventDispatcher();
      _wndProcCallback = wndProcCallback;
      _settings = settings;

      var webview = new Webview2WebbrowserControl();
      webview.StatusTextChangedAction = (status) => {
        toolStripStatusLabel1.Text = status;
      };

      _webviewInitTask = webview
        .InitializeAsync(new ProxySettings(_settings), OnEvent)
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

        var context = TaskScheduler.FromCurrentSynchronizationContext();
        _renderTask = new Task(async () => {
          try {

            await _webviewInitTask;
            if (_webView != null) {
              await _webView.SetContent(currentText, filepath);
            }
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

    private Task _webviewInitTask;
    private Task _renderTask;
    private Settings _settings;
    private Webview2WebbrowserControl _webView;
    private ActionRef<Message> _wndProcCallback;
  }
}
