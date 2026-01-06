using System;
using System.Collections.Generic;

namespace Webview2Viewer
{
  public static class WebResource
  {
    public static readonly IDictionary<string, string> MimeTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
      // HTML / text
      [".html"] = "text/html",
      [".htm"] = "text/html",
      [".xhtml"] = "application/xhtml+xml",
      [".txt"] = "text/plain",
      [".md"] = "text/markdown",
      [".csv"] = "text/csv",

      // Styles & scripts
      [".css"] = "text/css",
      [".js"] = "application/javascript",
      [".mjs"] = "application/javascript",
      [".map"] = "application/json",

      // Data / config
      [".json"] = "application/json",
      [".xml"] = "application/xml",
      [".yaml"] = "application/yaml",
      [".yml"] = "application/yaml",

      // Images
      [".png"] = "image/png",
      [".jpg"] = "image/jpeg",
      [".jpeg"] = "image/jpeg",
      [".gif"] = "image/gif",
      [".bmp"] = "image/bmp",
      [".webp"] = "image/webp",
      [".svg"] = "image/svg+xml",
      [".ico"] = "image/x-icon",
      [".avif"] = "image/avif",

      // Fonts
      [".ttf"] = "font/ttf",
      [".otf"] = "font/otf",
      [".woff"] = "font/woff",
      [".woff2"] = "font/woff2",
      [".eot"] = "application/vnd.ms-fontobject",

      // Media
      [".mp3"] = "audio/mpeg",
      [".wav"] = "audio/wav",
      [".ogg"] = "audio/ogg",
      [".mp4"] = "video/mp4",
      [".webm"] = "video/webm",

      // Binary / runtime
      [".wasm"] = "application/wasm",
      [".pdf"] = "application/pdf",
      [".zip"] = "application/zip",

      // Web extras
      [".manifest"] = "application/manifest+json",
      [".webmanifest"] = "application/manifest+json"
    };
  }
}
