import { Hashmap } from '../Lib/Common/Hashmap';
import { importCss, importScript } from '../Misc/DynamicLoad';
import { MarkdownRenderContext } from '../Misc/MarkdownRenderContext';

const scenes: Hashmap<any> = {};
const loader: Promise<void>|null = null;

class MarkdownItEmbedPano360 {
  constructor() {
  }

  public Render(sourceFile: string) {
    const panoramaId = ++this.seqId;
    const sceneId = `pano360.scene[${panoramaId}]`;
    if (scenes[sceneId]) {
      const element = document.getElementById(`pano${panoramaId}`)!;
      scenes[sceneId].div = element;
      element.parentElement!.removeChild(element);
    }

    MarkdownRenderContext.postRender.push(async () => {
      try {
        importCss(['markdown/pannellum.css']);
        const panellum = importScript(['markdown/pannellum.js']);

        let config = await readConfig(sourceFile);
        await panellum;

        const scene = {
          elementId: `pano${panoramaId}`,
          configText: JSON.stringify(config)
        }

        if (scenes[sceneId]) {
          if (scenes[sceneId].div && scenes[sceneId].configText === scene.configText) {
            const element = document.getElementById(scene.elementId);
            if (element) {
              const parentElement = element.parentElement!;
              parentElement.removeChild(element);
              parentElement.appendChild(scenes[sceneId].div);
              delete scenes[sceneId].div;
              return;
            }
          }
        }

        scenes[sceneId] = scene;
        ((window as any).pannellum as any).viewer(`pano${panoramaId}`, config);
      }
      catch (err) {
        console.error({ err });
      }
    });

    return `
<div class="panorama">
  <div id="pano${panoramaId}"></div>
</div>`

  }
  private seqId: number = 0;
}

async function readConfig(sourceFile: string) {
  let config;
  if (/\.json$/.test(sourceFile)) {
    config = await (await fetch(sourceFile)).json();
    config.default.basePath = (sourceFile.match(/^(.*)(\/)[^\/]*$/)!)[1] + "/";
  }
  else {
    config = {
      "default": {
        "firstScene": "default",
        "sceneFadeDuration": 1000,
        "autoLoad": true,
        "showZoomCtrl": false,
        "compass": false,
        "autoRotate": 0,
        "minHfov": 5,
        "maxHfov": 120
      },
      "scenes": {
        "default": {
          "type": "equirectangular",
          "panorama": sourceFile
        }
      }
    };
  }
  return config;
}

export default function markdownItEmbedPano360() {
  const builder = new MarkdownItEmbedPano360();

  return {
    name: "pano360",
    allowInline: false,
    setup: (config: string) => builder.Render(config)
  }
}
