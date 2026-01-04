(function(f){if(typeof exports==="object"&&typeof module!=="undefined"){module.exports=f()}else if(typeof define==="function"&&define.amd){define([],f)}else{var g;if(typeof window!=="undefined"){g=window}else if(typeof global!=="undefined"){g=global}else if(typeof self!=="undefined"){g=self}else{g=this}g.markdownItLineMark = f()}})(function(){var define,module,exports;return (function(){function e(t,n,r){function s(o,u){if(!n[o]){if(!t[o]){var a=typeof require=="function"&&require;if(!u&&a)return a(o,!0);if(i)return i(o,!0);var f=new Error("Cannot find module '"+o+"'");throw f.code="MODULE_NOT_FOUND",f}var l=n[o]={exports:{}};t[o][0].call(l.exports,function(e){var n=t[o][1][e];return s(n?n:e)},l,l.exports,e,t,n,r)}return n[o].exports}var i=typeof require=="function"&&require;for(var o=0;o<r.length;o++)s(r[o]);return s}return e})()({1:[function(require,module,exports){


module.exports = function(md, options) {
  function insertAt(array, pos, value) {
    if (pos >= array.length) {
      array = [...array];
      array.push(value);
      return array;
    }
    return (pos <= 0)
      ? [value, ...array]
      : [...array.slice(0, pos), value, ...array.slice(pos)];
  }

  function handleToken(data) {
    const { context, tokens, index: i } = data;
    const token = tokens[i];

    if (token.children) {
      for(let i1=0; i1 < token.children.length; i1++) {
        const entry = {
          tokens: token.children,
          index: i1,
          context
        };
        handleToken(entry);
        token.children = entry.tokens;
      }
    }

    if (token.type === 'text' && token.content && context.nline !== -1) {
      let a = token.content.trim();
      if (a.length === 0) {
        return false;
      }

      let b = context.line;
      let match = (a == b) ? true : false;

      if (!match) {
        match = b.includes(a);
      }
      if (!match) {
        let len = Math.min(a.length, b.length);
        a = (a.length > len) ? a.slice(0, len) : a;
        b = (b.length > len) ? b.slice(0, len) : b;
        match = (a == b) ? true : false;
      }
      if (!match) {
        a = token.content.trim();
        b = context.line.replace(/^(\*+|\=+|#+|\-+)/, '').trim();
        len = Math.min(a.length, b.length);
        a = (a.length > len) ? a.slice(0, len) : a;
        b = (b.length > len) ? b.slice(0, len) : b;
        match = (a == b) ? true : false;
      }

      if (match) {
        const state = insertLineMarker(data, i, context.nline, context);
        moveToNextLine(context);
        return state;
      }
      return false;
    }
    if (token.nesting == -1) {
      return false
    }
    if (token.map) {
      const map = token.map;
      const nline = map[0];
      const state = insertLineMarker(data, i, nline, context);
      if (context.nline < nline) {
        context.nline = nline;
        moveToNextLine(context);
      }
      return state;
    }
    return false;
  }

  function insertLineMarker(data, i, nline, context) {
    if (context.mark[`L${nline}`] === true) {
      return false;
    }
    context.mark[`L${nline}`] = true;
    const TokenConstructor = context.tokenConstructor;

    let anchor = new TokenConstructor('html_inline', '', 0);
    anchor.content = `<div id='LINE${nline}'></div>`;
    data.tokens = insertAt(data.tokens, i, anchor);
    return true;
  }

  function moveToNextLine(context) {
    const { lines, nline } = context;

    for(let n = nline + 1; n < lines.length; n++) {
      const line = lines[n].trim();
      if (line.length !== 0) {
        context.nline = n;
        context.line = line;
        return;
      }
    }

    context.nline = -1;
    context.line = '';
  }


  md.core.ruler.after('inline', 'linemark', function(state) {
    const context = {
      tokenConstructor: state.Token,
      lines: state.src.split('\n'),
      line: "",
      nline: -1,
      mark: {}
    }

    moveToNextLine(context);
		for (var i = 0; i < state.tokens.length; i++) {
      const entry = {
        tokens: state.tokens,
        index: i,
        context
      };
      handleToken(entry);
      state.tokens = entry.tokens;
		}
    console.log( { state });
	});
};



},{}]},{},[1])(1)
});