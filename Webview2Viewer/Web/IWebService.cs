using Microsoft.Web.WebView2.Core;

namespace Webview2Viewer.Web
{
  internal interface IWebService
  {
    string Hostname { get; }
    bool HandleRequest(CoreWebView2WebResourceRequestedEventArgs e);
  }
}
