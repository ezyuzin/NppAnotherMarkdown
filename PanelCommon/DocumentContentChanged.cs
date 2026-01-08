using System.Runtime.InteropServices;

namespace PanelCommon
{
  [StructLayout(LayoutKind.Sequential)]
  public struct DocumentContentChanged
  {
    public string Content;
  }
}
