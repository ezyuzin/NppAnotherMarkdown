using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Web.WebView2.Core;
using Newtonsoft.Json;
using PanelCommon;

namespace Webview2Viewer.Web
{
  internal class LocalFileService: IWebService
  {
    public EventHandler<DocumentContentChanged> DocumentChanged { get; set; }
    public string DocumentUri { get; private set; }
    public string DocumentPath { get; private set; }
    public string DocumentContent { get; set; }
    public string Hostname { get; }

    public LocalFileService(CoreWebView2Environment environment, string host)
    {
      _httpEnvironment = environment;
      Hostname = host;
    }

    public void SetContent(string documentPath, string content)
    {
      DocumentPath = documentPath;
      DocumentUri = "/" + HttpUtility2.PathToUri(documentPath);
      DocumentContent = content;
      _documentBytesData = Encoding.UTF8.GetBytes(DocumentContent);
    }

    public bool HandleRequest(CoreWebView2WebResourceRequestedEventArgs e)
    {
      if (e.Response != null) {
        return false;
      }
      if (e.Request.Method == "GET") {
        var uri = new Uri(e.Request.Uri);
        HttpGetContent(e, uri);
        return true;
      }
      else if (e.Request.Method == "PUT") {
        var uri = new Uri(e.Request.Uri);
        HttpPutContent(e, uri);
        return true;
      }
      else if (e.Request.Method == "OPTIONS") {
        var uri = new Uri(e.Request.Uri);
        HttpOptionsContent(e, uri);
        return true;
      }
      return false;
    }

    private void HttpPutContent(CoreWebView2WebResourceRequestedEventArgs e, Uri requestUri)
    {
      if (!DocumentUri.Equals(requestUri.AbsolutePath, StringComparison.InvariantCultureIgnoreCase)) {
        Error404(e);
        return;
      }

      var headers = new List<string> {
        "Access-Control-Allow-Origin: *"
      };

      string requestBody;
      using (var reader = new StreamReader(e.Request.Content, Encoding.UTF8)) {
        requestBody = reader.ReadToEnd();
      }

      e.Response = _httpEnvironment.CreateWebResourceResponse(new MemoryStream(), 200, "OK", string.Join("\r\n", headers));

      new Task(() => DocumentChanged?.Invoke(this, new DocumentContentChanged {
        Content = requestBody
      })).Start();
    }

    private void HttpGetContent(CoreWebView2WebResourceRequestedEventArgs e, Uri requestUri)
    {
      var charset = "";
      byte[] bytes = null;
      var headers = new List<string> {
        "Access-Control-Allow-Origin: *"
      };

      if (Regex.IsMatch(requestUri.AbsolutePath, @"\*\.(\*|.+)$")) {
        var path = HttpUtility2.UriToPath(requestUri.AbsolutePath);
        var mask = Path.GetFileName(path);

        var dir = Path.GetDirectoryName(path);
        if (Directory.Exists(dir)) {
          var files = Directory.GetFiles(dir, mask, SearchOption.TopDirectoryOnly)
            .Select(li => new { name = Path.GetFileName(li), type = "file" })
            .ToArray();

          Json(e, headers, files);
          return;
        }
        Error404(e);
        return;
      }

      if (DocumentUri.Equals(requestUri.AbsolutePath, StringComparison.InvariantCultureIgnoreCase)) {
        bytes = _documentBytesData;
        charset = "; charset=utf-8";
      }
      else {
        var path = HttpUtility2.UriToPath(requestUri.AbsolutePath);
        if (File.Exists(path)) {
          bytes = File.ReadAllBytes(path);
        }
      }

      if (bytes == null) {
        Error404(e);
        return;
      }

      if (WebResource.MimeTypes.TryGetValue(Path.GetExtension(requestUri.AbsolutePath), out var contentType)) {
        headers.Add($"Content-Type: {contentType}{charset}");
      }
      else {
        headers.Add($"Content-Type: application/octet-stream");
      }

      e.Response = _httpEnvironment.CreateWebResourceResponse(new MemoryStream(bytes), 200, "OK", string.Join("\r\n", headers));
    }

    private void Json<TModel>(CoreWebView2WebResourceRequestedEventArgs e, List<string> headers, TModel data)
    {
      headers.Add("Content-Type: application/json; charset=utf-8");
      e.Response = _httpEnvironment.CreateWebResourceResponse(ToJson(data), 200, "OK", string.Join("\r\n", headers));
    }

    private static MemoryStream ToJson<TData>(TData files, MemoryStream stream = null)
    {
      if (stream == null) {
        stream = new MemoryStream();
      }
      var serializer = new JsonSerializer();
      using (var writter = new StreamWriter(stream, Encoding.UTF8, 2048, true)) {
        serializer.Serialize(writter, files);
        writter.Flush();
      }
      return stream;
    }

    private void Error404(CoreWebView2WebResourceRequestedEventArgs e)
    {
      e.Response = _httpEnvironment.CreateWebResourceResponse(new MemoryStream(), 404, "NotFound", $"");
    }

    private void HttpOptionsContent(CoreWebView2WebResourceRequestedEventArgs e, Uri uri)
    {
      var charset = "";
      var headers = new List<string> {
        "Access-Control-Allow-Origin: *",
        "Access-Control-Allow-Headers: *"
      };

      if (DocumentUri.Equals(uri.AbsolutePath, StringComparison.InvariantCultureIgnoreCase)) {
        charset = "; charset=utf-8";
        headers.Add("Access-Control-Allow-Methods: GET, PUT, OPTIONS");
      }
      else {
        var path = HttpUtility.UrlDecode(uri.AbsolutePath);
        path = Regex.Replace(path, @"^\/disk(\w)\/", "$1:/");
        if (!File.Exists(path)) {
          Error404(e);
          return;
        }
        headers.Add("Access-Control-Allow-Methods: GET, OPTIONS");
      }

      if (WebResource.MimeTypes.TryGetValue(Path.GetExtension(uri.AbsolutePath), out var contentType) == false) {
        contentType = "application/octet-stream";
      }
      headers.Add($"Content-Type: {contentType}{charset}");

      e.Response = _httpEnvironment.CreateWebResourceResponse(new MemoryStream(), 204, "OK", string.Join("\r\n", headers));
    }

    private byte[] _documentBytesData;
    private readonly CoreWebView2Environment _httpEnvironment;
  }
}
