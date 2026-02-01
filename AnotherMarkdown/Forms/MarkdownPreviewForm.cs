using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using AnotherMarkdown.Entities;
using Kbg.NppPluginNET.PluginInfrastructure;
using Webview2Viewer;

namespace AnotherMarkdown.Forms
{
  public partial class MarkdownPreviewForm : Form
  {
    public EventDispatcher OnEvent { get; set; }

    public EventHandler DockClosed { get; set; }

    public static MarkdownPreviewForm Create(Settings settings) { 
      return new MarkdownPreviewForm(settings);
    }

    private MarkdownPreviewForm(Settings settings)
    {
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

    public async Task RenderMarkdown(string currentText, string filepath)
    {
      if (_webView != null) {
        await _webView.SetContentAsync(currentText, filepath);
      }
    }

    public async Task ScrollToElementWithLineNo(int lineNo)
    {
      if (_webView != null) {
        await _webView.ScrollToElementWithLineNo(lineNo);
      }
    }

    protected override void WndProc(ref Message m)
    {
      if (m.Msg == Win32.WM_NOTIFY) {
        var nmdr = (Win32.NMHDR) Marshal.PtrToStructure(m.LParam, typeof(Win32.NMHDR));
        if (nmdr.hwndFrom == PluginBase.nppData._nppHandle) {
          switch ((DockMgrMsg) (nmdr.code & 0xFFFFU)) {
            case DockMgrMsg.DMN_DOCK: {
              break;
            }
            case DockMgrMsg.DMN_FLOAT: {
              RemoveControlParent(this);
              break;
            }
            case DockMgrMsg.DMN_CLOSE: {
              DockClosed?.Invoke(this, EventArgs.Empty);
              break;
            }
          }
        }
      }
      base.WndProc(ref m);
    }

    /// <summary>
    /// Sets the <see cref="Win32.WS_EX_CONTROLPARENT"/> extended attribute on <paramref name="parent"/> and any child
    /// controls, following @mahee96's advice on the archived Plugin.Net issue tracker. 
    /// <para><seealso href="https://github.com/kbilsted/NotepadPlusPlusPluginPack.Net/issues/17#issuecomment-683455467"/></para>
    /// <para><seealso href="https://github.com/mohzy83/NppMarkdownPanel/issues/106"/></para>
    /// <para><seealso href="https://github.com/BdR76/CSVLint/pull/88"/></para>
    /// </summary>
    /// <param name="parent">
    /// A WinForm that's been registered with Npp's Docking Manager by sending <see cref="NppMsg.NPPM_DMMREGASDCKDLG"/>.
    /// </param>
    private void RemoveControlParent(Control parent)
    {
      if (parent.HasChildren) {
        long extAttrs = (Environment.Is64BitProcess)
          ? (long) Win32.GetWindowLongPtr(parent.Handle, Win32.GWL_EXSTYLE)
          : (long) Win32.GetWindowLong(parent.Handle, Win32.GWL_EXSTYLE);

        if (Win32.WS_EX_CONTROLPARENT == (extAttrs & Win32.WS_EX_CONTROLPARENT)) {
          var newAttrs = new IntPtr(extAttrs & ~Win32.WS_EX_CONTROLPARENT);

          _ = (Environment.Is64BitProcess)
            ? (long) Win32.SetWindowLongPtr(parent.Handle, Win32.GWL_EXSTYLE, newAttrs)
            : (long) Win32.SetWindowLong(parent.Handle, Win32.GWL_EXSTYLE, newAttrs);
        }
        foreach (Control c in parent.Controls) {
          RemoveControlParent(c);
        }
      }
    }

    /// <summary>
    /// Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
      if (disposing) {
        _disposed = true;
        if (_webView != null) {
          _webView.Dispose();
          _webView = null;
        }
        if (components != null) {
          components.Dispose();
          components = null;
        }
      }
      base.Dispose(disposing);
    }
    private Webview2WebbrowserControl _webView;
    private bool _disposed;
  }
}
