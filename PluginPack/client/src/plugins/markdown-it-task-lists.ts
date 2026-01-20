import MarkdownIt from "markdown-it/lib/index.mjs";
import StateCore from "markdown-it/lib/rules_core/state_core.mjs";

import { MarkdownRenderContext } from "../Misc/MarkdownRenderContext";
import Token from "markdown-it/lib/token.mjs";

var disableCheckboxes = false;
var useLabelWrapper = false;
var useLabelAfter = false;

const pattern = /^\[(x|v|\s|)\]/i

export default function markdownItTaskList(md: MarkdownIt, options: any) {
  md.core.ruler.after('inline', 'github-task-lists', function (state: StateCore) {

    var tokens = state.tokens;
    for (var i = 2; i < tokens.length; i++) {
      if (isTodoItem(tokens, i)) {
        todoify(tokens[i], state);
        attrSet(tokens[i - 2], 'class', 'task-list-item' + (!disableCheckboxes ? ' enabled' : ''));
        attrSet(tokens[parentToken(tokens, i - 2)], 'class', 'contains-task-list');
      }
    }

    MarkdownRenderContext.postRender.push(async () => {
      const inputs = Array
        .from(document.getElementsByClassName('task-list-item-checkbox'))
        .filter(li => li.localName === "input" && (li as HTMLInputElement).type === 'checkbox')
        .map(li => li as HTMLInputElement)

      for (let input of inputs) {
        input.onchange = (e) => onTaskChanged(e);
      }
    });
  });
}

function todoify(token: Token, state: StateCore) {
  if (makeCheckbox(token, state) && useLabelWrapper && token.children) {
    if (useLabelAfter) {
      const content = token.children[1].content;
      token.children.pop();
      // Use large random number as id property of the checkbox.
      var id = 'task-item-' + Math.ceil(Math.random() * (10000 * 1000) - 1000);
      token.children[0].content = token.children[0].content.slice(0, -1) + ' id="' + id + '">';
      token.children.push(afterLabel(content, id, state));
    } else {
      token.children.unshift(beginLabel(state));
      token.children.push(endLabel(state));
    }
  }
}

function makeCheckbox(token: Token, state: StateCore) {
  const match = token.content.match(pattern);
  if (match && token.map && token.children) {
    const map: [number, number] = token.map;

    var checkbox = new state.Token('html_inline', '', 0);
    checkbox.content = `<input class="task-list-item-checkbox" type="checkbox" data-line="${map[0]}" data-symbol="${match[1]}"`
    if ([" ", ""].includes(match[1]) === false) {
      checkbox.content += ' checked';
    }
    if (disableCheckboxes) {
      checkbox.content += ' disabled';
    }
    checkbox.content += ">";
    checkbox.markup = match[0];
    checkbox.map = map;
    delete (token as any).map;

    const textTokenList = token.children.filter(li => li.type === 'text');
    if (textTokenList.length !== 0) {
      const textToken = textTokenList[0];
      textToken.content = textToken.content.slice(checkbox.markup.length);
    }
    token.content = '';
    token.children.unshift(checkbox);
    return true;
  }
  return false;
}

async function onTaskChanged(e: Event) {
  if (!e.target) {
    return;
  }
  const target = e.target as HTMLInputElement;
  const context = MarkdownRenderContext;


  if (!target.attributes.getNamedItem('data-line')) {
    return;
  }

  const nline = Number.parseInt(target.attributes.getNamedItem('data-line')!.value)
  let symbol = target.attributes.getNamedItem('data-symbol')?.value;

  const lines = context.source.split("\n");
  let line = lines[nline];
  const pattern = `[${symbol}]`;
  let pos = line.indexOf(pattern);
  if (pos !== -1) {
    let newline = line.substring(0, pos);
    symbol = (target.checked ? "x" : " ");
    target.attributes.getNamedItem('data-symbol')!.value = symbol;

    newline += ("[" + symbol + "]");
    newline += line.substring(pos + pattern.length);
    lines[nline] = newline;

    context.source = lines.join("\n");
    await fetch(context.sourceUrl, {
      method: "PUT",
      headers: {
        "Content-Type": "text/text"
      },
      body: context.source
    });
  }
}

function attrSet(token: Token, name: string, value: string) {
  var index = token.attrIndex(name);
  var attr: [string, string] = [name, value];

  if (index < 0 || !token.attrs) {
    token.attrPush(attr);
  } else {
    token.attrs[index] = attr;
  }
}

function parentToken(tokens: Token[], index: number) {
  var targetLevel = tokens[index].level - 1;
  for (var i = index - 1; i >= 0; i--) {
    if (tokens[i].level === targetLevel) {
      return i;
    }
  }
  return -1;
}

function isTodoItem(tokens: Token[], index: number) {
  return isInline(tokens[index]) &&
    isParagraph(tokens[index - 1]) &&
    isListItem(tokens[index - 2]) &&
    startsWithTodoMarkdown(tokens[index]);
}

function beginLabel(state: StateCore) {
  var token = new state.Token('html_inline', '', 0);
  token.content = '<label>';
  return token;
}

function endLabel(state: StateCore) {
  var token = new state.Token('html_inline', '', 0);
  token.content = '</label>';
  return token;
}

function afterLabel(content: string, id: string, state: StateCore) {
  var token = new state.Token('html_inline', '', 0);
  token.content = '<label class="task-list-item-label" for="' + id + '">' + content + '</label>';
  token.attrs = [[ "for", id ]];
  return token;
}

function isInline(token: Token) { return token.type === 'inline'; }
function isParagraph(token: Token) { return token.type === 'paragraph_open'; }
function isListItem(token: Token) { return token.type === 'list_item_open'; }

function startsWithTodoMarkdown(token: Token) {
  return pattern.test(token.content);
}