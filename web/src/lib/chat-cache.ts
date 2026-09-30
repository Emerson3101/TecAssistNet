import type { ChatMessage } from "@/lib/types";

interface CachedConversation {
  messages: ChatMessage[];
  savedAt: number;
}

const CACHE_TTL_MS = 5 * 60 * 1000;
const cache = new Map<string, CachedConversation>();

export function cacheConversation(id: string, messages: ChatMessage[]) {
  cache.set(id, { messages, savedAt: Date.now() });
}

export function takeCachedConversation(id: string): ChatMessage[] | null {
  const entry = cache.get(id);
  cache.delete(id);
  if (!entry || Date.now() - entry.savedAt > CACHE_TTL_MS) {
    return null;
  }
  return entry.messages;
}
