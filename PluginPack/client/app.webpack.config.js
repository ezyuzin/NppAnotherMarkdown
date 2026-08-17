const { merge } = require('webpack-merge');
const path = require('path');
const fs = require('fs');
const baseConfig = require('./webpack.prod.config.js');

function moduleResolve(file) {
  if (fs.existsSync(path.resolve(__dirname, `../node_modules/${file}`))) {
    return path.resolve(__dirname, `../node_modules/${file}`);
  }
  if (fs.existsSync(path.resolve(__dirname, `node_modules/${file}`))) {
    return path.resolve(__dirname, `node_modules/${file}`);
  }
  throw `module ${file} not found in node_modules. Run "yarn install" first.`
}

module.exports = function() {
  const outDir = path.resolve(__dirname, '../dist/js');
  const modules = [
    "mermaid/dist/mermaid.min.js",
  ];
  for(let module of modules) {
    const name = path.basename(module);
    console.log(`copy ${name} to dist/js/${name}`);
    fs.copyFileSync(moduleResolve(module), path.join(outDir, name));
  }

  console.log(`compile ./src/markdown.ts ...`);
  return merge(baseConfig, {
    entry: [path.resolve(__dirname, './src/markdown.ts')],
    output: {
      path: outDir,
      filename: 'markdown.min.js',
      publicPath: "./"
    },
  });
}