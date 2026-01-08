using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Webview2Viewer.Web
{
  internal class WebEventDto
  {
    [JsonProperty("event")]
    public string EventName { get; set; }

    [JsonProperty("payload")]
    public JObject Payload { get; set; }
  }
}
