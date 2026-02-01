using System;
using PanelCommon;

namespace AnotherMarkdown.Entities
{
  public class EventDispatcher : IEventDispatcher
  {
    public EventHandler<DocumentChangedEvent> DocumentChanged { get; set; }
    public EventHandler<FirstLineChangedEvent> FirstLineChanged { get; set; }
    public EventHandler<PasteImageEvent> PasteImage { get; set; }
    public EventHandler<NavigateToEvent> Navigate { get; set; }
  }
}
