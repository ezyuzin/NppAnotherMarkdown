## Version History

### NppAnotherMarkdown 0.1.6 (released 2026-01-15)

- Support drag-and-drop and CTRL+V insert images into markdown preview.  
Markup `![](image path)` inserted into current cursor position. 
Image naming is starts from 010.ext and incrementing with step 5, ie 010.jpg 015.jpg, 020.jpg, and each image stored in folder "./img" where markdown document located.  
Location can be changed by edit [assets/markdown/markdown.js]().

- Added navigation over existing Markdown files when clicking a link to such a file in the preview window; previous behavior: such navigation was ignored.
- Preserve preview position after switch between documents
- highlight.js: code syntax highlight plugin added. Enable it via settings  
![](help/plugin-highlight.jpg)

### NppAnotherMarkdown 0.1.5 (released 2026-01-12)
* Markdown Plugin Pack added

| plugin        | description |
|---------------|-------------|
| [abbr](https://mdit-plugins.github.io/abbr.html)               | Support abbreviation tag \<abbr\> |
| [alert](https://mdit-plugins.github.io/alert.html)             | GFM style alerts |
| [align](https://mdit-plugins.github.io/align.html)             | Plugin to align contents |
| [attrs](https://mdit-plugins.github.io/attrs.html)             | Add attrs to Markdown content |
| [container](https://mdit-plugins.github.io/container.html)     | Creating block-level custom containers |
| [dl](https://mdit-plugins.github.io/dl.html)                   | Definition list |
| [emoji](https://github.com/markdown-it/markdown-it-emoji)      | Emoji |
| [figure](https://mdit-plugins.github.io/figure.html)           | Generating figures with captions from images |
| [footnote](https://mdit-plugins.github.io/footnote.html)       | Footnotes |
| [icon](https://mdit-plugins.github.io/icon.html)               | Icons |
| [imgLazyLoad](https://mdit-plugins.github.io/img-lazyload.html)| Lazy loading for images |
| [imgMark](https://mdit-plugins.github.io/img-mark.html)        | Mark images by ID suffix for theme mode |
| [imgSize](https://mdit-plugins.github.io/img-size.html)        | Support setting size for images |
| [ins](https://mdit-plugins.github.io/ins.html)                 | Аdd \<insert\> tag support |
| [katex](https://mdit-plugins.github.io/katex.html)             | Math Expressions<br> ![](help/plugin-katex.jpg) |
| [mark](https://mdit-plugins.github.io/mark.html)               | Mark and highlight contents |
| [plantuml](https://mdit-plugins.github.io/plantuml.html)       | Support plant uml schemes |
| [ruby](https://mdit-plugins.github.io/ruby.html)               | Ruby annotation \<ruby\> |
| [spoiler](https://mdit-plugins.github.io/spoiler.html)         | Plugin to hide content |
| [stylize](https://mdit-plugins.github.io/stylize.html)         | Plugin for stylizing tokens |
| [sub](https://mdit-plugins.github.io/sub.html)                 | Plugin to support subscript |
| [sup](https://mdit-plugins.github.io/sup.html)                 | Plugin to support superscript |
| [tab](https://mdit-plugins.github.io/tab.html)                 | Block-level custom tabs |


### NppAnotherMarkdown 0.1.4 (released 2026-01-08)
* Syncing view for both window (text and markdown preview) when "Sync with first visible line" enabled.
![](example/sync-both.gif)

### NppAnotherMarkdown 0.1.3 (released 2026-01-06)
* Editable tasklists, (bi-direction sync)

Fixes:
- [x] fix: reduce flickering panorama during text editing
- [x] fix: another attempt to make more accurate positioning in the viewer when changing the caret position or the first line  

![](example/tasklist.gif)

### NppAnotherMarkdown 0.1.2 (released 2026-01-03)
Fixes:
- [x] minor, but possible memory leaks

### NppAnotherMarkdown 0.1.1 (released 2025-12-30)
* Scene editor for 360 panoramic photos.
* 360 pano scene example  
![](example/pano/preview.gif)

Fixes:
- [x] some memory leaks
- [x] scrolling, positioning in the viewer when changing the caret position

### NppAnotherMarkdown 0.1.0 (released 2025-12-26)
* Removed support for IE11
* Removed support for the MarkdownDig library
* Markdown rendering using the [markdown-it](https://github.com/markdown-it/markdown-it) library
* Added markup for displaying panoramic photos: `{% pano360 %}`
* Added markup for displaying QR codes: `{% qrcode text="12345" %}`
* More accurate positioning in the viewer when changing the caret position or the first line