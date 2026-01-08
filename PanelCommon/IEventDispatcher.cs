using System;

namespace PanelCommon
{
  public interface IEventDispatcher
  {
    EventHandler<DocumentContentChanged> DocumentChanged { get; }
    EventHandler<FirstLineChanged> TrackFirstLine { get; }
  }
}