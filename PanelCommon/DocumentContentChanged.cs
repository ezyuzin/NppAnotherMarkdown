using System.Runtime.InteropServices;

namespace PanelCommon
{
  [StructLayout(LayoutKind.Sequential)]
  public struct DocumentContentChanged
  {
    public string Content;
  }

  [StructLayout(LayoutKind.Sequential)]
  public struct FirstLineChanged
  {
    public int Line;
  }
}
