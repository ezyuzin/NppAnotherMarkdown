// NPP plugin platform for .Net v0.94.00 by Kasper B. Graversen etc.
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace Kbg.NppPluginNET.PluginInfrastructure
{
  [StructLayout(LayoutKind.Sequential)]
  public struct NppData
  {
    public IntPtr _nppHandle;
    public IntPtr _scintillaMainHandle;
    public IntPtr _scintillaSecondHandle;
  }

    public delegate void NppFuncItemDelegate();

  [StructLayout(LayoutKind.Sequential)]
  public struct ShortcutKey
  {
    public byte _isCtrl;
    public byte _isAlt;
    public byte _isShift;
    public byte _key;

    public ShortcutKey(bool isCtrl, bool isAlt, bool isShift, Keys key)
    {
      // the types 'bool' and 'char' have a size of 1 byte only!
      _isCtrl = Convert.ToByte(isCtrl);
      _isAlt = Convert.ToByte(isAlt);
      _isShift = Convert.ToByte(isShift);
      _key = Convert.ToByte(key);
    }
  }

  [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
  public struct FuncItem
  {
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string _itemName;
    public NppFuncItemDelegate _pFunc;
    public int _cmdID;
    public bool _init2Check;
    public ShortcutKey _pShKey;
  }

  public class FuncItems : IDisposable
  {
    public IntPtr NativePointer { get; private set; }
    public List<FuncItem> Items { get; }

    public FuncItems()
    {
      Items = new List<FuncItem>();
      _sizeFuncItem = Marshal.SizeOf(typeof(FuncItem));
      _shortCutKeys = new List<IntPtr>();
    }

    [DllImport("kernel32")]
    static extern void RtlMoveMemory(IntPtr Destination, IntPtr Source, int Length);

    public void Add(FuncItem funcItem)
    {
      int oldSize = Items.Count * _sizeFuncItem;
      Items.Add(funcItem);
      int newSize = Items.Count * _sizeFuncItem;
      IntPtr newPointer = Marshal.AllocHGlobal(newSize);

      if (NativePointer != IntPtr.Zero) {
        RtlMoveMemory(newPointer, NativePointer, oldSize);
        Marshal.FreeHGlobal(NativePointer);
      }
      IntPtr ptrPosNewItem = (IntPtr)(newPointer.ToInt64() + oldSize);
      byte[] aB = Encoding.Unicode.GetBytes(funcItem._itemName + "\0");
      Marshal.Copy(aB, 0, ptrPosNewItem, aB.Length);
      ptrPosNewItem = (IntPtr)(ptrPosNewItem.ToInt64() + 128);
      IntPtr p = (funcItem._pFunc != null) ? Marshal.GetFunctionPointerForDelegate(funcItem._pFunc) : IntPtr.Zero;
      Marshal.WriteIntPtr(ptrPosNewItem, p);
      ptrPosNewItem = (IntPtr)(ptrPosNewItem.ToInt64() + IntPtr.Size);
      Marshal.WriteInt32(ptrPosNewItem, funcItem._cmdID);
      ptrPosNewItem = (IntPtr)(ptrPosNewItem.ToInt64() + 4);
      Marshal.WriteInt32(ptrPosNewItem, Convert.ToInt32(funcItem._init2Check));
      ptrPosNewItem = (IntPtr)(ptrPosNewItem.ToInt64() + 4);
      if (funcItem._pShKey._key != 0) {
        IntPtr newShortCutKey = Marshal.AllocHGlobal(4);
        Marshal.StructureToPtr(funcItem._pShKey, newShortCutKey, false);
        Marshal.WriteIntPtr(ptrPosNewItem, newShortCutKey);
      }
      else {
        Marshal.WriteIntPtr(ptrPosNewItem, IntPtr.Zero);
      }

      NativePointer = newPointer;
    }

    public void RefreshItems()
    {
      IntPtr ptrPosItem = NativePointer;
      for (int i = 0; i < Items.Count; i++) {
        FuncItem updatedItem = new FuncItem();
        updatedItem._itemName = Items[i]._itemName;
        ptrPosItem = (IntPtr)(ptrPosItem.ToInt64() + 128);
        updatedItem._pFunc = Items[i]._pFunc;
        ptrPosItem = (IntPtr)(ptrPosItem.ToInt64() + IntPtr.Size);
        updatedItem._cmdID = Marshal.ReadInt32(ptrPosItem);
        ptrPosItem = (IntPtr)(ptrPosItem.ToInt64() + 4);
        updatedItem._init2Check = Items[i]._init2Check;
        ptrPosItem = (IntPtr)(ptrPosItem.ToInt64() + 4);
        updatedItem._pShKey = Items[i]._pShKey;
        ptrPosItem = (IntPtr)(ptrPosItem.ToInt64() + IntPtr.Size);

        Items[i] = updatedItem;
      }
    }

    public void Dispose()
    {
      if (!_disposed) {
        foreach (IntPtr ptr in _shortCutKeys) {
          Marshal.FreeHGlobal(ptr);
        }

        if (NativePointer != IntPtr.Zero) {
          Marshal.FreeHGlobal(NativePointer);
        }

        _disposed = true;
      }
    }

    int _sizeFuncItem;
    List<IntPtr> _shortCutKeys;
    bool _disposed = false;

    ~FuncItems()
    {
      Dispose();
    }
  }

  public enum winVer
  {
    WV_UNKNOWN,
    WV_WIN32S,
    WV_95,
    WV_98,
    WV_ME,
    WV_NT,
    WV_W2K,
    WV_XP,
    WV_S2003,
    WV_XPX64,
    WV_VISTA,
    WV_WIN7,
    WV_WIN8,
    WV_WIN81,
    WV_WIN10
  }

  [Flags]
  public enum DockMgrMsg : uint
  {
    IDB_CLOSE_DOWN = 137,
    IDB_CLOSE_UP = 138,
    IDD_CONTAINER_DLG = 139,

    IDC_TAB_CONT = 1027,
    IDC_CLIENT_TAB = 1028,
    IDC_BTN_CAPTION = 1050,

    DMM_MSG = 0x5000,
    DMM_CLOSE = (DMM_MSG + 1),
    DMM_DOCK = (DMM_MSG + 2),
    DMM_FLOAT = (DMM_MSG + 3),
    DMM_DOCKALL = (DMM_MSG + 4),
    DMM_FLOATALL = (DMM_MSG + 5),
    DMM_MOVE = (DMM_MSG + 6),
    DMM_UPDATEDISPINFO = (DMM_MSG + 7),
    DMM_GETIMAGELIST = (DMM_MSG + 8),
    DMM_GETICONPOS = (DMM_MSG + 9),
    DMM_DROPDATA = (DMM_MSG + 10),
    DMM_MOVE_SPLITTER = (DMM_MSG + 11),
    DMM_CANCEL_MOVE = (DMM_MSG + 12),
    DMM_LBUTTONUP = (DMM_MSG + 13),

    DMN_FIRST = 1050,
    DMN_CLOSE = (DMN_FIRST + 1),
    //nmhdr.Code = DWORD(DMN_CLOSE, 0));
    //nmhdr.hwndFrom = hwndNpp;
    //nmhdr.IdFrom = ctrlIdNpp;

    DMN_DOCK = (DMN_FIRST + 2),
    DMN_FLOAT = (DMN_FIRST + 3)
    //nmhdr.Code = DWORD(DMN_XXX, int newContainer);
    //nmhdr.hwndFrom = hwndNpp;
    //nmhdr.IdFrom = ctrlIdNpp;
  }

  [StructLayout(LayoutKind.Sequential)]
  public struct toolbarIcons
  {
    public IntPtr hToolbarBmp;			// standard icon (color)
    public IntPtr hToolbarIcon;			// Fluent UI icon (black)
    public IntPtr hToolbarIconDarkMode;	// Fluent UI icon (white)
  }
}
