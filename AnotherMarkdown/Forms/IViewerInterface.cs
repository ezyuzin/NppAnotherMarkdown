using AnotherMarkdown.Entities;
using System;

namespace AnotherMarkdown.Forms
{
  public interface IViewerInterface
  {
    IntPtr Handle { get; }
    void UpdateSettings(Settings settings);
    void RenderMarkdown(string currentText, string filepath);
    void ScrollToElementWithLineNo(int lineNo);
  }
}
