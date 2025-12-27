window.viewLoader = (function() {
  function registerCss(cssFile) {
    const link = document.createElement("link");
    link.href = cssFile;
    link.rel = 'stylesheet';
    document.head.appendChild(link);    
  }
  function registerScript(src, defer = false) {
    const script = document.createElement("script");
    script.src = src;
    script.defer = defer;
    document.head.appendChild(script);
    return script;
  } 
  
  function markdown(sourceFile, cssFile, lineMark) {
    registerCss(cssFile);
    const script = registerScript('http://assets.example/markdown/markdown.js');
    let isLoaded = false;
    const documentChanged = () => {
      if (isLoaded) {
        importMarkdown(document.getElementById("content"), sourceFile, {
          lineMark
        });
      }
    }
    script.onload = () => {
      isLoaded = true;
      documentChanged();
    }
    
    return {
      documentChanged,
      dispose: () => {}      
    }
  }
  
  return function(sourceFile, cssFile, lineMark) {
    lineMark = (lineMark == "true" || lineMark === true);
    if (sourceFile.match(/\.(md)$/i)) {
      return markdown(sourceFile, cssFile, lineMark);      
    }
    console.log(`unsupported file extension: ${sourceFile}`);
    return {
      documentChanged: () => {},
      dispose: () => {}         
    }
  }
})();


