using System;

namespace PanelCommon
{
  public interface IEventDispatcher
  {
    EventHandler<DocumentChangedEvent> DocumentChanged { get; }
    EventHandler<FirstLineChangedEvent> FirstLineChanged { get; }
    EventHandler<PasteImageEvent> PasteImage { get; }
    EventHandler<NavigateToEvent> Navigate { get; set; }
  }
}