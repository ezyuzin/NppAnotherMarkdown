using System;
using System.Linq;
using Kbg.NppPluginNET.PluginInfrastructure;

namespace AnotherMarkdown
{
  class Main
  {
    public static void OnNotification(ScNotification notification)
    {
      mdpanel.OnNotification(notification);
    }

    internal static void CommandMenuInit()
    {
      mdpanel.InitCommandMenu();
    }

    internal static void SetToolBarIcon()
    {
      mdpanel.SetToolBarIcon();
    }

    internal static void PluginCleanUp()
    {
      mdpanel.PluginCleanUp();
      mdpanel.Dispose();
      mdpanel = null;
    }

    // PluginName is used as npp plugin's menu entry
    public const string PluginName = "AnotherMarkdown";
    // Modulename is used as config name (ini-file) and as _nppTbData.pszModuleName
    public const string ModuleName = "AnotherMarkdown";
    public const string PluginTitle = "AnotherMarkdown";

    private static MarkdownPanelController mdpanel = new MarkdownPanelController();
  }
}