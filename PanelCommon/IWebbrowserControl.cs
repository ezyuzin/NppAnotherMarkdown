using System;
using System.Drawing;
using System.Windows.Forms;

namespace PanelCommon
{
  public interface IWebbrowserControl
  {
    Action<string> StatusTextChangedAction { get; set; }
    Action RenderingDoneAction { get; set; }

    void Initialize(int zoomLevel);
    void AddToHost(Control host);
    void PrepareContentUpdate(bool preserveVerticalScrollPosition);
    void SetContent(string content, string documentPath, string assetsPath, string customCssFile, bool syncView);
    void SetZoomLevel(int zoomLevel);
    void ScrollToElementWithLineNo(int lineNo);
    string GetRenderingEngineName();
    Bitmap MakeScreenshot();
  }
}
