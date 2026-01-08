window.createView = async function (args) {
  let options = {
    document: "",
    css: "",
    lineMark: false
  }
  options = { ...options, ...args };

  const plugins = [
    [/\.(md)$/i, 'http://assets.example/markdown/markdown.js', options.css],
    [/\.pano360\.(json)$/i, 'http://assets.example/pano360/editor.js', null]
  ]

  for (let plugin of plugins) {
    if (options.document.match(plugin[0])) {
      if (plugin[2]) {
        const link = document.createElement("link");
        link.href = plugin[2];
        link.rel = 'stylesheet';
        document.head.appendChild(link);
      }
      const script = document.createElement("script");
      script.src = plugin[1];
      script.defer = true;
      const scriptLoad = new Promise((resolve) => {
        script.onload = () => resolve();
        document.head.appendChild(script);
      });
      await scriptLoad;

      const viewPlugin = window.viewPlugin;

      const documentChanged = async (modified = true) => {
        const args = options = {
          ...options,
          modified
        };
        await viewPlugin.setDocument(document.getElementById("content"), args);
      }

      await documentChanged(false);
      return {
        scrollToLine: viewPlugin.scrollToLine,
        documentChanged,
        dispose: () => { }
      }
    }
  }
  console.log(`unsupported file extension: ${source}`);
  return {
    scrollToLine: () => { },
    documentChanged: () => { },
    dispose: () => { }
  }
}
