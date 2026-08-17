import MarkdownIt from 'markdown-it'
import { MarkdownRenderContext } from '../Misc/MarkdownRenderContext'
import { importCss, importScript } from '../Misc/DynamicLoad'

// Mermaid renders on the client. Unlike plantuml (@mdit/plugin-plantuml, which
// produces a server-side <img>), the "mermaid" fenced code block is turned into a
// <pre class="mermaid"> holder and the mermaid runtime converts it to SVG after
// the HTML is inserted into the DOM. markdown.ts runs MarkdownRenderContext.postRender
// callbacks right after "container.innerHTML = html", which is the hook used here.

// mermaid is initialized once per session (theme included).
let initialized = false;

// The dark stylesheet is chosen in C# (Settings.DarkModeCssFile, default
// ".../markdown/markdown-dark.css") and injected as a <link>. There is no DOM theme
// class, so the theme is inferred from the injected stylesheet. A custom dark CSS
// whose name does not contain "markdown-dark" falls back to the light theme.
function isDarkTheme(): boolean {
  const links = document.querySelectorAll('link[rel="stylesheet"]');
  for (const link of Array.from(links)) {
    if (/markdown-dark/i.test((link as HTMLLinkElement).href || '')) {
      return true;
    }
  }
  return false;
}

async function runMermaid() {
  const nodes = document.querySelectorAll('pre.mermaid');
  if (nodes.length === 0) {
    return;
  }
  // importCss/importScript are idempotent, so the runtime is fetched only for the
  // first previewed document that actually contains a diagram.
  importCss(["markdown/plugin-mermaid/mermaid.css"]);
  await importScript(["markdown/mermaid.min.js"]);

  // The prebuilt UMD bundle assigns window.mermaid.
  const mermaid = (window as any).mermaid;
  if (mermaid == null) {
    return;
  }
  if (!initialized) {
    mermaid.initialize({
      startOnLoad: false,
      theme: isDarkTheme() ? 'dark' : 'default'
    });
    initialized = true;
  }
  // suppressErrors keeps one invalid diagram from breaking the whole preview.
  await mermaid.run({ querySelector: 'pre.mermaid', suppressErrors: true });
}

export default function markdownItMermaid(md: MarkdownIt) {
  const context = MarkdownRenderContext;
  const defaultFence = md.renderer.rules.fence!;
  // context.postRender is reset per render (markdown.ts) and this plugin is
  // registered per render (a new MarkdownIt each time), so schedule at most one
  // run per render, and only when a mermaid block is actually present.
  let scheduled = false;

  md.renderer.rules.fence = function (tokens, idx, options, env, self) {
    const token = tokens[idx];
    const info = token.info.trim().split(/\s+/g)[0];
    if (info !== 'mermaid') {
      return defaultFence(tokens, idx, options, env, self);
    }
    if (!scheduled) {
      scheduled = true;
      context.postRender.push(runMermaid);
    }
    return `<pre class="mermaid">${md.utils.escapeHtml(token.content)}</pre>`;
  };
}
