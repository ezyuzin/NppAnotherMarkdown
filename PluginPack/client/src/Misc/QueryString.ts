import { Hashmap } from "../Lib/Common/Hashmap";

export function parseQueryString(str: string) {
  const matches = ` ${str}`.matchAll(/[\s](\w+)=['"](.*?)['"]/g)

  return Array.from(matches).reduce((acc, li) => {
      let key = li[1].trim();
      let value = li[2];
      if (value.match(/^"(.*)"$/)) {
        value = value.substring(1, value.length - 1);
      }
      else if (value.match(/^'(.*)'$/)) {
        value = value.substring(1, value.length - 1);
      }
      acc[key] = value;
      return acc;
    }, {} as Hashmap<string>);
}