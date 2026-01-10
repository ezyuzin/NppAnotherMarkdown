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
// import { include } from '@mdit/plugin-include'
import { ins } from '@mdit/plugin-ins'
import { mark } from '@mdit/plugin-mark'
import { plantuml } from '@mdit/plugin-plantuml'
import { ruby } from '@mdit/plugin-ruby'
// import { snippet } from '@mdit/plugin-snippet'
import { spoiler } from '@mdit/plugin-spoiler'
import { stylize } from '@mdit/plugin-stylize'
import { sub } from '@mdit/plugin-sub'
import { sup } from '@mdit/plugin-sup'
import { tab } from '@mdit/plugin-tab'
import { uml } from '@mdit/plugin-uml'
import { embed } from '@mdit/plugin-embed'

const plugins: any = window

plugins.markdownItAbbr = abbr;
plugins.markdownItAlert = alert;
plugins.markdownItAlign = align;
plugins.markdownItAttrs = attrs;
plugins.markdownItContainer = container;
plugins.markdownItDemo = demo;
plugins.markdownItDl = dl;
plugins.markdownItFigure = figure;
plugins.markdownItFootnote = footnote;
plugins.markdownItIcon = icon;
plugins.markdownItImgLazyLoad = imgLazyload;
plugins.markdownItImgMark = imgMark;
plugins.markdownItImgSize = imgSize;
// plugins.markdownItInclude = include;
plugins.markdownItIns = ins;
plugins.markdownItMark = mark;
plugins.markdownItPlantUml = plantuml;
plugins.markdownItRuby = ruby;
// plugins.markdownItSnippet = snippet;
plugins.markdownItSpoiler = spoiler;
plugins.markdownItStylize = stylize;
plugins.markdownItSub = sub;
plugins.markdownItSup = sup;
plugins.markdownItTab = tab;
plugins.markdownItUml = uml;
plugins.markdownItEmbed = embed;
