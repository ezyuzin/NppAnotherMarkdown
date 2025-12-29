using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PanelCommon
{
  [StructLayout(LayoutKind.Sequential)]
  public struct DocumentContentChanged
  {
    public string Content;
  }

  public interface IWebbrowserControl
  {
    Action<string> StatusTextChangedAction { get; set; }
    Action RenderingDoneAction { get; set; }

    EventHandler<DocumentContentChanged> DocumentChanged { get; set; }

    void Initialize(int zoomLevel);
    void AddToHost(Control host);
    void PrepareContentUpdate(bool preserveVerticalScrollPosition);
    Task SetContent(string content, string documentPath, string assetsPath, string customCssFile, bool syncView);
    Task SetZoomLevel(int zoomLevel);
    void ScrollToElementWithLineNo(int lineNo);
    string GetRenderingEngineName();
    Bitmap MakeScreenshot();
  }
}
