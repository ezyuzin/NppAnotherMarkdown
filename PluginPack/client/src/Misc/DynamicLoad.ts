const loaded: string[] = [];

export function importCss(scripts: string[]) {
  for(let path of scripts) {
    if (loaded.includes(path)) {
      continue;
    }
    loaded.push(path);
    const link = document.createElement("link");
    link.href = 'http://assets.example/' + path;
    link.rel = 'stylesheet';
    document.head.appendChild(link);
  }
}

export async function importScript(scripts: string[]) {
  const promises = scripts
    .filter(li => !loaded.includes(li))
    .map(path => {
      loaded.push(path);
      if (path.endsWith('.js')) {
        const script = document.createElement("script");
        script.src = 'http://assets.example/' + path;
        script.defer = true;
        return new Promise<void>((resolve) => {
          script.onload = () => resolve();
          document.head.appendChild(script);
        });
      }
      else {
        return null;
      }
    })
    .filter(li => li != null);

  if (promises.length != 0) {
    await Promise.all(promises);
  }
}

export async function DynamicScriptsProcessor(container: HTMLElement) {
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