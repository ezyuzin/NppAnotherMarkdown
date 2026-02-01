import markdownIt, { Options as MarkdownItOptions } from 'markdown-it'

import detect_charset from 'detect-charset'
import markdownItLineMark from './plugins/markdown-it-linemark'
import { IDocumentOptions, IViewPlugin } from './Contract/IViewPlugin';
import { markdownItPluginPack } from './plugins/markdown-it-pluginpack';
import { InitBottomSpacer, ScrollToLine, ScrollToPageY } from './Misc/ScrollTo';
import { InitSyncView } from './Misc/SynvView';
import { InitDragAndDrop } from './Misc/DragAndDrop';
import { InitPasteContent } from './Misc/PasteContent';
import { MarkdownRenderContext } from './Misc/MarkdownRenderContext';
import { DynamicScriptsProcessor, importCss } from './Misc/DynamicLoad';

importCss(["markdown/editor.css"]);

async function setDocument(container: HTMLElement, args: Partial<IDocumentOptions>) {
  let options: IDocumentOptions = {
    document: "",
    modified: false,
    lineMark: false,
    trackFirstLine: false,
    pageYOffset: null,
    "md.extensions": [],
    ...args
  }

  const sourceUrl = options.document;
  const match = sourceUrl.match(/\/([^\/]+)$/);
  if (match) {
    document.title = decodeURI(match[1]);
  }

  const response = await fetch(sourceUrl);
  const data = await response.arrayBuffer();
  const decoder = new TextDecoder(detect_charset(new Uint8Array(data)));
  let source = decoder.decode(data);

  InitSyncView(options.trackFirstLine, options.modified);

  const context = MarkdownRenderContext;
  if (context.sourceUrl === sourceUrl && context.source === source && context.lineMark === options.lineMark) {
    return;
  }

  context.source = source;
  context.sourceUrl = sourceUrl;
  context.lineMark = options.lineMark;

  const renderCompleted = Promise.withResolvers<void>();
  context.documentReady = renderCompleted.promise;
  context.postRender = [];

  const markdownItOptions: MarkdownItOptions = {
    html: true
  }

  const md = markdownIt(markdownItOptions);
  await markdownItPluginPack(options['md.extensions'], md);

  if (options.lineMark) {
    md.use(markdownItLineMark);
  }

  if ((window as any).markdownSetup) {
    let markdownSetup: ((md: markdownIt, context: typeof MarkdownRenderContext) => Promise<void>);
    markdownSetup = (window as any).markdownSetup;
    const result = markdownSetup(md, context);
    if (result && result instanceof Promise) {
      await result;
    }
  }

  let html = md.render(source);
  container.innerHTML = html;
  if (context.postRender.length !== 0) {
    await Promise.all(context.postRender.map(li => li()));
    context.postRender = [];
  }
  renderCompleted.resolve();
  DynamicScriptsProcessor(container);

  InitBottomSpacer();
  InitDragAndDrop();
  InitPasteContent();

  if (!options.modified && options.pageYOffset && options.pageYOffset !== 0) {
    ScrollToPageY(options.pageYOffset);
  }
}

(window as any).viewPlugin = {
  setDocument,
  scrollToLine: ScrollToLine,
  dispose: () => { }
} satisfies IViewPlugin;