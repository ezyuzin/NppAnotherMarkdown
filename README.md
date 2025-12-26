# AnotherMarkdown for Notepad++

A plugin for previewing Markdown files in Notepad++.

* Lightweight plugin to preview Markdown within Notepad++
* Displays rendered Markdown HTML using **WebView2 (Edge)**

The plugin is a fork of the [NppMarkdownPanel plugin](https://github.com/mohzy83/NppMarkdownPanel).

The plugin uses the [markdown-it](https://github.com/markdown-it/markdown-it) library to render Markdown documents into HTML. The advantage of this library is that it is client-side and allows configuring Markdown by enabling or disabling required rendering options **without recompiling or reinstalling the plugin**.

In addition to the CSS file for the Markdown document, you can customize either the loader (`assets/markdown/loader.html`) or the file that directly renders Markdown (`assets/markdown/markdown.js`).

The script format is compatible with the Lister plugin [EdgeViewer](https://github.com/rg-software/wlx-edge-viewer) for viewing Markdown documents in Total Commander. It is also possible to specify a direct path to the EdgeViewer plugin configuration directory to synchronize rendering settings.


### Current Version

The current version is **0.1.0** it can be found [here](https://github.com/ezyuzin/NppAnotherMarkdown/releases)

## Prerequisites
- Windows
- .NET 4.7.2 or higher

## Installation
### Installation in Notepad++ 
The plugin can be installed with the Notepad++ Plugin Admin.
The name of the plugin is **AnotherMarkdown**.

### Manual Installation
Create the folder "AnotherMarkdown" in your Notepad++ plugin folder (e.g. "C:\Program Files\Notepad++\plugins") and extract the appropriate zip (x86 or x64) to it.

It should look like this:  

**Issues with manual installation:**
Windows blocks downloaded DLLs by default. That means you likely get the following error message: 

> Failed to load <br>
> AnotherMarkdown.dll is not compatible with the current version of Notepad++
	
Make sure to unblock __all__ DLLs of the plugin (also DLLs in subfolders).
![npp-unblock](help/npp-unblock.png "Unblock all DLLs")

**Note for Windows 7 users:**
 WebView2 Edge is required for the plugin to function properly. 
 Windows 7 does not include WebView2 Edge by default, so you must manually install the WebView2 Runtime from Microsoft's WebView2 download page before using the plugin.
 https://developer.microsoft.com/en-us/microsoft-edge/webview2?form=MA13LH#download
## Usage

After the installation you will find a small purple markdown icon in your toolbar.
Just click it to show the markdown preview. Click again to hide the preview.
Thats all you need to do ;)

With dark mode enabled in Notepad++

### Settings

To open the settings for this plugin: Plugins -> AnotherMarkdown -> Settings

* #### CSS File
    This allows you to select a CSS file to use if you don't want the default style of the preview
	
* #### Dark mode CSS File
	This allows you to select a Dark mode CSS file. When the Notepad++ dark mode is enabled, this Css file is used.
	When no file is set, the default dark mode Css is used.

* #### Zoom Level
    This allows you to set the zoom level of the preview

* #### Automatic HTML Output
    This allows you to select a file to save the rendered HTML to every time the preview is rendered. This is a way to automatically save the rendered content to use elsewhere. Leaving this empty disables the automatic saving.  
    __Note: This is a global setting, so all previewed documents will save to the same file.__

* #### Allow all file extensions
   This option allows you to skip file extension checking. Every active file will be processed by the markdown converter.
   But be careful, this option may have undesired effects. (e.g. rendering large logs or large source code files can be slow)
   The input field for supported file extensions is disabled when this option is checked.

* #### Supported File Extensions
    This allows you to define a list of file extensions, which are supported and displayed in Markdown Panel.
	Other file type won't be displayed (there will be a warning).
	The file extensions have to be separated by a comma `,` - character.
	No input allowed when option "Allow all file extensions" is checked.

* #### Automatically show panel for supported files
    When this option is checked, Markdown Panel will open the preview window automatically for files with a supported extension.
	The preview will be closed for files with no supported extension.
	

* #### Show Toolbar in Preview Window
    Checking this box will enable the toolbar in the preview window. By default, this is unchecked.

* #### Show Statusbar in Preview Window (Preview Links)
    Checking this box will show the status bar, which previews urls for links. By default, this is unchecked.


### Preview Window Toolbar

* #### Save As... ![save-btn](help/save-btn.png)
    Clicking this button allows you to save the rendered preview as an HTML document.

### Synchronize viewer with caret position

Enabling this in the plugin's menu (Plugins -> AnotherMarkdown) makes the preview panel stay in sync with the caret in the markdown document that is being edited.  
This is similar to the _Synchronize Vertical Scrolling_ option of Notepad++ for keeping two open editing panels scrolling together.

### Synchronize with first visible line in editor

When this option is enabled, the plugin ensures that the first visible line in the 
editor is also visible in the preview. (This is an alternative to _Synchronize viewer with caret position_)

## Version History
### Version 0.1.0 (released 2025-12-26)

* Removed support for IE11
* Removed support for the MarkdownDig library
* Markdown rendering using the [markdown-it](https://github.com/markdown-it/markdown-it) library
* Added a plugin for displaying panoramic photos; the plugin is not part of the Markdown standard
  `{% pano360 %}`
* Added a plugin for displaying QR codes; the plugin is not part of the Markdown standard
  `{% qrcode text="12345" %}`
* Fixes for more accurate positioning in the viewer when changing the caret position or the first line



### Used libs and resources

| Name                              | Version     | Authors       | Link                                                                                                                   |
|-----------------------------------|-------------|---------------|------------------------------------------------------------------------------------------------------------------------|
| NotepadPlusPlusPluginPack.Net     | 0.95    	  | kbilsted      | [https://github.com/kbilsted/NotepadPlusPlusPluginPack.Net](https://github.com/kbilsted/NotepadPlusPlusPluginPack.Net) |
| NppMarkdownPanel                  | 0.9.0    	  | mohzy83       | [https://github.com/mohzy83/NppMarkdownPanel](https://github.com/mohzy83/NppMarkdownPanel) |
| EdgeViewer                        | 1.0.9    	  | rg-software   | [https://github.com/rg-software/wlx-edge-viewer](https://github.com/rg-software/wlx-edge-viewer) |
| WebView2 Edge				              | 1.0.3296.44 | Microsoft     | [https://developer.microsoft.com/de-de/microsoft-edge/webview2?form=MA13LH](https://developer.microsoft.com/de-de/microsoft-edge/webview2?form=MA13LH) |
| github-markdown-css               | 3.0.1       | sindresorhus  | [https://github.com/sindresorhus/github-markdown-css](https://github.com/sindresorhus/github-markdown-css)             |
| Markdown Icon                     |             | dcurtis       | [https://github.com/dcurtis/markdown-mark](https://github.com/dcurtis/markdown-mark)                                   |

### Contributors

## License

This project is licensed under the MIT License - see the LICENSE.txt file for details
