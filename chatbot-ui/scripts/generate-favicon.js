const sharp = require('sharp');
const fs = require('fs');
const path = require('path');

const svgBuffer = fs.readFileSync(path.join(__dirname, '../public/favicon.svg'));

// Generate favicon.ico (32x32)
sharp(svgBuffer)
  .resize(32, 32)
  .toFile(path.join(__dirname, '../public/favicon.ico'))
  .catch(console.error);

// Generate icon.png (32x32)
sharp(svgBuffer)
  .resize(32, 32)
  .png()
  .toFile(path.join(__dirname, '../public/icon.png'))
  .catch(console.error);

// Generate apple-icon.png (180x180)
sharp(svgBuffer)
  .resize(180, 180)
  .png()
  .toFile(path.join(__dirname, '../public/apple-icon.png'))
  .catch(console.error); 