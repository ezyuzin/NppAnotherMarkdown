
export const MarkdownRenderContext: {
  source: string,
  sourceUrl: string,
  lineMark: boolean,
  documentReady: Promise<void>|null,
  postRender: (() => Promise<void>)[]
} = {
  source: "",
  sourceUrl: "",
  lineMark: false,
  documentReady: null,
  postRender: []
};
