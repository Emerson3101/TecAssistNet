"use client";

import { useEffect, useRef } from "react";
import { motion } from "motion/react";
import { ArrowUp, Square } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Textarea } from "@/components/ui/textarea";

const MAX_LENGTH = 8000;

export function Composer({
  value,
  onChange,
  onSend,
  onStop,
  streaming,
}: {
  value: string;
  onChange: (value: string) => void;
  onSend: (content: string) => void;
  onStop: () => void;
  streaming: boolean;
}) {
  const textareaRef = useRef<HTMLTextAreaElement>(null);

  useEffect(() => {
    const element = textareaRef.current;
    if (!element) {
      return;
    }
    element.style.height = "auto";
    element.style.height = `${Math.min(element.scrollHeight, 200)}px`;
  }, [value]);

  useEffect(() => {
    if (!streaming) {
      textareaRef.current?.focus();
    }
  }, [streaming]);

  function handleKeyDown(event: React.KeyboardEvent<HTMLTextAreaElement>) {
    if (event.key === "Enter" && !event.shiftKey) {
      event.preventDefault();
      if (value.trim() && !streaming) {
        onSend(value);
      }
    }
    if (event.key === "Escape" && streaming) {
      onStop();
    }
  }

  return (
    <motion.div
      initial={{ opacity: 0, y: 18 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ type: "spring", stiffness: 280, damping: 28 }}
      className="mx-auto w-full max-w-3xl px-4 pb-4 lg:px-6"
    >
      <div className="group/composer relative rounded-3xl">
        <div className="frame-glow" aria-hidden />
        <div className="glass relative rounded-3xl p-2 shadow-2xl shadow-black/20">
          <Textarea
            ref={textareaRef}
            value={value}
            onChange={(event) => onChange(event.target.value)}
            onKeyDown={handleKeyDown}
            placeholder="Ask anything about your documents..."
            rows={1}
            maxLength={MAX_LENGTH}
            disabled={streaming}
            className="max-h-[200px] min-h-[56px] resize-none border-0 bg-transparent px-4 py-3.5 text-[15px] leading-relaxed shadow-none placeholder:text-muted-foreground/70 focus-visible:border-0 focus-visible:ring-0 disabled:opacity-60"
          />

          <div className="flex items-center justify-between px-3 pb-1 pt-0.5">
            <p className="text-[11px] text-muted-foreground">
              <kbd className="rounded border border-border bg-muted px-1 py-0.5 text-[9px]">
                Enter
              </kbd>{" "}
              send ·{" "}
              <kbd className="rounded border border-border bg-muted px-1 py-0.5 text-[9px]">
                Shift+Enter
              </kbd>{" "}
              newline
            </p>

            {value.length > MAX_LENGTH * 0.85 && (
              <p className="text-[11px] text-muted-foreground">
                {value.length}/{MAX_LENGTH}
              </p>
            )}

            {streaming ? (
              <Button
                size="icon"
                onClick={onStop}
                className="size-9 rounded-full bg-destructive/90 text-white shadow-lg shadow-destructive/25 transition-all hover:bg-destructive active:scale-95"
                aria-label="Stop generating"
              >
                <Square className="size-3.5 fill-current" />
              </Button>
            ) : (
              <Button
                size="icon"
                disabled={!value.trim()}
                onClick={() => onSend(value)}
                className="size-9 rounded-full bg-gradient-to-r from-jade to-gold text-black/85 shadow-lg shadow-jade/30 transition-all hover:shadow-gold/40 hover:brightness-110 active:scale-95 disabled:opacity-35 disabled:shadow-none"
                aria-label="Send message"
              >
                <ArrowUp className="size-4" strokeWidth={2.5} />
              </Button>
            )}
          </div>
        </div>
      </div>

      <p className="mt-2 text-center text-[11px] text-muted-foreground/70">
        TecAssist answers from your indexed documents — always check the sources.
      </p>
    </motion.div>
  );
}
