using System.IO;
using System.Reflection;

namespace AnotherMarkdown.Entities
{
  public class Settings
  {
    public bool SyncViewWithCaretPosition { get; set; } = false;
    public bool SyncViewWithFirstVisibleLine { get; set; } = false;
    public string AssetsPath { get; set; }
    public string CssFileName { get; set; }
    public string CssDarkModeFileName { get; set; }
    public int ZoomLevel { get; set; }
    public string SupportedFileExt { get; set; }
    public bool AllowAllExtensions { get; set; }
    public bool IsDarkModeEnabled { get; set; }
    public bool ShowToolbar { get; set; }
    public bool ShowStatusbar { get; set; }
    public bool AutoShowPanel { get; set; }
    public string PreProcessorCommandFilename { get; set; }
    public string PreProcessorArguments { get; set; }
    public string PostProcessorCommandFilename { get; set; }
    public string PostProcessorArguments { get; set; }
    public string RenderingEngine { get; set; }

    public static string DefaultCssFile => DefaultAssetPath + "/markdown/markdown.css";
    public static string DefaultDarkModeCssFile => DefaultAssetPath + "/markdown/markdown-dark.css";
    public const string DEFAULT_SUPPORTED_FILE_EXT = "md,mkd,mdwn,mdown,mdtxt,markdown,txt";

    public static string DefaultAssetPath => (Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "/assets").Replace("\\", "/");
  }
}
