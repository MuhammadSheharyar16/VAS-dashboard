import en from './en.json';

type Dictionary = { [key: string]: string | Dictionary };

const dictionary: Dictionary = en;

function lookup(key: string): string | undefined {
  let node: string | Dictionary | undefined = dictionary;

  for (const part of key.split('.')) {
    if (typeof node !== 'object') {
      return undefined;
    }

    node = node[part];
  }

  return typeof node === 'string' ? node : undefined;
}

/** True when the key has a translation. Used for labels that come from data, such as a KPI key. */
export function hasTranslation(key: string): boolean {
  return lookup(key) !== undefined;
}

/**
 * Returns the text for a key, filling `{name}` placeholders. A missing key returns the key itself,
 * so a gap is visible on screen and in tests instead of rendering nothing.
 */
export function t(key: string, values: Record<string, string | number> = {}): string {
  const template = lookup(key) ?? key;
  return template.replace(/\{(\w+)\}/g, (match, name: string) =>
    name in values ? String(values[name]) : match,
  );
}

/** Text for a value that comes from the API (an outcome, a stage). Falls back to the value itself. */
export function tOr(key: string, fallback: string): string {
  return lookup(key) ?? fallback;
}
