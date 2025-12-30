const markdownScripts = [
 "http://assets.example/detect-charset.js",
 "http://assets.example/markdown/markdown-it@14.1.0.min.js",
 "http://assets.example/markdown/markdown-it-attrs@4.1.0.js",
 "http://assets.example/markdown/markdown-it-task-lists.min.js",
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
  let context = {};

  async function importMarkdown(container, url, options) {
    options = {
      lineMark: false,
      ...(options || {})
    }
    
    await Promise.all(markdownScripts);

    const response = await fetch(url);
    const data = await response.arrayBuffer();
    const decoder = new TextDecoder(detect_charset(new Uint8Array(data)));
    let source = decoder.decode(data);

    if (options.lineMark) {
      source = setLineMarkers(source);
    }

    const renderCompleted = Promise.withResolvers();
    context.documentReady = renderCompleted.promise;

    const md = window.markdownit({
      html: true
    });
    md.use(window.markdownItAttrs);
    md.use(window.markdownitTaskLists);
    md.use(window.markdownItEmbedd, {
      config: [
        embedPano360(),
        embedQrCode()
      ]
    });

    let html = md.render(source);
    if (options.lineMark) {
      html = applyLineMarkers(html);
    }
    container.innerHTML = html;
    renderCompleted.resolve();

    container.querySelectorAll("script").forEach((oldScript) => {
      const newScript = document.createElement("script");
      if (oldScript.src) {
        newScript.src = oldScript.src;
      } else {
        newScript.textContent = oldScript.textContent;
      }
      document.head.appendChild(newScript);
      document.head.removeChild(newScript); // опционально
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
    let registered = false;
    let panoramas = [];
    return {
      name: "pano360",
      allowInline: false,
      setup: (config) => {
        if (!registered) {
          registered = true;
          registerCss("pannellum.css");
          registerJs('pannellum.js').onload = async() => {
            for (let id = 0; id < panoramas.length; id++) {
              const configFile = panoramas[id];
              const config = await (await fetch(configFile)).json();
              config.default.basePath = (configFile.match(/^(.*)(\/)[^\/]*$/))[1] + "/";
              pannellum.viewer(`pano${id + 1}`, config);
            }
          }
        }
        panoramas.push(config);
        return `
<div class="panorama">
  <div id="pano${panoramas.length}"></div>
</div>`
      }
    }
  }

  function setLineMarkers(content) {
    let tags = []
    return content.replaceAll("\r\n", "\n")
      .split("\n")
      .map((li, num) => {
        let result = '';
        while(li.length !== 0) {
          if (tags.length !== 0) {
            const tag = tags.pop();
            let pos = li.indexOf(tag);
            if (pos !== -1) {
              result += li.substring(0, pos + tag.length);
              li = li.substring(pos + tag.length);
              continue;
            }
            tags.push(tag);
            result += li;
            li = '';
            break;
          }

          let match = li.match(/^(\s+|#+|\-+|\=+|\*+|\>+|\|+)/);
          match = match || li.match(/^(\d+\.)/)
          if (match) {
            result += match[1];
            li = li.substring(match[1].length);
            continue;
          }
          if (li.startsWith('{%')) {
            li = li.substring(2);
            result += "{%";
            tags.push("%}");
            continue;
          }
          if (li.startsWith('```')) {
            li = li.substring(3);
            result += "```";
            tags.push("```");
            continue;
          }
          match = li.match(/^<(br|hr)(\s*\/)?>/)
          if (match) {
            li = li.substring(match[0].length);
            result += match[0];
            continue;
          }

          match = li.match(/^<([^\s\/>]+)([^\/]*)>/)
          if (match) {
            li = li.substring(match[0].length);
            result += match[0];
            tags.push(`</${match[1]}>`);
            continue;
          }
          break;
        }
        if (li !== "") {
          result += (tags.length === 0)
            ? `MLINE:${num}:` + li
            : li
        }
        return result;
      })
      .join("\r\n");
  }
  function applyLineMarkers(html) {
    return html.replaceAll(/MLINE:(\d+):/g, '<a id="LINE$1"></a>');
  }

  return importMarkdown;
})();
