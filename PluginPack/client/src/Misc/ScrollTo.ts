import { scrollToY } from "./SynvView";

export async function ScrollToPageY(pageYOffset: number) {
  await scrollToY(pageYOffset);

  await WaitForImages();
  await scrollToY(pageYOffset);

  await WaitForDocumentStable();
  await scrollToY(pageYOffset);

  await new Promise<void>((resolve) => setTimeout(() => resolve(), 100));
  await scrollToY(pageYOffset);
}

export function ScrollToLine(line: number) {
  if (line === 0) {
    scrollToY(0)
    return;
  }

  let index = 0;
  let element = null;
  while (true) {
    element = document.getElementById(`LINE${line++}`);
    if (element) {
      break;
    }
    if (++index === 10) {
      return;
    }
  }

  const rect = element.getBoundingClientRect();
  const requiredScrollTop = rect.top + window.pageYOffset;

  InitBottomSpacer();
  scrollToY(requiredScrollTop);
}

export function InitBottomSpacer() {
  var spacer = document.getElementById('spacer');
  if (!spacer) {
    spacer = document.createElement('div');
    spacer.id = 'spacer';
    spacer.style.height = window.innerHeight + 'px';
    spacer.style.width = '1px';
    spacer.style.pointerEvents = 'none';
    document.body.appendChild(spacer);
  }
  else {
    const height = window.innerHeight + 'px';
    if (spacer.style.height !== height) {
      spacer.style.height = height;
    }
  }
}

function WaitForDocumentStable(timeout = 60) {
  return new Promise<void>(resolve => {
    let observer: MutationObserver;
    let timer: NodeJS.Timeout;

    const debouncedResolve = () => {
      clearTimeout(timer);
      timer = setTimeout(() => {
        observer.disconnect();
        resolve();
      }, timeout);
    };

    observer = new MutationObserver(debouncedResolve);
    observer.observe(document.body, {
      childList: true,
      subtree: true,
      attributes: true,
      characterData: true
    });

    timer = setTimeout(() => {
      observer.disconnect();
      resolve();
    }, timeout);
  });
}

function WaitForImages(timeout = 5000) {
  const images = Array.from(document.images);
  const promises = images.map(img => {
    if (img.complete) return Promise.resolve();
    return new Promise<void>(resolve => {
      const onLoadOrError = () => {
        resolve();
        img.removeEventListener('load', onLoadOrError);
        img.removeEventListener('error', onLoadOrError);
      };
      img.addEventListener('load', onLoadOrError);
      img.addEventListener('error', onLoadOrError);
    });
  });
  return Promise.race([
    Promise.all(promises),
    new Promise(resolve => setTimeout(resolve, timeout))
  ]);
}
