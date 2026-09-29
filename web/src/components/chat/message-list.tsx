"use client";

import { useEffect, useRef, useState } from "react";
import { AnimatePresence, motion } from "motion/react";
import { ArrowDown } from "lucide-react";
import type { ChatMessage } from "@/lib/types";
import { MessageItem } from "@/components/chat/message-item";
import { Button } from "@/components/ui/button";

export function MessageList({
  messages,
  streaming,
  onOpenSources,
}: {
  messages: ChatMessage[];
  streaming: boolean;
  onOpenSources: (citations: ChatMessage["citations"]) => void;
}) {
  const scrollRef = useRef<HTMLDivElement>(null);
  const bottomRef = useRef<HTMLDivElement>(null);
  const [atBottom, setAtBottom] = useState(true);

  useEffect(() => {
    const element = scrollRef.current;
    if (!element) {
      return;
    }

    function handleScroll() {
      if (!scrollRef.current) {
        return;
      }
      const { scrollTop, scrollHeight, clientHeight } = scrollRef.current;
      setAtBottom(scrollHeight - scrollTop - clientHeight < 130);
    }

    element.addEventListener("scroll", handleScroll, { passive: true });
    return () => element.removeEventListener("scroll", handleScroll);
  }, []);

  useEffect(() => {
    if (atBottom) {
      bottomRef.current?.scrollIntoView();
    }
  }, [messages, atBottom]);

  return (
    <div className="relative min-h-0 flex-1">
      <div
        ref={scrollRef}
        className="scrollbar-thin h-full overflow-y-auto scroll-smooth"
      >
        <div className="mx-auto flex w-full max-w-3xl flex-col gap-6 px-4 py-6 lg:px-6">
          {messages.map((message, index) => (
            <MessageItem
              key={message.id + index}
              message={message}
              onOpenSources={onOpenSources}
            />
          ))}
          <div ref={bottomRef} className="h-px" />
        </div>
      </div>

      <AnimatePresence>
        {!atBottom && (
          <motion.div
            initial={{ opacity: 0, y: 8 }}
            animate={{ opacity: 1, y: 0 }}
            exit={{ opacity: 0, y: 8 }}
            className="absolute bottom-4 left-1/2 -translate-x-1/2"
          >
            <Button
              size="sm"
              variant="secondary"
              className="rounded-full shadow-lg backdrop-blur"
              onClick={() => {
                setAtBottom(true);
                bottomRef.current?.scrollIntoView({ behavior: "smooth" });
              }}
            >
              <ArrowDown className="size-3.5" />
              Latest
              {streaming ? (
                <span className="absolute right-0 top-0 size-2 animate-pulse rounded-full bg-primary" />
              ) : null}
            </Button>
          </motion.div>
        )}
      </AnimatePresence>
    </div>
  );
}
