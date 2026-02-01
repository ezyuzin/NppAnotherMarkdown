using System.Runtime.InteropServices;

namespace PanelCommon
{
  [StructLayout(LayoutKind.Sequential)]
  public struct DocumentChangedEvent
  {
    public string Content;
  }
}
