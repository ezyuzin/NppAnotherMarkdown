import { PasteImageIntoDocument } from "../Client/PasteImage";

class PasteContent {
  constructor() {
    let onPaste = this.pasteEventHandler.bind(this);
    document.addEventListener("paste", onPaste);
    this.onDispose.push(() => document.removeEventListener("paste", onPaste));
  }

  private async pasteEventHandler(e: ClipboardEvent) {
    e.preventDefault();
    if (e.clipboardData && e.clipboardData.items) {
      const entries = Array.from(e.clipboardData.items);
      for (const entry of entries) {
        if (entry.type.startsWith("image/")) {
          e.preventDefault();
          const file = entry.getAsFile();
          if (file) {
            await PasteImageIntoDocument(file);
          }
        }
      }
    }
  }

  dispose() {
    for(let destroy of this.onDispose) {
      destroy();
    }
    this.onDispose = [];
  }

  private onDispose: (() => void)[] = [];
}

let instance: PasteContent|null = null;

export function InitPasteContent() {
  if (instance) {
    instance.dispose();
    instance = null;
  }
  instance = new PasteContent();
}
