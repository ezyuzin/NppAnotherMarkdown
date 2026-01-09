const path = require('path');

module.exports = {
  entry: [path.resolve(__dirname, './src/index.tsx')],
  output: {
    path: path.resolve(__dirname, '../dist/html/js'),
    filename: 'app.js',
    publicPath: "/js"
  },
  devtool: 'source-map', // Generates .map files
  module: {
    rules: [
      {
        test: /\.(js|ts)x?$/,
        use: {
          loader: 'babel-loader',
          options: {
            configFile: path.resolve(__dirname, 'babel.config.json')
          },
        },
        exclude: /node_modules/,
        include: [
          path.join(__dirname, "./src")
        ]
      }
    ],
  },
  performance: {
    hints: false,
  },
  plugins: [],
  resolve: {
    extensions: ['.tsx', '.ts', '.jsx', '.js'],
    alias: {
      '@': path.resolve(__dirname, 'src/')
    }
  },
  optimization: {
    //minimize: true,   -- раскомментируй для минификации в dev режиме
  },
  mode: 'development'
}