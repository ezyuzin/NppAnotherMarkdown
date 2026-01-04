using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using AnotherMarkdown.Entities;
using AnotherMarkdown.Forms;
using AnotherMarkdown.Properties;
using Kbg.NppPluginNET.PluginInfrastructure;
using PanelCommon;

namespace AnotherMarkdown
{
  public class MarkdownPanelController : IDisposable
  {
    private MarkdownPreviewForm PreviewForm
    {
      get {
        if (_previewForm == null) {
          lock (_lock) {
            if (_previewForm == null) {
              _previewForm = MarkdownPreviewForm.InitViewer(_settings, HandleWndProc);
              _previewForm.OnDocumentContentChanged += (s, e) => {
                DocumentChanged(e);
              };
            }
          }
        }
        return _previewForm;
      }
    }

    public MarkdownPanelController()
    {
      AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

      scintillaGatewayFactory = PluginBase.GetGatewayFactory();
      _nppGateway = new NotepadPPGateway();
      SetIniFilePath();
      _settings = LoadSettingsFromIni();
    }

    private Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
    {
      var di = new DirectoryInfo(Path.Combine(PluginUtils.GetPluginDirectory(), "lib"));

      var modulename = args.Name.Split(',')[0];

      var module = di.GetFiles().FirstOrDefault(i => i.Name == modulename + ".dll");
      if (module != null) {
        return Assembly.LoadFrom(module.FullName);
      }
      return null;
    }

    private Settings LoadSettingsFromIni()
    {
      Settings settings = new Settings();
      settings.SyncViewWithCaretPosition = (Win32.GetPrivateProfileInt("Options", "SyncViewWithCaretPosition", 0, _iniFilePath) != 0);
      settings.SyncViewWithFirstVisibleLine = (Win32.GetPrivateProfileInt("Options", "SyncWithFirstVisibleLine", 0, _iniFilePath) != 0);

      settings.PreProcessorCommandFilename = Win32.ReadIniValue("Options", "PreProcessorExe", _iniFilePath, "");
      settings.PreProcessorArguments = Win32.ReadIniValue("Options", "PreProcessorArguments", _iniFilePath, "");
      settings.PostProcessorCommandFilename = Win32.ReadIniValue("Options", "PostProcessorExe", _iniFilePath, "");
      settings.PostProcessorArguments = Win32.ReadIniValue("Options", "PostProcessorArguments", _iniFilePath, "");
      settings.AssetsPath = Win32.ReadIniValue("Options", "AssetsPath", _iniFilePath, "");
      settings.CssFileName = Win32.ReadIniValue("Options", "CssFileName", _iniFilePath, "style.css");
      settings.CssDarkModeFileName = Win32.ReadIniValue("Options", "CssDarkModeFileName", _iniFilePath, "style-dark.css");
      settings.ZoomLevel = Win32.GetPrivateProfileInt("Options", "ZoomLevel", 130, _iniFilePath);
      settings.ShowToolbar = PluginUtils.ReadIniBool("Options", "ShowToolbar", _iniFilePath);
      settings.ShowStatusbar = PluginUtils.ReadIniBool("Options", "ShowStatusbar", _iniFilePath);
      settings.IsDarkModeEnabled = IsDarkModeEnabled();
      return settings;
    }

    public void OnNotification(ScNotification notification)
    {
      try {
        NotificationHandler(notification);
      }
      catch (Exception) {
      }
    }

    private void NotificationHandler(ScNotification notification) 
    { 
      if (_disposedValue) {
        return;
      }

      switch (notification.Header.Code) {
        case (uint) SciMsg.SCN_UPDATEUI: {
          if (IsPanelVisible && _settings.SyncViewWithCaretPosition) {
            var scintillaGateway = scintillaGatewayFactory();
            var currentPos = scintillaGateway.GetCurrentLineNumber();
            if (_lastCaretPosition != currentPos) {
              _lastCaretPosition = currentPos;
              if (_skipScrollEventDue < DateTime.UtcNow) {
                ScrollToElementAtLineNo(_lastCaretPosition);
              }
            }
          }
          else if (IsPanelVisible && _settings.SyncViewWithFirstVisibleLine) {
            var scintillaGateway = scintillaGatewayFactory();
            var currentPos = scintillaGateway.GetFirstVisibleLine();

            if (_currentFirstVisibleLine != currentPos) {
              _currentFirstVisibleLine = currentPos;
              if (_skipScrollEventDue < DateTime.UtcNow) {
                var docLine = scintillaGateway.DocLineFromVisible(currentPos);
                ScrollToElementAtLineNo(docLine);
              }
            }
          }
          break;
        }
        case (uint) NppMsg.NPPN_BUFFERACTIVATED: {
          RenderMarkdownDirect();
          break;
        }
        case (uint) (NppMsg.NPPN_FIRST + 27): {
          // NPPN_DARKMODECHANGED (NPPN_FIRST + 27) // To notify plugins that Dark Mode was enabled/disabled

          _settings.IsDarkModeEnabled = IsDarkModeEnabled();
          if (IsPanelVisible) {
            PreviewForm.UpdateSettings(_settings);
            RenderMarkdownDirect();
          }
          break;
        }
        case (uint) SciMsg.SCN_MODIFIED: {
          RenderMarkdownDeferred();
          break;
        }
      }
    }

    private void RenderMarkdownDeferred()
    {
      lock(_renderDeferredLock) {
        if (_renderDeferredTask != null && !_renderDeferredTask.IsCompleted) {
          var task = _renderDeferredTask;
          var cts = _renderDeferredCancellationSource;
          cts.Cancel();
          task.ContinueWith(t => cts.Dispose());
        }
        _renderDeferredCancellationSource = new CancellationTokenSource();
        _renderDeferredTask = RenderDeferredWorkerAsync(_renderDeferredCancellationSource.Token);
      }
    }

    private async Task RenderDeferredWorkerAsync(CancellationToken cancellationToken)
    {
      await Task.Delay(InputUpdateThreshold, cancellationToken);
      try {
        RenderMarkdownDirect();
      }
      catch { }
    }

    private void RenderMarkdownDirect()
    {
      if (IsPanelVisible) {
        PreviewForm.RenderMarkdown(GetCurrentEditorText(), _nppGateway.GetCurrentFilePath());
      }
    }

    private string GetCurrentEditorText()
    {
      var scintillaGateway = scintillaGatewayFactory();
      return scintillaGateway.GetText(scintillaGateway.GetLength() + 1);
    }

    private void ScrollToElementAtLineNo(int lineNo)
    {
      if (IsPanelVisible) {
        PreviewForm.ScrollToElementWithLineNo(lineNo);
      }
    }

    public void InitCommandMenu()
    {
      PluginBase.SetCommand(0, "Toggle &Markdown Panel", TogglePanelVisible);
      PluginBase.SetCommand(1, "---", null);
      PluginBase.SetCommand(2, "Synchronize with &caret position", SyncViewWithCaretClicked, _settings.SyncViewWithCaretPosition);
      PluginBase.SetCommand(3, "Synchronize with &first visible line in editor", SyncViewWithFirstVisibleLineClicked, _settings.SyncViewWithFirstVisibleLine);
      PluginBase.SetCommand(4, "---", null);
      PluginBase.SetCommand(5, "&Settings", EditSettings);
      PluginBase.SetCommand(6, "&Help", ShowHelp);
      PluginBase.SetCommand(7, "&About", ShowAboutDialog);
      _myDlgId = 0;
    }

    private void EditSettings()
    {
      var settingsForm = new SettingsForm(_settings);
      if (settingsForm.ShowDialog() == DialogResult.OK) {
        _settings.AssetsPath = settingsForm.AssetsPath;
        _settings.CssFileName = settingsForm.CssFileName;
        _settings.CssDarkModeFileName = settingsForm.CssDarkModeFileName;
        _settings.ZoomLevel = settingsForm.ZoomLevel;
        _settings.ShowToolbar = settingsForm.ShowToolbar;
        _settings.ShowStatusbar = settingsForm.ShowStatusbar;

        _settings.IsDarkModeEnabled = IsDarkModeEnabled();
        SaveSettings();
        //Update Preview
        if (IsPanelVisible) {
          PreviewForm.UpdateSettings(_settings);
          RenderMarkdownDirect();
        }
      }
    }

    private void DocumentChanged(DocumentContentChanged args)
    {
      var scintillaGateway = scintillaGatewayFactory();
      int pos = scintillaGateway.GetCurrentPos();
      scintillaGateway.SetText(args.Content);

      if (SyncViewEnabled) {
        _skipScrollEventDue = DateTime.UtcNow.AddSeconds(1);
      }

      scintillaGateway.GotoPos(pos);
      scintillaGateway.ScrollCaret();
    }

    private void ShowHelp()
    {
      var currentPluginPath = PluginUtils.GetPluginDirectory();
      var helpFile = Path.Combine(currentPluginPath, "README.md");
      Win32.SendMessage(PluginBase.nppData._nppHandle, (uint)NppMsg.NPPM_DOOPEN, 0, helpFile);
      if (!IsPanelVisible) {
        TogglePanelVisible();
      }

      RenderMarkdownDirect();
    }

    private void SetIniFilePath()
    {
      StringBuilder sbIniFilePath = new StringBuilder(Win32.MAX_PATH);
      Win32.SendMessage(PluginBase.nppData._nppHandle, (uint)NppMsg.NPPM_GETPLUGINSCONFIGDIR, Win32.MAX_PATH, sbIniFilePath);
      _iniFilePath = sbIniFilePath.ToString();
      if (!Directory.Exists(_iniFilePath)) {
        Directory.CreateDirectory(_iniFilePath);
      }

      _iniFilePath = Path.Combine(_iniFilePath, Main.ModuleName + ".ini");
    }

    private bool SyncViewEnabled => (_settings.SyncViewWithCaretPosition || _settings.SyncViewWithFirstVisibleLine);

    private void SyncViewWithCaretClicked()
    {
      var wasSyncView = SyncViewEnabled;
      SetSyncViewWithCaretPosition(!_settings.SyncViewWithCaretPosition);
      if (SyncViewEnabled != wasSyncView) {
        RenderMarkdownDeferred();
      }      
    }

    private void SyncViewWithFirstVisibleLineClicked()
    {
      var wasSyncView = SyncViewEnabled;
      SetSyncViewWithFirstVisibleLine(!_settings.SyncViewWithFirstVisibleLine);
      if (SyncViewEnabled != wasSyncView) {
        RenderMarkdownDeferred();
      }
    }

    private void SetSyncViewWithCaretPosition(bool enabled)
    {
      if (_settings.SyncViewWithCaretPosition == enabled) {
        return;
      }
      _settings.SyncViewWithCaretPosition = enabled;
      if (enabled) {
        SetSyncViewWithFirstVisibleLine(false);
      }

      Win32.CheckMenuItem(Win32.GetMenu(PluginBase.nppData._nppHandle), PluginBase._funcItems.Items[2]._cmdID, Win32.MF_BYCOMMAND | (enabled ? Win32.MF_CHECKED : Win32.MF_UNCHECKED));
    }

    private void SetSyncViewWithFirstVisibleLine(bool enabled)
    {
      if (_settings.SyncViewWithFirstVisibleLine == enabled) {
        return;
      }
      _settings.SyncViewWithFirstVisibleLine = enabled;
      if (enabled) {
        SetSyncViewWithCaretPosition(false);
      }
      Win32.CheckMenuItem(Win32.GetMenu(PluginBase.nppData._nppHandle), PluginBase._funcItems.Items[3]._cmdID, Win32.MF_BYCOMMAND | (enabled ? Win32.MF_CHECKED : Win32.MF_UNCHECKED));
    }

    public void SetToolBarIcon()
    {
      toolbarIcons tbIconsOld = new toolbarIcons();
      tbIconsOld.hToolbarBmp = Resources.markdown_16x16_solid.GetHbitmap();
      tbIconsOld.hToolbarIcon = Resources.markdown_16x16_solid_dark.GetHicon();
      IntPtr pTbIcons = Marshal.AllocHGlobal(Marshal.SizeOf(tbIconsOld));
      Marshal.StructureToPtr(tbIconsOld, pTbIcons, false);
      Win32.SendMessage(PluginBase.nppData._nppHandle, (uint)NppMsg.NPPM_ADDTOOLBARICON, PluginBase._funcItems.Items[_myDlgId]._cmdID, pTbIcons);
      Marshal.FreeHGlobal(pTbIcons);
    }

    public void PluginCleanUp()
    {
      SaveSettings();
    }

    private void SaveSettings()
    {
      Win32.WritePrivateProfileString("Options", "SyncViewWithCaretPosition", _settings.SyncViewWithCaretPosition ? "1" : "0", _iniFilePath);
      Win32.WritePrivateProfileString("Options", "SyncWithFirstVisibleLine", _settings.SyncViewWithFirstVisibleLine ? "1" : "0", _iniFilePath);

      Win32.WriteIniValue("Options", "AssetsPath", _settings.AssetsPath, _iniFilePath);
      Win32.WriteIniValue("Options", "CssFileName", _settings.CssFileName, _iniFilePath);
      Win32.WriteIniValue("Options", "CssDarkModeFileName", _settings.CssDarkModeFileName, _iniFilePath);
      Win32.WriteIniValue("Options", "ZoomLevel", _settings.ZoomLevel.ToString(), _iniFilePath);
      Win32.WriteIniValue("Options", "ShowToolbar", _settings.ShowToolbar.ToString(), _iniFilePath);
      Win32.WriteIniValue("Options", "ShowStatusbar", _settings.ShowStatusbar.ToString(), _iniFilePath);
    }

    private void ShowAboutDialog()
    {
      var aboutDialog = new AboutForm();
      aboutDialog.ShowDialog();
    }

    private void TogglePanelVisible()
    {
      if (!_ptrNppTbData.HasValue) {
        var tbData = new NppTbData();
        tbData.hClient = PreviewForm.Handle;
        tbData.pszName = Main.PluginTitle;
        tbData.dlgID = _myDlgId;
        tbData.uMask = NppTbMsg.DWS_DF_CONT_RIGHT | NppTbMsg.DWS_ICONTAB | NppTbMsg.DWS_ICONBAR;
        tbData.hIconTab = (uint) ConvertBitmapToIcon(Resources.markdown_16x16_solid_bmp).Handle;
        tbData.pszModuleName = $"{Main.ModuleName}.dll";

        _ptrNppTbData = Marshal.AllocHGlobal(Marshal.SizeOf(tbData));
        Marshal.StructureToPtr(tbData, _ptrNppTbData.Value, false);

        Win32.SendMessage(PluginBase.nppData._nppHandle, (uint) NppMsg.NPPM_DMMREGASDCKDLG, 0, _ptrNppTbData.Value);
        Win32.SendMessage(PluginBase.nppData._nppHandle, (uint) NppMsg.NPPM_DMMSHOW, 0, PreviewForm.Handle);
        IsPanelVisible = true;
      }
      else {
        IsPanelVisible = !IsPanelVisible;
        var flag = IsPanelVisible ? NppMsg.NPPM_DMMSHOW : NppMsg.NPPM_DMMHIDE;
        Win32.SendMessage(PluginBase.nppData._nppHandle, (uint) flag, 0, PreviewForm.Handle);
      }

      if (IsPanelVisible) {
        PreviewForm.UpdateSettings(_settings);
        RenderMarkdownDirect();
      }
    }

    private Icon ConvertBitmapToIcon(Bitmap bitmapImage)
    {
      if (_icon != null) {
        return _icon; 
      }

      _iconBmp = new Bitmap(16, 16);
      using (Graphics g = Graphics.FromImage(_iconBmp)) {
        ColorMap[] colorMap = new ColorMap[1];
        colorMap[0] = new ColorMap();
        colorMap[0].OldColor = Color.Fuchsia;
        colorMap[0].NewColor = Color.FromKnownColor(KnownColor.ButtonFace);
        ImageAttributes attr = new ImageAttributes();
        attr.SetRemapTable(colorMap);
        g.DrawImage(bitmapImage, new Rectangle(0, 0, 16, 16), 0, 0, 16, 16, GraphicsUnit.Pixel, attr);
        _icon = Icon.FromHandle(_iconBmp.GetHicon());
      }
      return _icon;
    }

    /// <summary>
    /// Actions to do after the tool window was closed
    /// </summary>
    private void ToolWindowCloseAction()
    {
      TogglePanelVisible();
    }

    private bool IsDarkModeEnabled()
    {
      // NPPM_ISDARKMODEENABLED (NPPMSG + 107)
      IntPtr ret = Win32.SendMessage(PluginBase.nppData._nppHandle, (uint)(Constants.NPPMSG + 107), UNUSED, UNUSED);
      return ret.ToInt32() == 1;
    }

    protected void HandleWndProc(ref Message m)
    {
      if (_disposedValue) {
        return;
      }

      switch (m.Msg) {
        case (int)WindowsMessage.WM_NOTIFY:
          var notify = (NMHDR)Marshal.PtrToStructure(m.LParam, typeof(NMHDR));

          // do not intercept Npp notifications like DMN_CLOSE, etc.
          if (notify.hwndFrom != PluginBase.nppData._nppHandle) {
            PreviewForm.Invalidate(true);
            if (Environment.Is64BitProcess) {
              SetControlParent(PreviewForm, Win32.GetWindowLongPtr, Win32.SetWindowLongPtr);
            }
            else {
              SetControlParent(PreviewForm, Win32.GetWindowLong, Win32.SetWindowLong);
            }

            PreviewForm.Update();
            return;
          }

          switch (notify.code) {
            case (int) DockMgrMsg.DMN_CLOSE: {
              ToolWindowCloseAction();
              break;
            }
          }
          break;
      }
    }

    /// <summary>
    /// Sets the <see cref="Win32.WS_EX_CONTROLPARENT"/> extended attribute on <paramref name="parent"/> and any child
    /// controls, following @mahee96's advice on the archived Plugin.Net issue tracker. <para><seealso
    /// href="https://github.com/kbilsted/NotepadPlusPlusPluginPack.Net/issues/17#issuecomment-683455467"/></para>
    /// </summary>
    /// <param name="parent">
    /// A WinForm that's been registered with Npp's Docking Manager by sending <see cref="NppMsg.NPPM_DMMREGASDCKDLG"/>.
    /// </param>
    private void SetControlParent(Control parent, Func<IntPtr, int, IntPtr> wndLongGetter, Func<IntPtr, int, IntPtr, IntPtr> wndLongSetter)
    {
      if (parent.HasChildren) {
        long extAttrs = (long)wndLongGetter(parent.Handle, Win32.GWL_EXSTYLE);
        if (Win32.WS_EX_CONTROLPARENT != (extAttrs & Win32.WS_EX_CONTROLPARENT)) {
          wndLongSetter(parent.Handle, Win32.GWL_EXSTYLE, new IntPtr(extAttrs | Win32.WS_EX_CONTROLPARENT));
        }
        foreach (Control c in parent.Controls) {
          SetControlParent(c, wndLongGetter, wndLongSetter);
        }
      }
    }


    protected virtual void Dispose(bool disposing)
    {
      if (!_disposedValue) {
        _disposedValue = true;
        if (disposing) {
          if (_renderDeferredCancellationSource != null) {
            _renderDeferredCancellationSource.Cancel();
            if (_renderDeferredTask != null) {
              _renderDeferredTask.Wait();
              _renderDeferredTask = null;
            }
            _renderDeferredCancellationSource.Dispose();
            _renderDeferredCancellationSource = null;
          }

          _icon?.Dispose();
          _iconBmp?.Dispose();
          _icon = null;
          _iconBmp = null;

          if (_ptrNppTbData.HasValue) {
            Marshal.DestroyStructure(_ptrNppTbData.Value, typeof(NppTbData));
            Marshal.FreeHGlobal(_ptrNppTbData.Value);
            _ptrNppTbData = null;
          }
          _previewForm?.Dispose();
          _previewForm = null;
        }
        
      }
    }

    public void Dispose()
    {
      // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
      Dispose(disposing: true);
      GC.SuppressFinalize(this);
    }


    [StructLayout(LayoutKind.Sequential)]
    public struct NMHDR
    {
      public IntPtr hwndFrom;
      public IntPtr idFrom;
      public int code;
    }

    public enum WindowsMessage
    {
      WM_NOTIFY = 0x004E
    }

    private bool IsPanelVisible { get; set; }

    private const int UNUSED = 0;
    private static readonly TimeSpan InputUpdateThreshold = TimeSpan.FromMilliseconds(400);

    private object _renderDeferredLock = new object();
    private Task _renderDeferredTask;
    private CancellationTokenSource _renderDeferredCancellationSource;

    private MarkdownPreviewForm _previewForm;
    private object _lock = new object();
    private int _myDlgId = -1;

    private readonly Func<IScintillaGateway> scintillaGatewayFactory;
    private readonly INotepadPPGateway _nppGateway;
    private string _iniFilePath;
    private int _lastCaretPosition;
    private int _currentFirstVisibleLine;
    private Settings _settings;

    private IntPtr? _ptrNppTbData;
    private Icon _icon;
    private Bitmap _iconBmp;
    private bool _disposedValue;
    private DateTime _skipScrollEventDue = DateTime.MinValue;
  }
}
