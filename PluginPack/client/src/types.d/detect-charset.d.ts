declare module 'detect-charset' {
  export default function detect_charset(buffer: byte[]|Uint8Array): string;
}