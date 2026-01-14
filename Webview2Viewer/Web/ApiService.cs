using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Web.WebView2.Core;
using Newtonsoft.Json;
using PanelCommon;

namespace Webview2Viewer.Web
{
  internal class ApiService: IWebService
  {
    public string Hostname { get; }

    public ApiService(CoreWebView2Environment environment, string host, IEventDispatcher eventDispatcher)
    {
      _httpEnvironment = environment;
      Hostname = host;
      _on = eventDispatcher;
      _methods.Add(new ApiMethod { Method = "POST", Path = "/webevent", Handler = PostWebEvent });
      _methods.Add(new ApiMethod { Method = "POST", Path = "/paste-image", Handler = PasteImage });
    }

    private CoreWebView2WebResourceResponse PasteImage(CoreWebView2WebResourceRequest request)
    {
      if (_on.PasteImage != null) {
        string contentType = request.Headers
          .FirstOrDefault(h => h.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
          .Value;

        var boundary = contentType
          .Split(';')
          .Select(x => x.Trim())
          .First(x => x.StartsWith("boundary="))
          .Substring("boundary=".Length);

        byte[] body;
        using (var ms = new MemoryStream()) {
          request.Content.CopyTo(ms);
          body = ms.ToArray();
        }
        var pasteImage = GetPasteImage(body, boundary);
        if (pasteImage != null) {
          _on.PasteImage(this, pasteImage.Value);
        }
      }
      return NoContent();
    }

    private PasteImage? GetPasteImage(byte[] body, string boundary)
    {
      var boundaryBytes = Encoding.ASCII.GetBytes("--" + boundary);
      var headerEnd = Encoding.ASCII.GetBytes("\r\n\r\n");

      int start = IndexOf(body, boundaryBytes, 0);
      if (start < 0)
        return null;

      int headersStart = start + boundaryBytes.Length + 2; // \r\n
      int headersEnd = IndexOf(body, headerEnd, headersStart);
      if (headersEnd < 0)
        return null;

      string headers = Encoding.UTF8.GetString(body, headersStart, headersEnd - headersStart);

      var fileName = Regex.Match(headers, @"filename=""(.+?)""").Groups[1].Value;
      if (string.IsNullOrEmpty(fileName)) {
        fileName = Guid.NewGuid() + ".png";
      }

      int dataStart = headersEnd + headerEnd.Length;
      int dataEnd = IndexOf(body, boundaryBytes, dataStart) - 2; // \r\n
      if (dataEnd - dataStart == 0) {
        return null;
      }

      var content = new byte[dataEnd - dataStart];
      Buffer.BlockCopy(body, dataStart, content, 0, content.Length);

      var base64 = Encoding.ASCII.GetString(content);
      var commaIndex = base64.IndexOf(',');
      if (commaIndex >= 0) {
        base64 = base64.Substring(commaIndex + 1);
      }
      byte[] imageBytes = Convert.FromBase64String(base64);

      return new PasteImage { Filename = fileName, Content = imageBytes };
    }

    private CoreWebView2WebResourceResponse PostWebEvent(CoreWebView2WebResourceRequest request)
    {
      string requestBody;
      using (var reader = new StreamReader(request.Content, Encoding.UTF8)) {
        requestBody = reader.ReadToEnd();
      }
      var webEvent = JsonConvert.DeserializeObject<WebEventDto>(requestBody);
      switch (webEvent.EventName) {
        case "trackFirstLine": {
          var value = webEvent.Payload["line"]?.ToObject<int>();
          if (value != null && _on.TrackFirstLine != null) {
            _on.TrackFirstLine(this, new FirstLineChanged { Line = value.Value });
          }
          break;
        }
      }
      return NoContent();
    }

    public bool HandleRequest(CoreWebView2WebResourceRequestedEventArgs e)
    {
      if (e.Response != null) {
        return false;
      }

      var requestUri = new Uri(e.Request.Uri);
      if (e.Request.Method == "OPTIONS") {
        var methods = _methods.Where(li => li.Path == requestUri.AbsolutePath).ToArray();
        if (!methods.Any()) {
          e.Response = Error404();
        }
        else {
          var headers = new List<string> {
            "Access-Control-Allow-Headers: *",
            "Access-Control-Allow-Methods: " + string.Join(",", methods.Select(li => li.Method).Union(new string[] { "OPTIONS" }))
          };
          e.Response = NoContent(headers);
        }
        return true;
      }
      foreach (var method in _methods) {
        if (method.Method == e.Request.Method && method.Path == requestUri.AbsolutePath) {
          e.Response = method.Handler(e.Request);
          return true;
        }
      }
      return false;
    }

    private CoreWebView2WebResourceResponse Json<TModel>(TModel data, List<string> headers = null)
    {
      headers = headers ?? new List<string>();
      headers.Add("Content-Type: application/json; charset=utf-8");
      return _httpEnvironment.CreateWebResourceResponse(ToJson(data), 200, "OK", string.Join("\r\n", headers));
    }

    private static MemoryStream ToJson<TData>(TData model)
    {
      var stream = new MemoryStream();
      var serializer = new JsonSerializer();
      using (var writter = new StreamWriter(stream, Encoding.UTF8, 2048, true)) {
        serializer.Serialize(writter, model);
        writter.Flush();
      }
      return stream;
    }

    private CoreWebView2WebResourceResponse Error404()
    {
      return _httpEnvironment.CreateWebResourceResponse(new MemoryStream(), 404, "NotFound", $"");
    }

    private CoreWebView2WebResourceResponse Error400(List<string> headers = null)
    {
      headers = headers ?? new List<string>();
      headers.AddRange(new string[] {
        "Access-Control-Allow-Origin: *"
      });
      return _httpEnvironment.CreateWebResourceResponse(new MemoryStream(), 400, "BadRequest", string.Join("\r\n", headers));
    }

    private CoreWebView2WebResourceResponse NoContent(List<string> headers = null)
    {
      headers = headers ?? new List<string>();
      headers.AddRange(new string[] {
        "Access-Control-Allow-Origin: *"
      });
      return _httpEnvironment.CreateWebResourceResponse(new MemoryStream(), 204, "OK", string.Join("\r\n", headers));
    }

    private static int IndexOf(byte[] src, byte[] pattern, int start)
    {
      for (int i = start; i <= src.Length - pattern.Length; i++) {
        bool match = true;
        for (int j = 0; j < pattern.Length; j++) {
          if (src[i + j] != pattern[j]) {
            match = false;
            break;
          }
        }
        if (match)
          return i;
      }
      return -1;
    }

    private class ApiMethod
    {
      public string Method { get; set; }
      public string Path { get; set; }
      public Func<CoreWebView2WebResourceRequest, CoreWebView2WebResourceResponse> Handler { get; set; }
    }

    private List<ApiMethod> _methods = new List<ApiMethod>();
    private readonly CoreWebView2Environment _httpEnvironment;
    private readonly IEventDispatcher _on;
  }
}
