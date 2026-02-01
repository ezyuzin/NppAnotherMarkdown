import { notifyWebEvent } from "../Client/Webevent";

interface IScrollToActive {
  promise: Promise<void>
  cancelled: boolean
}

let lastPageYOffset = 0;
let lastTrackFirstLine = -1;
let scrollToActive: IScrollToActive | null = null;
let scrollHandlerActive = false;

export function InitSyncView(enabled: boolean, documentModified: boolean) {
  document.removeEventListener("scroll", scrollEventHandler);
  if (enabled) {
    document.addEventListener("scroll", scrollEventHandler);
    if (documentModified === false) {
      lastTrackFirstLine = 0;
    }
  }
}

export async function scrollToY(targetY: number, behavior: ScrollBehavior = "auto"): Promise<void> {
  window.scrollTo({ top: targetY, behavior: behavior });
  if (scrollToActive !== null) {
    return scrollToActive.promise;
  }

  const context: IScrollToActive = {
    cancelled: false,
    promise: new Promise<void>(resolve => {
      let pageYOffset = window.pageYOffset;
      const checkInterval = setInterval(() => {
        if (context.cancelled) {
          clearInterval(checkInterval);
          resolve();
          return;
        }
        if (window.pageYOffset !== pageYOffset) {
          pageYOffset = window.pageYOffset;
          return;
        }

        if (scrollToActive === context) {
          scrollToActive = null;
        }
        clearInterval(checkInterval);
        resolve();
      }, 50);
    })
  }
  scrollToActive = context;

  return context.promise;
}

function scrollEventHandler() {
  if (scrollToActive || scrollHandlerActive) {
    return;
  }
  setTimeout(async () => {
    if (!scrollToActive) {
      await syncFirstLineEx();
    }
    scrollHandlerActive = false;
  }, 50);
  scrollHandlerActive = true;
}

async function syncFirstLineEx() {
  console.log(`handle user window scroll()`)

  const dY = window.pageYOffset - (lastPageYOffset || 0);
  lastPageYOffset = window.pageYOffset;
  if (dY === 0) {
    return;
  }

  let lines = Array.from(document.querySelectorAll('span.linemark'))
    .map(el => {
      const rect = el.getBoundingClientRect();
      return {
        el,
        rect,
        dY: Math.abs(rect.top)
      };
    });

  const dH = 0.1 * window.innerHeight;
  lines = lines.filter(li => li.rect.bottom >= -dH && li.rect.top < dH);

  if (lines.length === 0) {
    return;
  }

  if (lines.length > 1) {
    lines.sort((a, b) => a.rect.left - b.rect.left);
    lines.sort((a, b) => a.dY - b.dY);
  }

  const match = `${lines[0].el.id}`.match(/^LINE(\d+)$/);
  if (match && match.length >= 2) {
    const line = Number.parseInt(match[1]);
    if (lastTrackFirstLine !== line) {
      lastTrackFirstLine = line;
      await notifyWebEvent("trackFirstLine", { line });
    }
  }
}