import { PasteImageIntoDocument } from "../Client/PasteImage";

class DragAndDrop {
  constructor() {
    const dropZone = document.body;
    const onDispose = this.onDispose;

    this.dropZone = dropZone;

    let onDragEnter = this.dragEnter.bind(this);
    dropZone.addEventListener("dragenter", onDragEnter);
    onDispose.push(() => dropZone.removeEventListener("dragenter", onDragEnter));

    let onDragOver = this.dragOver.bind(this);
    dropZone.addEventListener("dragover", onDragOver);
    onDispose.push(() => dropZone.removeEventListener("dragover", onDragOver));

    let onDragLeave = this.dragLeave.bind(this);
    dropZone.addEventListener("dragleave", onDragLeave);
    onDispose.push(() => dropZone.removeEventListener("dragleave", onDragLeave));

    let onDrag = this.drag.bind(this);
    dropZone.addEventListener("drag", (e) => e.dataTransfer);
    onDispose.push(() => dropZone.removeEventListener("drag", onDrag));
  }

  private async drag(e: DragEvent) {
    e.preventDefault();
    this.dropZone.classList.remove("dragover");
    if (!e.dataTransfer || !e.dataTransfer.files) {
      return;
    }

    const files = Array.from(e.dataTransfer.files);
    for (const file of files) {
      if (file.type.startsWith("image/")) {
        await PasteImageIntoDocument(file);
      }
    }
  }

  private dragLeave(e: Event) {
    e.preventDefault();
    this.dropZone.classList.remove("dragover");
  }

  private dragOver(e: Event) {
    e.preventDefault();
    this.dropZone.classList.add("dragover");
  }

  private dragEnter(e: Event) {
    e.preventDefault();
  }

  dispose() {
    for(let destroy of this.onDispose) {
      destroy();
    }
    this.onDispose = [];
  }

  private onDispose: (() => void)[] = [];
  private dropZone;
}


let instance: DragAndDrop|null = null;

export function InitDragAndDrop() {
  if (instance) {
    instance.dispose();
    instance = null;
  }
  instance = new DragAndDrop();
}
