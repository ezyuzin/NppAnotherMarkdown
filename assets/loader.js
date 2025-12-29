window.viewLoader = (function() {

  return function(source, css, lineMark) {
    lineMark = (lineMark == "true" || lineMark === true);
    
    const plugins = [
      [/\.(md)$/i, 'http://assets.example/markdown/markdown.js', css ],
      [/\.pano360\.(json)$/i, 'http://assets.example/pano360/editor.js', null]
    ]
    
    for(let plugin of plugins) {
      if (source.match(plugin[0])) {
        if (plugin[2]) {
          const link = document.createElement("link");
          link.href = plugin[2];
          link.rel = 'stylesheet';
          document.head.appendChild(link);
        }
        const script = document.createElement("script");
        script.src = plugin[1];
        script.defer = true;
        document.head.appendChild(script);

        let isLoaded = false;
        const documentChanged = (modified = false) => {
          if (isLoaded) {
            viewPlugin(document.getElementById("content"), source, {
              lineMark,
              modified
            });
          }
        }
        script.onload = () => {
          isLoaded = true;
          documentChanged(false);
        }

        return {
          documentChanged,
          dispose: () => {}
        }
      }
    }
    console.log(`unsupported file extension: ${sourceFile}`);
    return {
      documentChanged: () => {},
      dispose: () => {}
    }
  }
})();
