const { merge } = require('webpack-merge');
const path = require('path');
const baseConfig = require('./webpack.prod.config.js');

module.exports = merge(baseConfig, {
  entry: [path.resolve(__dirname, './src/markdown.ts')],
  output: {
    path: path.resolve(__dirname, '../dist/js'),
    filename: 'markdown.min.js',
    publicPath: "./"
  },
});