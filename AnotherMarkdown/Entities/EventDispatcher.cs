using System;
using PanelCommon;

namespace AnotherMarkdown.Entities
{
  public class EventDispatcher : IEventDispatcher
  {
    public EventHandler<DocumentContentChanged> DocumentChanged { get; set; }
    public EventHandler<FirstLineChanged> TrackFirstLine { get; set; }
    public EventHandler<PasteImage> PasteImage { get; set; }
    public EventHandler<NavigateTo> Navigate { get; set; }
  }
}
