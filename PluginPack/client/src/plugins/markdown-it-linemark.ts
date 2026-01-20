import MarkdownIt from "markdown-it";
import StateCore from "markdown-it/lib/rules_core/state_core.mjs"
import { Token } from "markdown-it/index.js";

import { Hashmap } from "../Lib/Common/Hashmap";

export default function markdownItLineMark(md: MarkdownIt, options: any) {
  md.core.ruler.after('inline', 'linemark', function (state) {
    const ruler = new LineMarkRuler(state);
    ruler.Render();
  });
}

class LineMarkRuler {
  constructor(state: StateCore) {
    this.state = state;
  }

  public Render() {
    const { state } = this;

    this.line = ""
    this.nline = -1;
    this.lines = this.state.src.split('\n');
    this.moveToNextLine();

    for (var i = 0; i < state.tokens.length; i++) {
      state.tokens = this.handleToken(state.tokens, i);
    }
  }

  private moveToNextLine() {
    const { nline, lines } = this;

    for (let n = nline + 1; n < lines.length; n++) {
      const line = lines[n].trim();
      if (/^[=\*\-\|]+$/.test(line)) {
        continue;
      }
      if (line.length !== 0) {
        this.nline = n;
        this.line = line;
        return;
      }
    }

    this.nline = -1;
    this.line = '';
  }

  private handleToken(tokens: Token[], ix: number): Token[] {
    const token = tokens[ix];

    if (token.children) {
      const children = token.children.filter(li => li.type === 'text');
      children.push(...token.children.filter(li => li.type !== 'text'))

      for (let i1 = 0; i1 < children.length; i1++) {
        if (!token.children) {
          continue;
        }

        const child = children[i1];
        const index = token.children.indexOf(child);
        if (index !== -1) {
          token.children = this.handleToken(token.children, index);
        }
      }
    }

    if (token.type === 'text' && token.content && this.nline !== -1) {
      let a = token.content.trim();
      if (a.length === 0) {
        return tokens;
      }

      let b = this.line;
      let match = (a == b) ? true : false;
      if (!match) {
        match = b.startsWith(a);
      }
      if (!match) {
        let len = Math.min(a.length, b.length);
        a = (a.length > len) ? a.slice(0, len) : a;
        b = (b.length > len) ? b.slice(0, len) : b;
        match = (a == b) ? true : false;
      }
      if (!match) {
        a = token.content.trim();
        b = this.line;
        b = b.replace(/^(\*+|\=+|#+|\-+|\|\s)/, '').trim();

        const len = Math.min(a.length, b.length);
        a = (a.length > len) ? a.slice(0, len) : a;
        b = (b.length > len) ? b.slice(0, len) : b;
        match = (a == b) ? true : false;
      }

      if (match) {
        tokens = this.insertLineMarker(tokens, ix, this.nline);
        this.moveToNextLine();
      }
      return tokens;
    }
    if (token.nesting === 1 || token.nesting === -1) {
      return tokens;
    }
    if (token.map) {
      const map = token.map;
      const nline = map[0];
      tokens = this.insertLineMarker(tokens, ix, nline);
      if (this.nline < nline) {
        this.nline = nline;
        this.moveToNextLine();
      }
    }
    return tokens;
  }

  private insertAt<T>(array: Array<T>, pos: number, value: T) {
    if (pos >= array.length) {
      array = [...array];
      array.push(value);
      return array;
    }
    return (pos <= 0)
      ? [value, ...array]
      : [...array.slice(0, pos), value, ...array.slice(pos)];
  }

  private insertLineMarker(tokens: Token[], pos: number, nline: number) {
    if (this.mark[`L${nline}`] === true) {
      return tokens;
    }

    this.mark[`L${nline}`] = true;
    let anchor = new this.state.Token('html_inline', '', 0);
    anchor.content = `<span id='LINE${nline}' class="linemark"></span>`;
    return this.insertAt(tokens, pos, anchor);
  }

  private mark: Hashmap<boolean> = {};
  private nline = -1;
  private line: string = "";
  private lines: string[] = []
  private state: StateCore
}
