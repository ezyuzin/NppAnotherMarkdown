export interface IDocumentOptions {
  document: string,
  modified: boolean,
  lineMark: boolean,
  trackFirstLine: boolean,
  pageYOffset: number|null,
  "md.extensions": string[]
}

export interface IViewPlugin {
  setDocument(container: HTMLElement, args: Partial<IDocumentOptions>): void;
  scrollToLine(nline: number): void;
  dispose(): void;
}