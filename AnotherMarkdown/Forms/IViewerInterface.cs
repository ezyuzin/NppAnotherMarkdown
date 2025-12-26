using System;
using AnotherMarkdown.Entities;

namespace AnotherMarkdown.Forms
{
  public interface IViewerInterface
  {
    IntPtr Handle { get; }
    void SetMarkdownFilePath(string filepath);
    void UpdateSettings(Settings settings);
    void RenderMarkdown(string currentText, string filepath);
    void ScrollToElementWithLineNo(int lineNo);
    bool IsValidFileExtension(string filename);
  }
}
