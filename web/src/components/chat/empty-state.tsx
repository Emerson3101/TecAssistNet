"use client";

import { motion } from "motion/react";
import {
  FileSearch,
  ListChecks,
  MessageSquareQuote,
  Wrench,
} from "lucide-react";
import { Skeleton } from "@/components/ui/skeleton";

const SUGGESTIONS = [
  {
    icon: FileSearch,
    title: "Search across docs",
    prompt: "Where do my documents discuss voltage tolerances?",
  },
  {
    icon: ListChecks,
    title: "Summarize",
    prompt: "Summarize the key points of my uploaded documents.",
  },
  {
    icon: Wrench,
    title: "Build a checklist",
    prompt: "Draft a maintenance checklist based on my technical manuals.",
  },
  {
    icon: MessageSquareQuote,
    title: "Compare sources",
    prompt: "Do any of my documents contradict each other about the Q3 results?",
  },
];

export function EmptyState({
  loading,
  onSuggest,
}: {
  loading: boolean;
  onSuggest: (prompt: string) => void;
}) {
  return (
    <div className="relative flex min-h-0 flex-1 items-center justify-center overflow-y-auto px-4">
      <div className="grid-overlay" aria-hidden />
      <div className="relative w-full max-w-2xl py-10 text-center">
        <motion.div
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ type: "spring", stiffness: 240, damping: 26 }}
        >
          <div className="mx-auto mb-6 flex size-16 items-center justify-center rounded-3xl bg-gradient-to-br from-violet-500 to-cyan-500 shadow-2xl shadow-violet-500/30">
            <MessageSquareQuote className="size-7 text-white" />
          </div>
          <h1 className="text-3xl font-semibold tracking-tight sm:text-4xl">
            Ask <span className="text-gradient">TecAssist</span> anything
          </h1>
          <p className="mx-auto mt-3 max-w-md text-sm text-muted-foreground">
            Your AI technical assistant, grounded in the documents you upload —
            every answer cites its sources.
          </p>
        </motion.div>

        {loading ? (
          <div className="mt-10 grid grid-cols-1 gap-3 sm:grid-cols-2">
            {Array.from({ length: 4 }).map((_, index) => (
              <Skeleton key={index} className="h-20 rounded-2xl" />
            ))}
          </div>
        ) : (
          <div className="mt-10 grid grid-cols-1 gap-3 sm:grid-cols-2">
            {SUGGESTIONS.map((suggestion, index) => (
              <motion.button
                key={suggestion.title}
                initial={{ opacity: 0, y: 16 }}
                animate={{ opacity: 1, y: 0 }}
                transition={{ delay: 0.15 + index * 0.07, type: "spring", stiffness: 260, damping: 26 }}
                whileHover={{ y: -3 }}
                whileTap={{ scale: 0.98 }}
                onClick={() => onSuggest(suggestion.prompt)}
                className="glass group flex items-start gap-3 rounded-2xl p-4 text-left transition-colors hover:border-primary/30"
              >
                <div className="flex size-9 shrink-0 items-center justify-center rounded-xl bg-primary/10 text-primary transition-colors group-hover:bg-primary/20">
                  <suggestion.icon className="size-4.5" />
                </div>
                <div className="min-w-0">
                  <p className="text-sm font-medium">{suggestion.title}</p>
                  <p className="mt-0.5 line-clamp-2 text-xs leading-relaxed text-muted-foreground">
                    {suggestion.prompt}
                  </p>
                </div>
              </motion.button>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}
