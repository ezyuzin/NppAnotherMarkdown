using System;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace AnotherMarkdown.Forms
{
  public partial class AboutForm : Form
  {
    public AboutForm()
    {
      InitializeComponent();
      var versionString = "0.X";
      try {
        Version version = Assembly.GetExecutingAssembly().GetName().Version;
        versionString = version.ToString();
      }
      catch (Exception) { }
      tbAbout.Text = string.Format(AboutDialogText, versionString);
      btnOk.Focus();
      ActiveControl = btnOk;
    }

    private const string AboutDialogText =
      @"AnotherMarkdown for Notepad++
Version {0}

Created by Evgeny Zyuzin 2025

Github: https://github.com/ezyuzin/AnotherMarkdown

Used Libs and Resources:

NotepadPlusPlusPluginPack.Net 0.95.00 by kbilsted - https://github.com/kbilsted/NotepadPlusPlusPluginPack.Net
WebView2 Edge 1.0.3296.44 by Microsoft - https://developer.microsoft.com/de-de/microsoft-edge/webview2?form=MA13LH
github-markdown-css 3.0.1 by sindresorhus - \r\nhttps://github.com/sindresorhus/github-markdown-css
Markdown icon by dcurtis - https://github.com/dcurtis/markdown-mark
EdgeViewer by rg-software - https://github.com/rg-software/wlx-edge-viewer

The plugin uses portions of NppMarkdownPanel Plugin code - https://github.com/mohzy83/NppMarkdownPanel
The plugin uses portions of MarkdownViewerPlusPlus Plugin code - https://github.com/nea/MarkdownViewerPlusPlus";
  }
}
