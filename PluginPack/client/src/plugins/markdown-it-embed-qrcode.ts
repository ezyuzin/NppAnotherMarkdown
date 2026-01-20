import QRCode, { QRCodeToDataURLOptions } from 'qrcode'
import { parseQueryString } from '../Misc/QueryString';
import { Hashmap } from '../Lib/Common/Hashmap';
import { MarkdownRenderContext } from '../Misc/MarkdownRenderContext';

const data = {
  seq: 0,
  entries: [] as {
    active: boolean,
    text: string,
    options: string,
    value: string
  }[]
}

let loading: Promise<void>|null = null;

class MarkdownItEmbedQrcode {
  constructor() {
  }

  CreateQRCodeImage(args: Hashmap<string>) {
    const options: QRCodeToDataURLOptions = {
      margin: Number.parseInt(args['margin']) || 2,
      color: {
        dark: args['color'] || '#0277bd',
        light: args['background'] || '#ffffff'
      }
    }
    for (let entry of data.entries) {
      if (entry.text === args.text) {
        if (entry.options === JSON.stringify(options)) {
          entry.active = true;
          return entry.value;
        }
      }
    }

    return new Promise<string>(async (resolve, error) => {
      await loading;

      QRCode.toDataURL(args['text'], options, (err, value) => {
        if (err) {
          error(err);
          return;
        }
        data.entries.push({
          text: args.text,
          active: true,
          options: JSON.stringify(options),
          value
        });

        resolve(value);
      });
    });
  }

  public Render(config: string) {
    const context = MarkdownRenderContext;
    const args = parseQueryString(config);

    const qrcode = this.CreateQRCodeImage(args);
    let block = `<img class="qrcode" alt="${args.text}" `;
    if (args['style']) {
      block += `style="${args['style']}" `;
    }
    if (qrcode instanceof Promise) {
      const id = `qrcode${++data.seq}`;
      block += `id='${id}' `;
      (async () => {
        await context.documentReady;
        (document.getElementById(id) as HTMLImageElement).src = await qrcode;
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


export default function markdownItEmbedQrcode() {
  data.seq = 0;
  data.entries.forEach(li => li.active = false);
  const qrcode = new MarkdownItEmbedQrcode();

  return {
    name: "qrcode",
    allowInline: true,
    setup: (config: string) => qrcode.Render(config)
  }
}
