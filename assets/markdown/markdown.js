const markdownScripts = [
 "http://assets.example/detect-charset.js",
 "http://assets.example/markdown/markdown-it@14.1.0.min.js",
 "http://assets.example/markdown/markdown-it-attrs@4.1.0.js",
 "http://assets.example/markdown/markdown-it-task-lists.min.js",
 "http://assets.example/markdown/markdown-it-linemark.js",
 "http://assets.example/markdown/markdown-it-embed.js"
].map(li => {
  const script = document.createElement("script");
  script.src = li;
  script.defer = true;
  document.head.appendChild(script);
  return new Promise((resolve) => {
    script.onload = () => resolve();
  });
});

window.viewPlugin = (() => {
  let context = {
    source: "",
    sourceUrl: "",
    lineMark: false
  };

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

  function trackFirstLine(event) {
    if (context.trackFirstLineActive) {
      return;
    }
    setTimeout(async () => {
      const lines = [...document.querySelectorAll('span.linemark')]
        .map(el => { return { el, rect: el.getBoundingClientRect() }; } )
        .filter(li => li.rect.top > 0 && li.rect.bottom > 0 && li.rect.top < window.innerHeight);

      if (lines.length !== 0) {
        if (lines.length > 1) {
          lines.sort((a, b) => a.rect.left - b.rect.left);
          lines.sort((a, b) => a.rect.top - b.rect.top);
        }
        const line = Number.parseInt(`${lines[0].el.id}`.match(/^LINE(\d+)$/)[1]);
        if (context.lastTrackFirstLine !== line) {
          context.lastTrackFirstLine = line;
          console.log({ line });
          await sendWebEvent("trackFirstLine", { line });
        }
      }
      context.trackFirstLineActive = false;
    }, 20);
    context.trackFirstLineActive = true;
  }

  async function setDocument(container, args) {
    let options = {
      document: "",
      modified: false,
      lineMark: false,
      trackFirstLine: false
    }
    options = { ...options, ...args };
    const { document: url } = options;

    document.title = decodeURI(url.match(/\/([^\/]+)$/)[1]);
    await Promise.all(markdownScripts);

    const response = await fetch(url);
    const data = await response.arrayBuffer();
    const decoder = new TextDecoder(detect_charset(new Uint8Array(data)));
    let source = decoder.decode(data);

    document.removeEventListener("scroll", trackFirstLine);

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
    md.use(window.markdownItAttrs);
    md.use(window.markdownItEmbedd, {
      config: [
        embedPano360(),
        embedQrCode()
      ]
    });
    md.use(...markdownItTaskLists());
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

    if (options.trackFirstLine) {
      document.addEventListener("scroll", trackFirstLine);
    }

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

  function registerCss(path) {
    const link = document.createElement("link");
    link.href = 'http://assets.example/markdown/' + path;
    link.rel = 'stylesheet';
    document.body.appendChild(link);
    return link;
  }

  function registerJs(path) {
    const script = document.createElement("script");
    script.src = 'http://assets.example/markdown/' + path;
    script.defer = true;
    document.body.appendChild(script);
    return script;
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
        QRCode.toDataURL(qrcode.text, options, (err, value) => {
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
        if (!data['loading']) {
          data.loading = new Promise((resolve) => {
            registerCss("qrcode.css");
            registerJs('qrcode.js').onload = () => resolve();
          });
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
    let panoIdSeq = 0;
    return {
      name: "pano360",
      allowInline: false,
      setup: (configFile) => {
        if (!context['pano360.loader']) {
          loader = Promise.withResolvers();
          registerCss("pannellum.css");
          registerJs('pannellum.js').onload = () => loader.resolve();
          context['pano360.loader'] = loader.promise;
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
          await context['pano360.loader'];
          context[sceneId] = scene;
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

  return {
    setDocument,
    scrollToLine,
    dispose: () => {}
  }
})();
