const cssScripts = [
 "https://cdn.jsdelivr.net/npm/pannellum@2.5.6/build/pannellum.css"
].map(li => {
  const link = document.createElement("link");
  link.href = li;
  link.rel = 'stylesheet';
  document.head.appendChild(link);
});

const markdownScripts = [
 "https://cdn.jsdelivr.net/npm/pannellum@2.5.6/build/pannellum.js"
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

  async function init(container, url) {
    await Promise.all(markdownScripts);
    const config = await (await fetch(url)).json();
    config.default.basePath = "./";
    pannellum.viewer(container, config);
  }

  return init;
})();
