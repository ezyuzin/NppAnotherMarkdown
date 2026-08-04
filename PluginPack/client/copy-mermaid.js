// Copies the prebuilt mermaid UMD bundle into dist/js, so the packaging step
// (see .github/workflows/build.yml) deploys it to assets/markdown next to
// markdown.min.js, the same way the highlightjs runtime is handled.
//
// Why a copy and not a webpack entry (as used for highlightjs): mermaid loads
// its diagram types with dynamic import(), so bundling it would emit lazy chunks
// that the runtime script loader never fetches. The published UMD build already
// inlines everything and assigns window.mermaid, so a plain copy is correct.
const fs = require('fs');
const path = require('path');

const candidates = [
  path.resolve(__dirname, '../node_modules/mermaid/dist/mermaid.min.js'),
  path.resolve(__dirname, 'node_modules/mermaid/dist/mermaid.min.js'),
];

const src = candidates.find(p => fs.existsSync(p));
if (!src) {
  console.error('mermaid.min.js not found in node_modules. Run "yarn install" first.');
  process.exit(1);
}

const outDir = path.resolve(__dirname, '../dist/js');
fs.mkdirSync(outDir, { recursive: true });
fs.copyFileSync(src, path.join(outDir, 'mermaid.min.js'));
console.log('copied mermaid.min.js to dist/js/mermaid.min.js');
