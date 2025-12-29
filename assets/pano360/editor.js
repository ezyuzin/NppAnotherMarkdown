[
 "http://assets.example/pano360/pannellum/pannellum.css",
 "http://assets.example/pano360/editor.css"
].map(li => {
  const link = document.createElement("link");
  link.href = li;
  link.rel = 'stylesheet';
  document.head.appendChild(link);
});

const dependencies = [
 "http://assets.example/pano360/pannellum/libpannellum.js",
 "http://assets.example/pano360/pannellum/pannellum.js"
].map(li => {
  const script = document.createElement("script");
  script.src = li;
  script.defer = true;
  document.head.appendChild(script);
  return new Promise((resolve) => {
    script.onload = () => resolve();
  });
});

const editorConstructor = function(container, config) {
  let viewer;
  let hotspotSeq = 0;
  let onSave = [];

  function setDocument(config) {
    viewer.setConfig(config);
  }

  function addHotspot() {
    viewer.addHotSpot({
      "pitch": viewer.getPitch(),
      "yaw": viewer.getYaw(),
      "type": "info",
      "text": `Hotspot ${++hotspotSeq}`
    })
  }

  function getDocument() {
    const current = viewer.getConfig();
    const data = {
      default: { ...current.default },
      scenes: { ... current.scenes }
    };
    delete data.default.hotSpotDebug;
    for(let entry of Object.entries(data.scenes)) {
      let scene = entry[1];
      data.scenes[entry[0]] = {
        ...scene,
        hotSpots: (scene.hotSpots || []).map(hotspot => {
          hotspot = { ...hotspot };
          delete hotspot.div;
          if (`${hotspot.url}` === '') {
            delete hotspot.url;
          }
          if (`${hotspot.sceneId}` === '') {
            delete hotspot.sceneId;
          }
          return hotspot;
        })
      }
    }
    return data;
  }

  async function save() {
    onSave.forEach(h => h());
  }

  function findHotspot(emouse) {
    const pos = viewer.mouseEventToCoords(emouse);
    const sceneId = viewer.getScene();
    const config = viewer.getConfig();
    const scene = config.scenes[sceneId];
    if (!scene.hotSpots) {
      return null;
    }

    for(const entry of scene.hotSpots) {
      const dx =  (entry.yaw - pos[1]);
      const dy =  (entry.pitch - pos[0]);
      const r2 = (dx*dx) + (dy*dy);
      if (r2 < 1) {
        return entry
        break;
      }
    }
    return null;
  }

  function editHotspot(hotspot, dialog) {
    dialog.innerHTML = `
      <div class='form-row'>
        <span>Title:</span>
        <input id="hotspot-name" />
      </div>
      <div class='form-row'>
        <span>Url:</span>
        <input id="hotspot-url" />
      </div>
      <div class='form-row'>
        <span>Scene:</span>
        <select id="hotspot-scene"></select>
      </div>

      <hr/>
      <div style="text-align: right;">
      <button id="cancel">Cancel</button>&nbsp;<button id="save">OK</button>
      </div>
      `;

    const config = viewer.getConfig();

    for(let scene of [["", { title: "" }], ...Object.entries(config.scenes)]) {
      const option = document.createElement('option');
      option.value = scene[0];
      option.textContent = scene[1].title || "";
      dialog.querySelector('#hotspot-scene').appendChild(option);
    }
    dialog.querySelector('#hotspot-scene').addEventListener('keydown', e => e.stopPropagation());
    dialog.querySelector('#hotspot-scene').value = hotspot.sceneId;


    dialog.querySelector('#hotspot-name').addEventListener('keydown', (e) => {
      e.stopPropagation();
    });
    dialog.querySelector('#hotspot-url').addEventListener('keydown', (e) => {
      e.stopPropagation();
    });

    dialog.querySelector('#hotspot-name').value = hotspot.text;
    dialog.querySelector('#hotspot-url').value = hotspot.url || "";

    dialog.querySelector('#save')?.addEventListener('click', (e) => {
      e.preventDefault();
      hotspot.text = dialog.querySelector('#hotspot-name').value;
      hotspot.url = dialog.querySelector('#hotspot-url').value;
      hotspot.sceneId = dialog.querySelector('#hotspot-scene').value;
      viewer.updateHotspot(hotspot);
      viewer.closeContextMenu();
    });

    dialog.querySelector('#cancel')?.addEventListener('click', (e) => {
      e.preventDefault();
      viewer.closeContextMenu();
    });
  }

  async function addScene(dialog) {
    dialog.innerHTML = `
      <div class='form-row'>
        <span>Title:</span>
        <input id="name" />
      </div>
      <div class='form-row'>
        <span>Panorama:</span>
        <select id="panorama"></select>
      </div>
      <hr/>
      <div style="text-align: right;">
      <button id="cancel">Cancel</button>&nbsp;<button id="ok">OK</button>
      </div>
      `;

    const panos = await (await (fetch(location + "/pano*.jpg"))).json();
    const exists = Object.entries(viewer.getConfig().scenes).map(li => li[1].panorama);

    for(let pano of panos) {
      if (exists.includes(pano)) {
        continue;
      }
      const option = document.createElement('option');
      option.value = pano;
      option.textContent = pano;
      dialog.querySelector('#panorama').appendChild(option);
    }
    dialog.querySelector('#panorama').addEventListener('keydown', e => e.stopPropagation());


    dialog.querySelector('#name').addEventListener('keydown', (e) => {
      e.stopPropagation();
    });

    dialog.querySelector('#ok')?.addEventListener('click', (e) => {
      e.preventDefault();
      const image = dialog.querySelector('#panorama').value;
      if (`${image}` !== "") {
        const options = {
          "title": dialog.querySelector('#name').value,
          "type": "equirectangular",
          "panorama": image,
          "pitch": 0,
          "yaw": 0,
          "hfov": 70,
          "hotSpots": []
        }

        viewer.addScene(image, options)
        viewer.loadScene(image);
      }
      viewer.closeContextMenu();
    });

    dialog.querySelector('#cancel')?.addEventListener('click', (e) => {
      e.preventDefault();
      viewer.closeContextMenu();
    });
  }

  function contextMenu(e, pos) {
    var menu = document.createElement('div');
    menu.className = 'context-menu';

    const hotspot = findHotspot(e);
    let menuItems = [];
    if (hotspot) {
      menuItems.push('<a id="contextmenu-edit">Edit</a>');
      menuItems.push('<a id="contextmenu-delete">Delete</a>')
    }
    else {
      menuItems.push('<a id="contextmenu-addhotspot">Add HotSpot</a>');
      menuItems.push('<a id="contextmenu-addscene">Add Scene</a>');
      menuItems.push('<a id="contextmenu-defaultsceneview">Set As Default Scene View</a>')
      menuItems.push('<hr/>')
      menuItems.push('<a id="contextmenu-save">Save</a>')
    }

    menu.innerHTML = menuItems.join("\n");

    menu.querySelector('#contextmenu-addscene')?.addEventListener('click', async (e) => {
      e.preventDefault();
      addScene(menu);
    });

    menu.querySelector('#contextmenu-defaultsceneview')?.addEventListener('click', (e) => {
      e.preventDefault();
      viewer.closeContextMenu();
      const sceneId = viewer.getScene();
      const config = viewer.getConfig();
      const scene = config.scenes[sceneId];
      scene.pitch = viewer.getPitch();
      scene.yaw = viewer.getYaw();
      scene.hfov = viewer.getHfov();
    });


    menu.querySelector('#contextmenu-save')?.addEventListener('click', (e) => {
      e.preventDefault();
      viewer.closeContextMenu();
      save();
    });
    menu.querySelector('#contextmenu-addhotspot')?.addEventListener('click', (e) => {
      e.preventDefault();
      viewer.closeContextMenu();
      addHotspot();
    });

    menu.querySelector('#contextmenu-edit')?.addEventListener('click', (e) => {
      e.preventDefault();
      editHotspot(hotspot, menu);
    });

    menu.querySelector('#contextmenu-delete')?.addEventListener('click', (e) => {
      e.preventDefault();
      viewer.closeContextMenu();
      hotspot.id = "deleted-hotspot";
      viewer.removeHotSpot("deleted-hotspot");
    });

    return menu;
  }

  viewer = pannellum.viewer(container, config);
  viewer.onContextMenu = contextMenu;

  return {
    viewer,
    setDocument,
    getDocument,
    onSave: (handler) => onSave.push(handler)
  }
}

window.viewPlugin = (() => {
  let context = {};

  async function save(editor, documentUrl) {
    const content = JSON.stringify(editor.getDocument(), null, 2);
    if (context['current.content'] === content) {
      return;
    }
    context['current.content'] = content;
    await fetch(documentUrl, {
      method: "PUT",
      headers: {
        "Content-Type": "application/json"
      },
      body: content
    });
  }

  const init = async (container, url, changed) => {
    await Promise.all(dependencies);
    const content = await (await fetch(url)).text();
    if (context['current.url'] === url && context['current.content'] === content) {
      return;
    }

    const updateView = (changed && context['current.editor'] && context['current.url'] === url);
    context['current.content'] = content;
    context['current.url'] = url;

    const config = JSON.parse(content);
    config.default.basePath = "./";
    config.default.hotSpotDebug = true;

    if (updateView) {
      context['current.editor'].setDocument(config);
    }
    else {
      container.innerHTML = "";
      const editor = editorConstructor(container, config);
      editor.onSave(() => save(editor, url));
      context['current.editor'] = editor;
    }
  }

  return init;
})();