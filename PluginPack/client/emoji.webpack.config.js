const { merge } = require('webpack-merge');
const path = require('path');
const baseConfig = require('./webpack.prod.config.js');

module.exports = merge(baseConfig, {
  entry: [path.resolve(__dirname, './src/emoji.tsx')],
  output: {
    path: path.resolve(__dirname, '../dist/js'),
    filename: 'markdown-it-emoji@3.0.0.min.js',
    publicPath: "./"
  },
});