## Version History

### NppAnotherMarkdown 0.1.5 (released 2026-01-??)

Euler’s identity $e^{i\pi}+1=0$ is a beautiful formula in $\mathbb{R}^2$.

$$ S = S_0 + V \cdot t + \frac {a \cdot t ^2} {2}$$

$$
\frac {\partial^r} {\partial \omega^r} \left(\frac {y^{\omega}} {\omega}\right)
= \left(\frac {y^{\omega}} {\omega}\right) \left\{(\log y)^r + \sum_{i=1}^r \frac {(-1)^ Ir \cdots (r-i+1) (\log y)^{ri}} {\omega^i} \right\}
$$


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

