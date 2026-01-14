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

    public static MarkdownPreviewForm Create(Settings settings, ActionRef<Message> wndProcCallback) { 
      return new MarkdownPreviewForm(settings, wndProcCallback);
    }

    private MarkdownPreviewForm(Settings settings, ActionRef<Message> wndProcCallback)
    {
      _wndProcCallback = wndProcCallback;
      OnEvent = new EventDispatcher();
      InitializeComponent();

      var webView = new Webview2WebbrowserControl();
      webView.Initialize(new ProxySettings(settings), OnEvent);

      panel1.Controls.Clear();
      webView.AddToHost(panel1);
      panel1.Visible = true;

      webView.StatusTextChangedAction = (status) => {
        toolStripStatusLabel1.Text = status;
      };
      _webView = webView;
    }

    public void UpdateSettings(Settings settings)
    {
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
      lock (_renderTaskLock) {
        _markdownContent = new MarkdownContent {
          Path = filepath,
          Text = currentText,
        };

        if (_renderTask == null || (_renderTask.IsCompleted || _renderTask.IsFaulted)) {
          _renderTask = RenderMarkdownTask();
        }
      }
    }

    private async Task RenderMarkdownTask()
    {
      try {
        while (true) {
          await Task.Delay(20);
          MarkdownContent content;
          lock (_renderTaskLock) {
            if (_markdownContent == null) {
              _renderTask = null;
              break;
            }
            content = _markdownContent.Value;
            _markdownContent = null;
          }
          
          await _webView.SetContentAsync(content.Text, content.Path);
        }
      }
      catch (Exception err) {
        Console.WriteLine(err);
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

    private struct MarkdownContent
    {
      public string Text;
      public string Path;
    }

    private MarkdownContent? _markdownContent;

    private object _renderTaskLock = new object();
    private Task _renderTask;
    private Webview2WebbrowserControl _webView;
    private ActionRef<Message> _wndProcCallback;
  }
}
