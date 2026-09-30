"use client";

import { useState } from "react";
import { toast } from "sonner";
import { motion } from "motion/react";
import { AlertTriangle, Check, Copy, Sparkles, User } from "lucide-react";
import type { ChatMessage } from "@/lib/types";
import { Markdown } from "@/components/chat/markdown";
import { CitationChips } from "@/components/chat/citation-chips";
import { TypingIndicator } from "@/components/chat/typing-indicator";
import { formatRelativeTime } from "@/lib/format";

export function MessageItem({
  message,
  onOpenSources,
}: {
  message: ChatMessage;
  onOpenSources: (citations: ChatMessage["citations"]) => void;
}) {
  const [copied, setCopied] = useState(false);

  async function handleCopy() {
    try {
      await navigator.clipboard.writeText(message.content);
      setCopied(true);
      toast.success("Answer copied to clipboard.");
      setTimeout(() => setCopied(false), 1600);
    } catch {
      toast.error("Could not access the clipboard.");
    }
  }

  if (message.role === "user") {
    return (
      <motion.div
        initial={{ opacity: 0, y: 16, scale: 0.97, rotateX: -6 }}
        animate={{ opacity: 1, y: 0, scale: 1, rotateX: 0 }}
        transition={{ type: "spring", stiffness: 320, damping: 26 }}
        style={{ transformPerspective: 700 }}
        className="flex justify-end gap-3"
      >
        <motion.div
          whileHover={{ scale: 1.01 }}
          className="max-w-[85%] rounded-2xl rounded-br-md bg-gradient-to-br from-jade/90 to-sea/90 px-4 py-3 text-sm leading-relaxed text-black/85 shadow-lg shadow-jade/20"
        >
          <p className="whitespace-pre-wrap break-words">{message.content}</p>
        </motion.div>
        <div className="flex size-8 shrink-0 items-center justify-center rounded-xl border border-border/60 bg-secondary/70">
          <User className="size-4 text-muted-foreground" />
        </div>
      </motion.div>
    );
  }

  const waiting = message.streaming && !message.content;

  return (
    <motion.div
      initial={{ opacity: 0, y: 16, rotateX: -5 }}
      animate={{ opacity: 1, y: 0, rotateX: 0 }}
      transition={{ type: "spring", stiffness: 320, damping: 26 }}
      style={{ transformPerspective: 700 }}
      className="group flex gap-3"
    >
      <motion.div
        whileHover={{ rotate: 12, scale: 1.08 }}
        transition={{ type: "spring", stiffness: 300, damping: 18 }}
        className="flex size-8 shrink-0 items-center justify-center rounded-xl bg-gradient-to-br from-jade to-gold shadow-md shadow-jade/25"
      >
        <Sparkles className="size-4 text-black/85" />
      </motion.div>

      <div className="min-w-0 flex-1">
        <div
          className={`glass relative w-fit max-w-full rounded-2xl rounded-tl-md px-4 py-3 transition-shadow ${
            message.error
              ? "border-destructive/40"
              : "shadow-lg shadow-black/5"
          }`}
        >
          {waiting ? (
            <TypingIndicator />
          ) : (
            <div
              className={`text-sm text-foreground ${
                message.streaming ? "stream-caret" : ""
              }`}
            >
              <Markdown content={message.content} />
            </div>
          )}

          {message.error && (
            <div className="mt-2 flex items-center gap-2 text-xs text-destructive">
              <AlertTriangle className="size-3.5" />
              Generation was interrupted — the answer may be incomplete.
            </div>
          )}
        </div>

        {message.citations.length > 0 && (
          <motion.div
            initial={{ opacity: 0, y: 6 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ delay: 0.1 }}
            className="mt-2"
          >
            <CitationChips
              citations={message.citations}
              onOpen={() => onOpenSources(message.citations)}
            />
          </motion.div>
        )}

        <div className="mt-1.5 flex items-center gap-2 text-[11px] text-muted-foreground">
          <span>{formatRelativeTime(message.createdAt)}</span>
          {!message.streaming && message.content && (
            <button
              onClick={() => void handleCopy()}
              className="flex items-center gap-1 opacity-0 transition-opacity group-hover:opacity-100 hover:text-foreground"
            >
              {copied ? (
                <>
                  <Check className="size-3" /> Copied
                </>
              ) : (
                <>
                  <Copy className="size-3" /> Copy
                </>
              )}
            </button>
          )}
        </div>
      </div>
    </motion.div>
  );
}
