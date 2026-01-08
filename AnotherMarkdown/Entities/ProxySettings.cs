using PanelCommon;

namespace AnotherMarkdown.Entities
{
  public class ProxySettings : ISettings
  {
    public ProxySettings(Settings s)
    {
      _s = s;
    }

    public bool SyncViewWithCaretPosition => _s.SyncViewWithCaretPosition;
    public bool SyncViewWithFirstVisibleLine => _s.SyncViewWithFirstVisibleLine;
    public string AssetsPath => _s.AssetsPath;
    public string CssFileName => _s.CssFileName;
    public string CssDarkModeFileName => _s.CssDarkModeFileName;
    public int ZoomLevel => _s.ZoomLevel;
    public bool IsDarkModeEnabled => _s.IsDarkModeEnabled;
    public bool ShowToolbar => _s.ShowToolbar;
    public bool ShowStatusbar => _s.ShowStatusbar;

    public string DefaultAssetPath => _s.DefaultAssetPath;
    public string DefaultCssFile => _s.DefaultCssFile;
    public string DefaultDarkModeCssFile => _s.DefaultDarkModeCssFile;

    private readonly Settings _s;
  }
}
