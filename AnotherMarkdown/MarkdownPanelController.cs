using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
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
              _previewForm = MarkdownPreviewForm.InitViewer(settings, HandleWndProc);
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
      notepadPPGateway = new NotepadPPGateway();
      SetIniFilePath();
      settings = LoadSettingsFromIni();

      renderTimer = new Timer();
      renderTimer.Interval = renderRefreshRateMilliSeconds;
      renderTimer.Tick += OnRenderTimerElapsed;
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
      settings.SyncViewWithCaretPosition = (Win32.GetPrivateProfileInt("Options", "SyncViewWithCaretPosition", 0, iniFilePath) != 0);
      settings.SyncViewWithFirstVisibleLine = (Win32.GetPrivateProfileInt("Options", "SyncWithFirstVisibleLine", 0, iniFilePath) != 0);

      settings.PreProcessorCommandFilename = Win32.ReadIniValue("Options", "PreProcessorExe", iniFilePath, "");
      settings.PreProcessorArguments = Win32.ReadIniValue("Options", "PreProcessorArguments", iniFilePath, "");
      settings.PostProcessorCommandFilename = Win32.ReadIniValue("Options", "PostProcessorExe", iniFilePath, "");
      settings.PostProcessorArguments = Win32.ReadIniValue("Options", "PostProcessorArguments", iniFilePath, "");
      settings.AssetsPath = Win32.ReadIniValue("Options", "AssetsPath", iniFilePath, "");
      settings.CssFileName = Win32.ReadIniValue("Options", "CssFileName", iniFilePath, "style.css");
      settings.CssDarkModeFileName = Win32.ReadIniValue("Options", "CssDarkModeFileName", iniFilePath, "style-dark.css");
      settings.ZoomLevel = Win32.GetPrivateProfileInt("Options", "ZoomLevel", 130, iniFilePath);
      settings.ShowToolbar = PluginUtils.ReadIniBool("Options", "ShowToolbar", iniFilePath);
      settings.ShowStatusbar = PluginUtils.ReadIniBool("Options", "ShowStatusbar", iniFilePath);
      settings.SupportedFileExt = Win32.ReadIniValue("Options", "SupportedFileExt", iniFilePath, Settings.DEFAULT_SUPPORTED_FILE_EXT);
      settings.AllowAllExtensions = PluginUtils.ReadIniBool("Options", "AllowAllExtensions", iniFilePath);
      settings.IsDarkModeEnabled = IsDarkModeEnabled();
      settings.AutoShowPanel = PluginUtils.ReadIniBool("Options", "AutoShowPanel", iniFilePath);
      return settings;
    }

    public void OnNotification(ScNotification notification)
    {
      if (IsPanelVisible && notification.Header.Code == (uint)SciMsg.SCN_UPDATEUI) {
        var scintillaGateway = scintillaGatewayFactory();
        if (settings.SyncViewWithCaretPosition) {
          if (lastCaretPosition != scintillaGateway.GetCurrentPos()) {
            lastCaretPosition = scintillaGateway.GetCurrentPos();
            ScrollToElementAtLineNo(scintillaGateway.GetCurrentLineNumber());
          }
        }
        else if (settings.SyncViewWithFirstVisibleLine) {
          if (currentFirstVisibleLine != scintillaGateway.GetFirstVisibleLine()) {
            var firstVisibleLine = scintillaGateway.GetFirstVisibleLine();
            currentFirstVisibleLine = firstVisibleLine;
            ScrollToElementAtLineNo(firstVisibleLine);
          }
        }
      }
      if (notification.Header.Code == (uint)NppMsg.NPPN_BUFFERACTIVATED) {
        // Focus was switched to a new document
        var currentFilePath = notepadPPGateway.GetCurrentFilePath();
        AutoShowOrHidePanel(currentFilePath);
        if (IsPanelVisible) {
          RenderMarkdownDirect();
        }
      }
      // NPPN_DARKMODECHANGED (NPPN_FIRST + 27) // To notify plugins that Dark Mode was enabled/disabled
      if (notification.Header.Code == (uint)(NppMsg.NPPN_FIRST + 27)) {
        settings.IsDarkModeEnabled = IsDarkModeEnabled();
        if (IsPanelVisible) {
          PreviewForm.UpdateSettings(settings);
          RenderMarkdownDirect();
        }
      }
      if (IsPanelVisible && notification.Header.Code == (uint)SciMsg.SCN_MODIFIED) {
        lastTickCount = Environment.TickCount;
        RenderMarkdownDeferred();
      }
      if (notification.Header.Code == (uint)NppMsg.NPPN_READY) {
        nppReady = true;
        var currentFilePath = notepadPPGateway.GetCurrentFilePath();
        AutoShowOrHidePanel(currentFilePath);
      }
    }

    private void RenderMarkdownDeferred()
    {
      // if we get a lot of key stroks within a short period, dont update preview
      var currentDeltaMilliseconds = Environment.TickCount - lastTickCount;
      if (currentDeltaMilliseconds < inputUpdateThresholdMiliseconds) {
        // Reset Timer
        renderTimer.Stop();
      }
      renderTimer.Start();
      lastTickCount = Environment.TickCount;
    }

    private void OnRenderTimerElapsed(object source, EventArgs e)
    {
      renderTimer.Stop();
      try {
        RenderMarkdownDirect();
      }
      catch { }
    }

    private void RenderMarkdownDirect()
    {
      if (IsPanelVisible) {
        PreviewForm.RenderMarkdown(GetCurrentEditorText(), notepadPPGateway.GetCurrentFilePath());
      }
    }

    private string GetCurrentEditorText()
    {
      var scintillaGateway = scintillaGatewayFactory();
      return scintillaGateway.GetText(scintillaGateway.GetLength() + 1);
    }

    private void ScrollToElementAtLineNo(int lineNo)
    {
      PreviewForm.ScrollToElementWithLineNo(lineNo);
    }

    public void InitCommandMenu()
    {
      PluginBase.SetCommand(0, "Toggle &Markdown Panel", TogglePanelVisible);
      PluginBase.SetCommand(1, "---", null);
      PluginBase.SetCommand(2, "Synchronize with &caret position", SyncViewWithCaretChanged, settings.SyncViewWithCaretPosition);
      PluginBase.SetCommand(3, "Synchronize with &first visible line in editor", SyncViewWithFirstVisibleLineChanged, settings.SyncViewWithFirstVisibleLine);
      PluginBase.SetCommand(4, "---", null);
      PluginBase.SetCommand(5, "&Settings", EditSettings);
      PluginBase.SetCommand(6, "&Help", ShowHelp);
      PluginBase.SetCommand(7, "&About", ShowAboutDialog);
      idMyDlg = 0;
    }

    private void EditSettings()
    {
      var settingsForm = new SettingsForm(settings);
      if (settingsForm.ShowDialog() == DialogResult.OK) {
        settings.AssetsPath = settingsForm.AssetsPath;
        settings.CssFileName = settingsForm.CssFileName;
        settings.CssDarkModeFileName = settingsForm.CssDarkModeFileName;
        settings.ZoomLevel = settingsForm.ZoomLevel;
        settings.ShowToolbar = settingsForm.ShowToolbar;
        settings.SupportedFileExt = settingsForm.SupportedFileExt;
        settings.AllowAllExtensions = settingsForm.AllowAllExtensions;
        settings.ShowStatusbar = settingsForm.ShowStatusbar;
        settings.AutoShowPanel = settingsForm.AutoShowPanel;
        settings.RenderingEngine = settingsForm.RenderingEngine;

        settings.IsDarkModeEnabled = IsDarkModeEnabled();
        SaveSettings();
        //Update Preview
        if (IsPanelVisible) {
          PreviewForm.UpdateSettings(settings);
          RenderMarkdownDirect();
        }
      }
    }

    private void DocumentChanged(DocumentContentChanged args)
    {
      var scintillaGateway = scintillaGatewayFactory();
      int pos = scintillaGateway.GetCurrentPos();
      scintillaGateway.SetText(args.Content);

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
      iniFilePath = sbIniFilePath.ToString();
      if (!Directory.Exists(iniFilePath)) {
        Directory.CreateDirectory(iniFilePath);
      }

      iniFilePath = Path.Combine(iniFilePath, Main.ModuleName + ".ini");
    }

    private void SyncViewWithCaretChanged()
    {
      var value = !settings.SyncViewWithCaretPosition;
      settings.SyncViewWithCaretPosition = value;
      if (value && settings.SyncViewWithFirstVisibleLine) {
        // Disable syncWithFirstVisibleLine
        SyncViewWithFirstVisibleLineChanged();
      }

      Win32.CheckMenuItem(Win32.GetMenu(PluginBase.nppData._nppHandle), PluginBase._funcItems.Items[2]._cmdID,
        Win32.MF_BYCOMMAND | (value ? Win32.MF_CHECKED : Win32.MF_UNCHECKED));
      var scintillaGateway = scintillaGatewayFactory();

      if (value) {
        ScrollToElementAtLineNo(scintillaGateway.GetCurrentLineNumber());
      }
    }

    private void SyncViewWithFirstVisibleLineChanged()
    {
      var value = !settings.SyncViewWithFirstVisibleLine;
      settings.SyncViewWithFirstVisibleLine = value;
      if (value && settings.SyncViewWithCaretPosition) {
        // Disable syncViewWithCaretPosition
        SyncViewWithCaretChanged();
      }

      Win32.CheckMenuItem(Win32.GetMenu(PluginBase.nppData._nppHandle), PluginBase._funcItems.Items[3]._cmdID, Win32.MF_BYCOMMAND | (value ? Win32.MF_CHECKED : Win32.MF_UNCHECKED));
      var scintillaGateway = scintillaGatewayFactory();
      if (value) {
        ScrollToElementAtLineNo(scintillaGateway.GetFirstVisibleLine());
      }
    }

    public void SetToolBarIcon()
    {
      toolbarIcons tbIconsOld = new toolbarIcons();
      tbIconsOld.hToolbarBmp = Resources.markdown_16x16_solid.GetHbitmap();
      tbIconsOld.hToolbarIcon = Resources.markdown_16x16_solid_dark.GetHicon();
      IntPtr pTbIcons = Marshal.AllocHGlobal(Marshal.SizeOf(tbIconsOld));
      Marshal.StructureToPtr(tbIconsOld, pTbIcons, false);
      Win32.SendMessage(PluginBase.nppData._nppHandle, (uint)NppMsg.NPPM_ADDTOOLBARICON, PluginBase._funcItems.Items[idMyDlg]._cmdID, pTbIcons);
      Marshal.FreeHGlobal(pTbIcons);
    }

    public void PluginCleanUp()
    {
      SaveSettings();
    }

    private void SaveSettings()
    {
      Win32.WritePrivateProfileString("Options", "SyncViewWithCaretPosition", settings.SyncViewWithCaretPosition ? "1" : "0", iniFilePath);
      Win32.WritePrivateProfileString("Options", "SyncWithFirstVisibleLine", settings.SyncViewWithFirstVisibleLine ? "1" : "0", iniFilePath);

      Win32.WriteIniValue("Options", "AssetsPath", settings.AssetsPath, iniFilePath);
      Win32.WriteIniValue("Options", "CssFileName", settings.CssFileName, iniFilePath);
      Win32.WriteIniValue("Options", "CssDarkModeFileName", settings.CssDarkModeFileName, iniFilePath);
      Win32.WriteIniValue("Options", "ZoomLevel", settings.ZoomLevel.ToString(), iniFilePath);
      Win32.WriteIniValue("Options", "ShowToolbar", settings.ShowToolbar.ToString(), iniFilePath);
      Win32.WriteIniValue("Options", "ShowStatusbar", settings.ShowStatusbar.ToString(), iniFilePath);
      Win32.WriteIniValue("Options", "SupportedFileExt", settings.SupportedFileExt, iniFilePath);
      Win32.WriteIniValue("Options", "AutoShowPanel", settings.AutoShowPanel.ToString(), iniFilePath);
      Win32.WriteIniValue("Options", "AllowAllExtensions", settings.AllowAllExtensions.ToString(), iniFilePath);
      Win32.WriteIniValue("Options", "RenderingEngine", settings.RenderingEngine, iniFilePath);
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
        tbData.dlgID = idMyDlg;
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
        PreviewForm.UpdateSettings(settings);
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
      IntPtr ret = Win32.SendMessage(PluginBase.nppData._nppHandle, (uint)(Constants.NPPMSG + 107), Unused, Unused);
      return ret.ToInt32() == 1;
    }

    private void AutoShowOrHidePanel(string currentFilePath)
    {
      if (nppReady && settings.AutoShowPanel) {
        // automatically show panel for supported file types
        if ((!IsPanelVisible && PreviewForm.IsValidFileExtension(currentFilePath))
          || (IsPanelVisible && !PreviewForm.IsValidFileExtension(currentFilePath))) {
          TogglePanelVisible();
        }
      }
    }

    protected void HandleWndProc(ref Message m)
    {
      switch (m.Msg) {
        case (int)WindowsMessage.WM_NOTIFY:
          var notify = (NMHDR)Marshal.PtrToStructure(m.LParam, typeof(NMHDR));

          // do not intercept Npp notifications like DMN_CLOSE, etc.
          if (notify.hwndFrom != PluginBase.nppData._nppHandle) {
            PreviewForm.Invalidate(true);
            if (Environment.Is64BitOperatingSystem) {
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
        if (disposing) {
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
        _disposedValue = true;
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

    private const int Unused = 0;
    private const int renderRefreshRateMilliSeconds = 250;
    private const int inputUpdateThresholdMiliseconds = 200;

    private MarkdownPreviewForm _previewForm;
    private object _lock = new object();
    private Timer renderTimer;
    private int idMyDlg = -1;
    private int lastTickCount = 0;
    private bool IsPanelVisible { get; set; }

    private readonly Func<IScintillaGateway> scintillaGatewayFactory;
    private readonly INotepadPPGateway notepadPPGateway;
    private string iniFilePath;
    private int lastCaretPosition;
    private int currentFirstVisibleLine;
    private bool nppReady;
    private Settings settings;

    private IntPtr? _ptrNppTbData;
    private Icon _icon;
    private Bitmap _iconBmp;
    private bool _disposedValue;
  }
}
