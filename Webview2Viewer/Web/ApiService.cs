using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Web.WebView2.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
