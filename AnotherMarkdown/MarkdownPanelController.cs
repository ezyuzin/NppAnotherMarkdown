using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using AnotherMarkdown.Entities;
using AnotherMarkdown.Forms;
using AnotherMarkdown.Properties;
using DiffPlex;
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
              try {
                _previewForm = MarkdownPreviewForm.Create(_settings);
                _previewForm.OnEvent.DocumentChanged += (_, e) => DocumentChanged(e);
                _previewForm.OnEvent.TrackFirstLine += (_, e) => FirstLineChanged(e);
                _previewForm.OnEvent.PasteImage += (_, e) => PasteImage(e);
                _previewForm.OnEvent.Navigate += (_, e) => OpenFile(e);
                _previewForm.DockClosed += (_, e) => TogglePanelVisible();
              }
              catch (Exception ex) {
                Console.WriteLine(ex.ToString());
              }
            }
          }
        }
        return _previewForm;
      }
    }
    private bool SyncViewEnabled => (_settings.SyncViewWithCaretPosition || _settings.SyncViewWithFirstVisibleLine);
    private bool IsPanelVisible { get; set; }

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

      settings.EnabledMarkdownPlugins = Win32.ReadIniValue("Options", "EnabledMarkdownPlugins", _iniFilePath, "tasks-list;attrs;qrcode;pano360")
        .Split(';')
        .Select(li => li.Trim())
        .ToArray();

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
      catch (Exception) { }
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
              if (_skipSyncEventsDue < DateTime.UtcNow) {
                ScrollToElementAtLineNo(_lastCaretPosition);
              }
            }
          }
          else if (IsPanelVisible && _settings.SyncViewWithFirstVisibleLine) {
            _ = SyncWithFirstVisibleLineTask();
          }
          break;
        }
        case (uint) NppMsg.NPPN_BUFFERACTIVATED: {
          if (_skipSyncEventsDue < DateTime.UtcNow) {
            RenderMarkdownDirect();
          }
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
          if (_skipSyncEventsDue < DateTime.UtcNow) {
            RenderMarkdownDeferred();
          }
          break;
        }
      }
    }

    private async Task SyncWithFirstVisibleLineTask()
    {
      await Task.Delay(50);
      var scintillaGateway = scintillaGatewayFactory();
      var currentPos = scintillaGateway.GetFirstVisibleLine();

      if (_currentFirstVisibleLine != currentPos) {
        _currentFirstVisibleLine = currentPos;
        if (_skipSyncEventsDue < DateTime.UtcNow) {
          var docLine = scintillaGateway.DocLineFromVisible(currentPos);
          ScrollToElementAtLineNo(docLine);
        }
      }
    }

    private void RenderMarkdownDeferred()
    {
      lock (_renderDeferredLock) {
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
        _currentFile = _nppGateway.GetCurrentFilePath();
        PreviewForm.RenderMarkdown(GetCurrentEditorText(), _currentFile);
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
        var currentFile = _nppGateway.GetCurrentFilePath();
        if (currentFile == _currentFile) {
          PreviewForm.ScrollToElementWithLineNo(lineNo);
        }
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
        _settings.EnabledMarkdownPlugins = settingsForm.AllowedMarkdownPlugins;

        _settings.IsDarkModeEnabled = IsDarkModeEnabled();
        SaveSettings();
        //Update Preview
        if (IsPanelVisible) {
          PreviewForm.UpdateSettings(_settings);
          RenderMarkdownDirect();
        }
      }
    }

    private void OpenFile(NavigateTo args)
    {
      if (!File.Exists(args.Filename)) {
        return;
      }
      Win32.SendMessage(PluginBase.nppData._nppHandle, (uint) NppMsg.NPPM_DOOPEN, 0, args.Filename);
    }

    private void PasteImage(PasteImage args)
    {
      var path = _nppGateway.GetCurrentFilePath();
      var rootDir = Path.GetDirectoryName(path);
      var targetDir = Path.Combine(rootDir, Path.GetDirectoryName(args.Filename));

      if (!Directory.Exists(targetDir)) {
        Directory.CreateDirectory(targetDir);
      }

      var extension = Path.GetExtension(args.Filename).ToLower().Substring(1);
      var sameFiles = Directory.GetFiles(targetDir, $"*.{extension}", SearchOption.TopDirectoryOnly);
      string filename = null;

      if (sameFiles.Length != 0) {
        using (var md5 = MD5.Create()) {
          var hash2 = string.Join("", md5.ComputeHash(args.Content).Select(li => $"{li}:X2"));
          foreach (var file in sameFiles) {
            var hash1 = string.Join("", md5.ComputeHash(File.ReadAllBytes(file)).Select(li => $"{li}:X2"));
            if (hash1 == hash2) {
              filename = file;
              break;
            }
          }
        }
      }

      if (filename == null) {
        var index = 10;
        while (true) {
          var fname = $"{index:D3}";
          if (Directory.GetFiles(targetDir, $"{fname}.*", SearchOption.TopDirectoryOnly).Length == 0) {
            break;
          }
          index += 5;
        }

        filename = (targetDir + $"/{index:D3}.{extension}").Replace("\\", "/");
        File.WriteAllBytes(filename, args.Content);
      }

      Uri rootUri = new Uri(rootDir + Path.DirectorySeparatorChar);
      Uri fileUri = new Uri(filename);
      var relativePath = rootUri.MakeRelativeUri(fileUri).ToString();

      var scintillaGateway = scintillaGatewayFactory();
      var pos = scintillaGateway.GetCurrentPos();
      scintillaGateway.InsertText(pos, $"![](./{relativePath})\r\n");
    }

    private void FirstLineChanged(FirstLineChanged args)
    {
      var scintillaGateway = scintillaGatewayFactory();
      var visibleLine = scintillaGateway.GetFirstVisibleLine();
      var docLine = scintillaGateway.DocLineFromVisible(visibleLine);
      var newVisibleLine = scintillaGateway.VisibleFromDocLine(args.Line);

      if (visibleLine != newVisibleLine) {
        scintillaGateway.LineScroll(0, newVisibleLine - visibleLine);
        _skipSyncEventsDue = DateTime.UtcNow.AddSeconds(1);
      }
    }

    private void DocumentChanged(DocumentContentChanged args)
    {
      var scintillaGateway = scintillaGatewayFactory();

      var currentTextLength = scintillaGateway.GetLength();
      var currentText = scintillaGateway.GetText(currentTextLength + 1);
      var newText = args.Content;
      if (newText == currentText) {
        return;
      }
      var len1 = currentText.Length;
      var len2 = newText.Length;

      var differ = new Differ();
      var diff = differ.CreateCharacterDiffs(currentText, newText, false);
      _skipSyncEventsDue = DateTime.UtcNow.AddSeconds(600);
      scintillaGateway.BeginUndoAction();
      try {
        foreach (var block in diff.DiffBlocks.Reverse()) {
          int deleteStartA = block.DeleteStartA;
          for (var i = 0; i < block.DeleteStartA; i++) {
            var ch = currentText[i];
            if (char.IsHighSurrogate(ch)) {
              deleteStartA += 2;
              i++;
            }
            else if (ch >= 0x800) {
              deleteStartA += 2;
            }
            else if (ch >= 0x80) {
              deleteStartA += 1;
            }
          }

          if (block.DeleteCountA > 0) {
            scintillaGateway.DeleteRange(deleteStartA, block.DeleteCountA);
          }
          if (block.InsertCountB > 0) {
            var insertText = newText.Substring(block.InsertStartB, block.InsertCountB).Replace("\n", "\r\n");
            scintillaGateway.InsertText(deleteStartA, insertText);
          }
        }
      }
      finally {
        scintillaGateway.EndUndoAction();
        _skipSyncEventsDue = DateTime.UtcNow.AddSeconds(0.5);
      }
    }

    private void ShowHelp()
    {
      var currentPluginPath = PluginUtils.GetPluginDirectory();
      var helpFile = Path.Combine(currentPluginPath, "README.md");
      Win32.SendMessage(PluginBase.nppData._nppHandle, (uint) NppMsg.NPPM_DOOPEN, 0, helpFile);
      if (!IsPanelVisible) {
        TogglePanelVisible();
      }

      RenderMarkdownDirect();
    }

    private void SetIniFilePath()
    {
      StringBuilder sbIniFilePath = new StringBuilder(Win32.MAX_PATH);
      Win32.SendMessage(PluginBase.nppData._nppHandle, (uint) NppMsg.NPPM_GETPLUGINSCONFIGDIR, Win32.MAX_PATH, sbIniFilePath);
      _iniFilePath = sbIniFilePath.ToString();
      if (!Directory.Exists(_iniFilePath)) {
        Directory.CreateDirectory(_iniFilePath);
      }

      _iniFilePath = Path.Combine(_iniFilePath, Main.ModuleName + ".ini");
    }

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

      Win32.CheckMenuItem(Win32.GetMenu(PluginBase.nppData._nppHandle), PluginBase._funcItems.Items[2]._cmdID, Win32.MF_BYCOMMAND
        | (enabled ? Win32.MF_CHECKED : Win32.MF_UNCHECKED));
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
      Win32.CheckMenuItem(Win32.GetMenu(PluginBase.nppData._nppHandle), PluginBase._funcItems.Items[3]._cmdID, Win32.MF_BYCOMMAND
        | (enabled ? Win32.MF_CHECKED : Win32.MF_UNCHECKED));
    }

    public void SetToolBarIcon()
    {
      toolbarIcons tbIconsOld = new toolbarIcons();
      tbIconsOld.hToolbarBmp = Resources.markdown_16x16_solid.GetHbitmap();
      tbIconsOld.hToolbarIcon = Resources.markdown_16x16_solid_dark.GetHicon();
      IntPtr pTbIcons = Marshal.AllocHGlobal(Marshal.SizeOf(tbIconsOld));
      Marshal.StructureToPtr(tbIconsOld, pTbIcons, false);
      Win32.SendMessage(PluginBase.nppData._nppHandle, (uint) NppMsg.NPPM_ADDTOOLBARICON, PluginBase._funcItems.Items[_myDlgId]._cmdID, pTbIcons);
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
      Win32.WritePrivateProfileString("Options", "EnabledMarkdownPlugins", string.Join(";", _settings.EnabledMarkdownPlugins), _iniFilePath);

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

    private bool IsDarkModeEnabled()
    {
      // NPPM_ISDARKMODEENABLED (NPPMSG + 107)
      IntPtr ret = Win32.SendMessage(PluginBase.nppData._nppHandle, (uint) (Constants.NPPMSG + 107), UNUSED, UNUSED);
      return ret.ToInt32() == 1;
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

    private const int UNUSED = 0;

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
    private DateTime _skipSyncEventsDue = DateTime.MinValue;
    private string _currentFile;

    private static readonly TimeSpan InputUpdateThreshold = TimeSpan.FromMilliseconds(400);
  }
}
