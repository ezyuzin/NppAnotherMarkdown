using System.Runtime.InteropServices;

namespace PanelCommon
{
  [StructLayout(LayoutKind.Sequential)]
  public struct FirstLineChangedEvent
  {
    public int Line;
  }
}
