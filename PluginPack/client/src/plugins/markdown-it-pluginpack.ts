import { abbr } from '@mdit/plugin-abbr'
import { alert } from '@mdit/plugin-alert'
import { align } from '@mdit/plugin-align'
import { attrs } from '@mdit/plugin-attrs'
import { container } from '@mdit/plugin-container'
import { demo } from '@mdit/plugin-demo'
import { dl } from '@mdit/plugin-dl'
import { figure } from '@mdit/plugin-figure'
import { footnote } from '@mdit/plugin-footnote'
import { icon } from '@mdit/plugin-icon'
import { imgLazyload } from '@mdit/plugin-img-lazyload'
import { imgMark } from '@mdit/plugin-img-mark'
import { imgSize } from '@mdit/plugin-img-size'
import { ins } from '@mdit/plugin-ins'
import { mark } from '@mdit/plugin-mark'
import { plantuml } from '@mdit/plugin-plantuml'
import { ruby } from '@mdit/plugin-ruby'
import { spoiler } from '@mdit/plugin-spoiler'
import { stylize } from '@mdit/plugin-stylize'
import { sub } from '@mdit/plugin-sub'
import { sup } from '@mdit/plugin-sup'
import { tab } from '@mdit/plugin-tab'
import { embed as markdownItEmbed } from '@mdit/plugin-embed'
import { full as MarkdownItPluginEmoji } from "markdown-it-emoji"
import { katex as MarkdownItPluginKatex } from '@mdit/plugin-katex';
import MarkdownItPluginHighlightJs from 'highlight.js'

import MarkdownIt from 'markdown-it'
import markdownItTaskLists from './markdown-it-task-lists'
import markdownItEmbedQrcode from './markdown-it-embed-qrcode'
import markdownItEmbedPano360 from './markdown-it-embed-pano360'
import markdownItMermaid from './markdown-it-mermaid'
import { importCss, importScript } from '../Misc/DynamicLoad'

export async function markdownItPluginPack(enabled: string[], md: MarkdownIt) {

  const builder: (() => Promise<void>)[] = [];

  if (enabled.includes("emoji")) {
    md.use(MarkdownItPluginEmoji, {});
  }

  if (enabled.includes("katex")) {
    builder.push((async () => {
      importCss(["markdown/plugin-katex/katex@0.24.1.min.css"]);
      md.use(MarkdownItPluginKatex, {});
    }))
  }

  if (enabled.includes("highlightjs")) {
    builder.push((async () => {
      importCss(["markdown/plugin-highlightjs/github.min.css"]);
      await importScript(["markdown/markdown-it-highlightjs@11.11.1.min.js"]);

      md.options.highlight = function (str, lang) {
        const hljs = (window as any).markdownItPluginHighlightJs as typeof MarkdownItPluginHighlightJs;
        if (lang && hljs.getLanguage(lang)) {
          try {
            return hljs.highlight(str, { language: lang }).value;
          }
          catch (err) {}
        }
        return ''; // use external default escaping
      };
    }))
  }

  const embed = [];
  if (enabled.includes("qrcode")) {
    embed.push(markdownItEmbedQrcode());
  }
  if (enabled.includes("pano360")) {
    embed.push(markdownItEmbedPano360());
  }
  if (embed.length !== 0) {
    md.use(markdownItEmbed, { config: embed });
  }

  if (enabled.includes("attrs")) {
    md.use(attrs);
  }
  if (enabled.includes("tasks-list")) {
    md.use(markdownItTaskLists);
  }
  if (enabled.includes("abbr")) {
    md.use(abbr, {});
  }
  if (enabled.includes("alert")) {
    importCss(["markdown/plugin-alert/alert.css"]);
    md.use(alert, {});
  }
  if (enabled.includes("align")) {
    md.use(align, {});
  }
  if (enabled.includes("container")) {
    md.use(container, {});
  }
  if (enabled.includes("demo")) {
    md.use(demo, {});
  }
  if (enabled.includes("dl")) {
    md.use(dl, {});
  }
  if (enabled.includes("figure")) {
    md.use(figure, {});
  }
  if (enabled.includes("footnote")) {
    md.use(footnote, {});
  }
  if (enabled.includes("icon")) {
    md.use(icon, {});
  }
  if (enabled.includes("imgLazyload")) {
    md.use(imgLazyload, {});
  }
  if (enabled.includes("ImgMark")) {
    md.use(imgMark, {});
  }
  if (enabled.includes("imgSize")) {
    md.use(imgSize, {});
  }
  if (enabled.includes("ins")) {
    md.use(ins, {});
  }
  if (enabled.includes("mark")) {
    md.use(mark, {});
  }
  if (enabled.includes("plantuml")) {
    md.use(plantuml, {
    });
  }
  if (enabled.includes("mermaid")) {
    md.use(markdownItMermaid);
  }
  if (enabled.includes("ruby")) {
    md.use(ruby, {});
  }
  if (enabled.includes("spoiler")) {
    importCss(["markdown/plugin-spoiler/spoiler.css"]);
    md.use(spoiler, {});
  }
  if (enabled.includes("stylize")) {
    md.use(stylize, {});
  }
  if (enabled.includes("sub")) {
    md.use(sub, {});
  }
  if (enabled.includes("sup")) {
    md.use(sup, {});
  }
  if (enabled.includes("tab")) {
    md.use(tab, {});
  }

  await Promise.all(builder.map(b => b()));
}
