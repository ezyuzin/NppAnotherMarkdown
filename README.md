# AnotherMarkdown for Notepad++

A plugin for previewing Markdown files in Notepad++.

* Lightweight plugin to preview Markdown within Notepad++
* Displays rendered Markdown HTML using **WebView2 (Edge)**

The plugin is a fork of the [NppMarkdownPanel plugin](https://github.com/mohzy83/NppMarkdownPanel) and uses the [markdown-it](https://github.com/markdown-it/markdown-it) javascript library to render Markdown documents into HTML and allows configuring used Markdown extensions **without recompiling or reinstalling the plugin** (just edit `assets/markdown/markdown.js`).

* Editing and preview interactive 360-degree panoramic photos into Markdown. Extension uses [panellum](https://github.com/mpetroff/pannellum) library and invokes with markdown markup syntax `{% pano360 path_to_scene %}`. Example can be found [here](https://github.com/ezyuzin/NppAnotherMarkdown/tree/master/example/pano).  
  Open index.pano360.json in Notepad++ for editing, the AnotherMarkdown preview makes scene editing easier.
  
* Displaying QR codes
`{% qrcode text="12345" %}`

![](example/pano/preview.gif)


### Current Version

The current version can be found [here](https://github.com/ezyuzin/NppAnotherMarkdown/releases)

{% qrcode text="https://github.com/ezyuzin/NppAnotherMarkdown/releases" %}


## Prerequisites
- Windows
- .NET 4.7.2 or higher

## Installation
#### Installation in Notepad++ 
The plugin can be installed with the Notepad++ Plugin Admin.
The name of the plugin is **AnotherMarkdown**.

#### Manual Installation
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

* #### Show Toolbar in Preview Window
    Checking this box will enable the toolbar in the preview window. By default, this is unchecked.

* #### Show Statusbar in Preview Window (Preview Links)
    Checking this box will show the status bar, which previews urls for links. By default, this is unchecked.

### Synchronize viewer with caret position

Enabling this in the plugin's menu (Plugins -> AnotherMarkdown) makes the preview panel stay in sync with the caret in the markdown document that is being edited.  
This is similar to the _Synchronize Vertical Scrolling_ option of Notepad++ for keeping two open editing panels scrolling together.

### Synchronize with first visible line in editor

When this option is enabled, the plugin ensures that the first visible line in the 
editor is also visible in the preview. (This is an alternative to _Synchronize viewer with caret position_)

## Used libs and resources

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
