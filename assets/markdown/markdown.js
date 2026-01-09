window.viewPlugin = (() => {
  let context = {
    source: "",
    sourceUrl: "",
    lineMark: false,
    scripts: []
  };

  async function setDocument(container, args) {
    let options = {
      document: "",
      modified: false,
      lineMark: false,
      trackFirstLine: false,
      "md.extensions": []
    }
    options = { ...options, ...args };
    const { document: url } = options;

    document.title = decodeURI(url.match(/\/([^\/]+)$/)[1]);

    const dependencies = [
      "detect-charset.js",
      "markdown/markdown-it@14.1.0.min.js",
      "markdown/markdown-it-linemark.js"
    ]

    if (options["md.extensions"].includes("katex")) {
      dependencies.push(...[
        "markdown/plugin-katex/katex@0.24.1.min.css",
        "markdown/markdown-it-katex@0.24.1.min.js"
      ]);
    }
    if (options["md.extensions"].includes("attrs")) {
      dependencies.push(...[
        "markdown/markdown-it-attrs@4.1.0.js"
      ]);
    }
    if (options["md.extensions"].includes("tasks-list")) {
      dependencies.push(...[
        "markdown/markdown-it-task-lists@2.1.0.js"
      ]);
    }
    if (["qrcode", "pano360"].some(li => options["md.extensions"].includes(li))) {
      dependencies.push(...[
        "markdown/markdown-it-embed.js"
      ]);
    }

    await loadScripts(dependencies);

    const response = await fetch(url);
    const data = await response.arrayBuffer();
    const decoder = new TextDecoder(detect_charset(new Uint8Array(data)));
    let source = decoder.decode(data);

    document.removeEventListener("scroll", trackFirstLine);
    if (options.trackFirstLine) {
      document.addEventListener("scroll", trackFirstLine);
      if (options.modified === false) {
        context.lastTrackFirstLine = 0;
      }
    }
    if (context.sourceUrl === url && context.source === source && context.lineMark === options.lineMark) {
      return;
    }

    context.source = source;
    context.sourceUrl = url;
    context.lineMark = options.lineMark;

    const renderCompleted = Promise.withResolvers();
    context.documentReady = renderCompleted.promise;
    context.postRender = [];

    const md = window.markdownit({
      html: true
    });

    if (options["md.extensions"].includes("attrs")) {
      md.use(window.markdownItAttrs);
    }

    const embed = [];
    if (options["md.extensions"].includes("qrcode")) {
      embed.push(embedQrCode());
    }
    if (options["md.extensions"].includes("pano360")) {
      embed.push(embedPano360());
    }
    if (embed.length !== 0) {
      md.use(window.markdownItEmbedd, { config: embed });
    }
    if (options["md.extensions"].includes("tasks-list")) {
      md.use(...markdownItTaskLists());
    }
     if (options["md.extensions"].includes("katex")) {
      md.use(window.markdownItKatex, {});
    }

    if (options.lineMark) {
      md.use(window.markdownItLineMark);
    }

    let html = md.render(source);
    container.innerHTML = html;
    if (context.postRender.length !== 0) {
      await Promise.all(context.postRender.map(li => li()));
      context.postRender = [];
    }
    renderCompleted.resolve();

    container.querySelectorAll("script").forEach((oldScript) => {
      const newScript = document.createElement("script");
      if (oldScript.src) {
        newScript.src = oldScript.src;
      } else {
        newScript.textContent = oldScript.textContent;
      }
      document.head.appendChild(newScript);
      document.head.removeChild(newScript);
    });
  }

  function parseQuery(str) {
    return ` ${str}`
      .matchAll(/[\s](\w+)=['"](.*?)['"]/g)
      .reduce((acc, li) => {
        let key = li[1].trim();
        let value = li[2];
        if (value.match(/^"(.*)"$/)) {
          value = value.substring(1, value.length - 1);
        }
        else if (value.match(/^'(.*)'$/)) {
          value = value.substring(1, value.length - 1);
        }
        acc[key] = value;
        return acc;
      }, {});
  }

  function markdownItTaskLists() {
    async function onTaskChanged(e) {
      const target = e.target;
      const nline = target.attributes['data-line'].value;
      let symbol = target.attributes['data-symbol'].value;

      const lines = context.source.split("\n");
      let line = lines[nline];
      const pattern = `[${symbol}]`;
      let pos = line.indexOf(pattern);
      if (pos !== -1) {
        let newline = line.substring(0, pos);
        symbol = (target.checked ? "x" : " ");
        target.attributes['data-symbol'].value = symbol;

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

    context.postRender.push(async() => {
      const inputs = Array.from(document.getElementsByClassName('task-list-item-checkbox')).filter(li => li.localName === "input" && li.type === 'checkbox');
      for(let input of inputs) {
        input.onchange = onTaskChanged;
      }
    });
    return [
      window.markdownItTaskLists, {
      enabled: true
    }];
  }

  function embedQrCode() {
    if (!context['qrcode']) {
      context['qrcode'] = {
        seq: 0,
        entries: []
      };
    }
    const data = context['qrcode'];
    data.seq = 0;
    data.entries.forEach(li => li.active = false);

    const onCompleted = new Promise(async(resolve) => {
      await context.documentReady;
      data.entries = data.entries.filter(li => li.active);
      resolve();
    })

    function createQRCode(qrcode) {
      const options = {
        margin: qrcode['margin'] || 2,
        color: {
          dark: qrcode['color'] || '#0277bd',
          light: qrcode['background'] || '#ffffff'
        }
      }
      for(let entry of data.entries) {
        if (entry.text === qrcode.text) {
          if (entry.options === JSON.stringify(options)) {
            entry.active = true;
            return entry.value;
          }
        }
      }

      return new Promise(async (resolve, error) => {
        await data.loading
        window.QRCode.toDataURL(qrcode.text, options, (err, value) => {
          if (err) {
            error(err);
            return;
          }
          data.entries.push({
            text: qrcode.text,
            active: true,
            options: JSON.stringify(options),
            value
          });

          resolve(value);
        });
      });
    }

    return {
      name: "qrcode",
      allowInline: true,
      setup: (config) => {
        if (!data.loading) {
          data.loading = loadScripts(['markdown/qrcode.css', 'markdown/qrcode.js']);
        }
        const args = parseQuery(config);
        const qrcode = createQRCode(args);
        let block =`<img class="qrcode" alt="${args.text}" `;
        if (args['style']) {
          block += `style="${args['style']}" `;
        }
        if (qrcode instanceof Promise) {
          const id = `qrcode${++data.seq}`;
          block += `id='${id}' `;
          (async() => {
            await context.documentReady;
            document.getElementById(id).src = await qrcode;
          })();
        }
        else {
          block += "src='" + qrcode + "'";
        }
        block += "/>"

        if (args['hover'] !== undefined) {
          block = `
  <div style="position: relative;">
    <div style="position: absolute; z-index: 1000; ${args['hover']}">
      ${block}
    </div>
  </div>`
        }
        return block;
      }
    }
  }

  function embedPano360() {
    if (!context['pano360']) {
      context['pano360'] = {
      };
    }
    const data = context['pano360'];
    let panoIdSeq = 0;

    return {
      name: "pano360",
      allowInline: false,
      setup: (configFile) => {
        if (!data.loader) {
          data.loader = loadScripts(['markdown/pannellum.css', 'markdown/pannellum.js']);
        }

        const panoramaId = ++panoIdSeq;
        const sceneId = `pano360.scene[${panoramaId}]`;
        if (context[sceneId]) {
          const element = document.getElementById(`pano${panoramaId}`);
          context[sceneId].div = element;
          element.parentElement.removeChild(element);
        }

        context.postRender.push(async() => {
          const config = await (await fetch(configFile)).json();
          config.default.basePath = (configFile.match(/^(.*)(\/)[^\/]*$/))[1] + "/";

          const scene = {
            elementId: `pano${panoramaId}`,
            configText: JSON.stringify(config)
          }

          if (context[sceneId]) {
            if (context[sceneId].div && context[sceneId].configText === scene.configText) {
              const element = document.getElementById(scene.elementId);
              if (element) {
                const parentElement = element.parentElement;
                parentElement.removeChild(element);
                parentElement.appendChild(context[sceneId].div);
                delete context[sceneId].div;
                return;
              }
            }
          }

          context[sceneId] = scene;
          await data.loader;
          pannellum.viewer(`pano${panoramaId}`, config);
        });

        return `
<div class="panorama">
  <div id="pano${panoramaId}"></div>
</div>`
      }
    }
  }
  function scrollToLine(line) {
    if (line === 0) {
      context.trackFirstLineActive = true;
      clearTimeout(context.scrollToLineTimeoutId)
      context.scrollToLineTimeoutId = setTimeout(() => context.trackFirstLineActive = false, 1500);

      window.scrollTo({
        top: 0,
        behavior: 'smooth'
      });
      return;
    }

    let index = 0;
    let element = null;
    while(true) {
      element = document.getElementById(`LINE${line++}`);
      if (element) {
        break;
      }
      if (++index === 10) {
        return;
      }
    }

    var spacer = document.getElementById('spacer');
    var rect = element.getBoundingClientRect();

    var elementTop = rect.top + window.pageYOffset;
    var requiredScrollTop = elementTop;
    var maxScrollTop = document.documentElement.scrollHeight - window.innerHeight;
    if (requiredScrollTop > maxScrollTop) {
      var extraHeight = requiredScrollTop - maxScrollTop;
      if (!spacer) {
        spacer = document.createElement('div');
        spacer.id = 'spacer';
        spacer.style.height = extraHeight + 'px';
        spacer.style.width = '1px';
        spacer.style.pointerEvents = 'none';
        document.body.appendChild(spacer);
      }
      else {
        var spacerRect = spacer.getBoundingClientRect();
        spacer.style.height = extraHeight + spacerRect.height + 'px';
      }
    }

    context.trackFirstLineActive = true;
    clearTimeout(context.scrollToLineTimeoutId);
    context.scrollToLineTimeoutId = setTimeout(() => context.trackFirstLineActive = false, 1500);

    window.scrollTo({
      top: requiredScrollTop,
      behavior: 'smooth'
    });
  }

  async function sendWebEvent(name, payload) {
    await fetch('http://api.example/webevent', {
      method: "POST",
      headers: {
        "Content-Type": "application/json"
      },
      body: JSON.stringify({
        event: name,
        payload
      })
    });
  }

  function trackFirstLine(e) {
    if (context.trackFirstLineActive) {
      return;
    }
    setTimeout(async () => {
      context.trackFirstLineActive = false;
      const dY = window.pageYOffset - (context.lastPageYOffset || 0);
      context.lastPageYOffset = window.pageYOffset;
      if (dY === 0) {
        return;
      }

      let lines = [...document.querySelectorAll('span.linemark')]
        .map(el => {
          const rect = el.getBoundingClientRect();
          return {
            el,
            rect,
            dY: Math.abs(rect.top)
          };
        });

      const dH = 0.1 * window.innerHeight;
      lines = lines.filter(li => li.rect.bottom >= -dH && li.rect.top < dH);

      if (lines.length === 0) {
        return;
      }

      if (lines.length > 1) {
        lines.sort((a, b) => a.rect.left - b.rect.left);
        lines.sort((a, b) => a.dY - b.dY);
      }
      const line = Number.parseInt(`${lines[0].el.id}`.match(/^LINE(\d+)$/)[1]);
      if (context.lastTrackFirstLine !== line) {
        context.lastTrackFirstLine = line;
        await sendWebEvent("trackFirstLine", { line });
      }
    }, 50);
    context.trackFirstLineActive = true;
  }

  async function loadScripts(scripts) {
    const promises = scripts
      .filter(li => !context.scripts.includes(li))
      .map(path => {
        context.scripts.push(path);
        if (path.endsWith('.css')) {
          const link = document.createElement("link");
          link.href = 'http://assets.example/' + path;
          link.rel = 'stylesheet';
          document.head.appendChild(link);
          return Promise.resolve();
        }
        else if (path.endsWith('.js')) {
          const script = document.createElement("script");
          script.src = 'http://assets.example/' + path;
          script.defer = true;
          return new Promise((resolve) => {
            script.onload = () => resolve();
             document.head.appendChild(script);
          });
        }
        else {
          return Promise.resolve();
        }
      });

    await Promise.all(promises);
  }


  return {
    setDocument,
    scrollToLine,
    dispose: () => {}
  }
})();
