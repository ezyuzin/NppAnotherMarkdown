window.importMarkdown = (() => {
  let context = {};
  
  async function importMarkdown(container, url, options) {
    options = {
      lineMark: false,
      ...(options || {})
    }

    const response = await fetch(url);
    const data = await response.arrayBuffer();
    const decoder = new TextDecoder(detect_charset(new Uint8Array(data)));
    let source = decoder.decode(data);

    if (options.lineMark) {
      let block = null;
      source = source.replaceAll("\r\n", "\n")
        .split("\n")
        .map((li, num) => {
          if (block === null) {
            if (li.match(/^```/)) {
              block = '```'
              return li;
            }
            const match = li.match(/^<(script|style)>/);
            if (match) {
              block = `</${match[1]}>`
              return li;
            }            
          }
          if (block !== null) {
            if (li.includes(block)) {
              blockStarted = null;
            }
            return li;
          }          
          if (li.trim().length === 0) {
            return li;
          }

          if (li.match(/^{%/)) { // embedded blocks
            return li;
          }
          if (li.match(/^[\s\|\-]+$/)) { // table definition
            return li;
          }
          if (li.match(/^([=]+|[\-]+|[\*]+)$/)) { // quote definition
            return li;
          }
          let match = li.match(/^(\s+|[|]\s+|[#]+\s+|[>]\s+|[\*\+\-]\s+|\s*\d+\.\s)(.*)$/);
          if (match) {
            return `${match[1]}<span id="LINE${num}"></span>${match[2]}`
          }
          return `<span id="LINE${num}"></span>` + li;
        })
        .join("\r\n");
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

    container.innerHTML = md.render(source);
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
    return ` ${str}`.matchAll(/[\s](\w+)=['"](.*?)['"]/g)
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
  return importMarkdown;
})();
