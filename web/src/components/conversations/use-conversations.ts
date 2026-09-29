"use client";

import { useCallback, useEffect, useState } from "react";
import { api } from "@/lib/api";
import type { ConversationResponse } from "@/lib/types";

export const CONVERSATIONS_CHANGED_EVENT = "conversations-changed";

export function notifyConversationsChanged() {
  window.dispatchEvent(new Event(CONVERSATIONS_CHANGED_EVENT));
}

export function useConversations() {
  const [conversations, setConversations] = useState<ConversationResponse[] | null>(null);
  const [error, setError] = useState<string | null>(null);

  const refresh = useCallback(async () => {
    try {
      setConversations(await api.listConversations());
      setError(null);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load conversations");
    }
  }, []);

  useEffect(() => {
    void refresh();
    const handler = () => void refresh();
    window.addEventListener(CONVERSATIONS_CHANGED_EVENT, handler);
    return () => window.removeEventListener(CONVERSATIONS_CHANGED_EVENT, handler);
  }, [refresh]);

  return { conversations, error, refresh };
}
