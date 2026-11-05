import { Injectable } from "@angular/core";

interface CacheEntry {
  response: unknown;
  expiry: number;
}

/**
 * Simple in-memory HTTP cache service.
 */
@Injectable({ providedIn: "root" })
export class HttpCacheService {
  private readonly cache = new Map<string, CacheEntry>();
  private readonly generations = new Map<string, number>();

  /**
   * Retrieves a cached response if it exists and is not expired.
   * @param key Cache key
   * @returns Cached response or null if not found/expired
   */
  get<T>(key: string): T | null {
    const entry = this.cache.get(key);
    if (!entry) return null;
    if (Date.now() > entry.expiry) {
      this.cache.delete(key);
      return null;
    }
    return entry.response as T;
  }

  /**
   * Caches a response with a specified TTL.
   * @param key Cache key
   * @param response Response to cache
   * @param ttlMs Time to live in milliseconds
   */
  set(key: string, response: unknown, ttlMs: number): void {
    this.cache.set(key, {
      response,
      expiry: Date.now() + ttlMs,
    });
  }

  /**
   * How many times `pattern` has been invalidated.
   *
   * Deleting entries is not enough on its own: a GET that was already in flight when a write
   * landed will arrive afterwards carrying pre-write data, and would otherwise repopulate the
   * cache and pin stale data for the whole TTL. Callers read this before issuing a request and
   * compare on response - a changed value means a write raced the read, so it must not be cached.
   * @param pattern The invalidation pattern to read the counter for
   * @returns The current generation, starting at 0
   */
  generation(pattern: string): number {
    return this.generations.get(pattern) ?? 0;
  }

  /**
   * Invalidates cache entries matching the given pattern.
   * @param pattern String or RegExp to match cache keys
   */
  invalidate(pattern: string | RegExp): void {
    if (typeof pattern === "string") {
      this.generations.set(pattern, this.generation(pattern) + 1);
    }

    const keys = Array.from(this.cache.keys());
    for (const key of keys) {
      if (typeof pattern === "string" ? key.includes(pattern) : pattern.test(key)) {
        this.cache.delete(key);
      }
    }
  }

  /**
   * Clears the entire cache.
   */
  clear(): void {
    this.cache.clear();
    this.generations.clear();
  }
}
