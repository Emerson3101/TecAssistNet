"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import { useRouter } from "next/navigation";
import { toast } from "sonner";
import { api, ApiError } from "@/lib/api";
import { streamChatAnswer } from "@/lib/sse";
import { cacheConversation, takeCachedConversation } from "@/lib/chat-cache";
import type { ChatMessage } from "@/lib/types";
import { ChatBackdrop } from "@/components/chat/chat-backdrop";
import { notifyConversationsChanged } from "@/components/conversations/use-conversations";
import { useSettings } from "@/components/settings-provider";
import { MessageList } from "@/components/chat/message-list";
import { Composer } from "@/components/chat/composer";
import { EmptyState } from "@/components/chat/empty-state";
import { SourcesSheet } from "@/components/chat/sources-sheet";

function applyDoneIds(
  previous: ChatMessage[],
  ids: { userMessageId: string; assistantMessageId: string }
): ChatMessage[] {
  if (!previous.length) {
    return previous;
  }
  const copy = [...previous];
  const last = copy.length - 1;
  copy[last] = { ...copy[last], id: ids.assistantMessageId, streaming: false };
  if (copy[last - 1]?.role === "user") {
    copy[last - 1] = { ...copy[last - 1], id: ids.userMessageId };
  }
  return copy;
}

export function ChatView({ conversationId }: { conversationId?: string }) {
  const router = useRouter();
  const [messages, setMessages] = useState<ChatMessage[] | null>(() =>
    conversationId ? takeCachedConversation(conversationId) : []
  );
  const [composerValue, setComposerValue] = useState("");
  const [streaming, setStreaming] = useState(false);
  const [sources, setSources] = useState<{
    citations: ChatMessage["citations"];
  } | null>(null);
  const abortRef = useRef<AbortController | null>(null);
  const draftConversationIdRef = useRef<string | null>(null);
  const latestMessagesRef = useRef<ChatMessage[] | null>(messages);
  const { animatedBackground } = useSettings();

  useEffect(() => {
    latestMessagesRef.current = messages;
  }, [messages]);

  useEffect(() => {
    if (!conversationId) {
      return;
    }

    let cancelled = false;
    api
      .getMessages(conversationId)
      .then((history) => {
        if (!cancelled) {
          setMessages(
            history.map((message) => ({
              id: message.id,
              role: message.role,
              content: message.content,
              createdAt: message.createdAt,
              citations: message.citations,
            }))
          );
        }
      })
      .catch((error: unknown) => {
        if (!cancelled) {
          setMessages([]);
          toast.error(
            error instanceof Error ? error.message : "Failed to load messages."
          );
        }
      });
    return () => {
      cancelled = true;
    };
  }, [conversationId]);

  const updateLast = useCallback(
    (update: (message: ChatMessage) => ChatMessage) => {
      setMessages((previous) => {
        if (!previous?.length) {
          return previous;
        }
        const copy = [...previous];
        copy[copy.length - 1] = update(copy[copy.length - 1]);
        return copy;
      });
    },
    []
  );

  const send = useCallback(
    async (rawContent: string) => {
      const content = rawContent.trim();
      if (!content || streaming) {
        return;
      }

      const now = new Date().toISOString();
      const userMessage: ChatMessage = {
        id: crypto.randomUUID(),
        role: "user",
        content,
        createdAt: now,
        citations: [],
      };
      const assistantMessage: ChatMessage = {
        id: crypto.randomUUID(),
        role: "assistant",
        content: "",
        createdAt: now,
        citations: [],
        streaming: true,
      };
      setMessages((previous) => [...(previous ?? []), userMessage, assistantMessage]);
      setComposerValue("");
      setStreaming(true);

      const controller = new AbortController();
      abortRef.current = controller;

      try {
        let activeId = conversationId ?? draftConversationIdRef.current;
        if (!activeId) {
          const conversation = await api.createConversation();
          draftConversationIdRef.current = conversation.id;
          activeId = conversation.id;
          notifyConversationsChanged();
        }

        await streamChatAnswer(
          activeId,
          content,
          {
            onToken: (token) =>
              updateLast((message) => ({
                ...message,
                content: message.content + token,
              })),
            onCitation: (citation) =>
              updateLast((message) => ({
                ...message,
                citations: [...message.citations, citation],
              })),
            onDone: (ids) => {
              setMessages((previous) =>
                previous ? applyDoneIds(previous, ids) : previous
              );

              const base = latestMessagesRef.current;
              if (base && base.length >= 2) {
                cacheConversation(activeId, applyDoneIds(base, ids));
              }

              notifyConversationsChanged();
            },
          },
          controller.signal
        );

        setStreaming(false);
        abortRef.current = null;
        if (!conversationId && draftConversationIdRef.current) {
          const draftedId = draftConversationIdRef.current;
          router.push(`/chat/${draftedId}`);
          void api
            .generateConversationTitle(draftedId)
            .then(() => notifyConversationsChanged())
            .catch(() => undefined);
        }
      } catch (error) {
        setStreaming(false);
        abortRef.current = null;

        if (error instanceof DOMException && error.name === "AbortError") {
          updateLast((message) => ({ ...message, streaming: false }));
          return;
        }

        const messageText =
          error instanceof Error ? error.message : "Something went wrong.";

        if (error instanceof ApiError && error.status === 404) {
          toast.error("That conversation no longer exists.");
          router.push("/chat");
          return;
        }

        setMessages((previous) => {
          if (!previous?.length) {
            return previous;
          }
          const copy = [...previous];
          const last = copy[copy.length - 1];
          if (last.role === "assistant" && !last.content) {
            copy.pop();
          } else {
            copy[copy.length - 1] = {
              ...copy[copy.length - 1],
              streaming: false,
              error: true,
            };
          }
          return copy;
        });
        toast.error(messageText);
      }
    },
    [conversationId, router, streaming, updateLast]
  );

  const stop = useCallback(() => {
    abortRef.current?.abort();
  }, []);

  const hasMessages = (messages?.length ?? 0) > 0;

  return (
    <div className="relative flex min-h-0 flex-1 flex-col">
      {animatedBackground && <ChatBackdrop subtle={hasMessages} />}

      {hasMessages ? (
        <MessageList
          messages={messages ?? []}
          streaming={streaming}
          onOpenSources={(citations) => setSources({ citations })}
        />
      ) : (
        <EmptyState
          loading={messages === null}
          onSuggest={(suggestion) => {
            setComposerValue(suggestion);
          }}
        />
      )}

      <div className="relative">
        <div className="pointer-events-none absolute -top-12 bottom-0 left-0 right-0 h-12 bg-gradient-to-t from-background to-transparent" />
        <Composer
          value={composerValue}
          onChange={setComposerValue}
          onSend={send}
          onStop={stop}
          streaming={streaming}
        />
      </div>

      <SourcesSheet
        open={sources !== null}
        onOpenChange={(open) => !open && setSources(null)}
        citations={sources?.citations ?? []}
      />
    </div>
  );
}
