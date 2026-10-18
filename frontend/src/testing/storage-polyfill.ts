/**
 * Lightweight Storage polyfill for the Angular unit-test runner.
 * The default Vitest environment ('node') only provides stub objects for
 * window.localStorage; here we replace them with a real Map-backed
 * implementation so production code that uses getItem/setItem/removeItem
 * works during tests.
 */
class MemoryStorage implements Storage {
  private store = new Map<string, string>();
  get length(): number { return this.store.size; }
  clear(): void { this.store.clear(); }
  getItem(key: string): string | null { return this.store.get(key) ?? null; }
  key(i: number): string | null { return Array.from(this.store.keys())[i] ?? null; }
  removeItem(key: string): void { this.store.delete(key); }
  setItem(key: string, value: string): void { this.store.set(key, String(value)); }
}

export function installStoragePolyfill(): void {
  const memory = new MemoryStorage();
  Object.defineProperty(globalThis, 'localStorage', { value: memory, configurable: true });
  Object.defineProperty(globalThis, 'sessionStorage', { value: new MemoryStorage(), configurable: true });
}
