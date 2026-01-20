import { notifyWebEvent } from "../Client/Webevent";

let trackFirstLineActive = false;
let lastPageYOffset = 0;
let lastTrackFirstLine = -1;
let scrollToLineTimeoutId: NodeJS.Timeout;

export function InitTrackFirstLine(enabled: boolean, documentModified: boolean) {
  document.removeEventListener("scroll", scrollEventHandler);
  if (enabled) {
    trackFirstLineActive = false;
    document.addEventListener("scroll", scrollEventHandler);
    if (documentModified === false) {
      lastTrackFirstLine = 0;
    }
  }
}

export function setDelayTrackFirstLine() {
  clearTimeout(scrollToLineTimeoutId);
  trackFirstLineActive = true;
  scrollToLineTimeoutId = setTimeout(() => {
    trackFirstLineActive = false
  }, 1500);
}

export function scrollEventHandler() {
  if (trackFirstLineActive) {
    return;
  }
  setTimeout(async () => {
    await trackFirstLineEx();
    trackFirstLineActive = false;
  }, 50);
  trackFirstLineActive = true;
}

async function trackFirstLineEx() {
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