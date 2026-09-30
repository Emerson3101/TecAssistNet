"use client";

import { motion } from "motion/react";
import {
  FileSearch,
  ListChecks,
  MessageSquareQuote,
  Wrench,
} from "lucide-react";
import { OrbitalScene, ParticlesCanvas, GridFloor } from "@/components/scene";
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
    <div className="relative flex min-h-0 flex-1 items-center justify-center overflow-y-auto overflow-x-hidden px-4">
      <ParticlesCanvas className="pointer-events-none absolute inset-0 size-full opacity-40" />
      <GridFloor />

      <div className="relative w-full max-w-2xl py-10 text-center">
        <motion.div
          initial={{ opacity: 0, scale: 0.8 }}
          animate={{ opacity: 1, scale: 1 }}
          transition={{ type: "spring", stiffness: 200, damping: 22 }}
          className="relative mx-auto mb-8 size-44"
        >
          <OrbitalScene className="size-full" />
        </motion.div>

        <motion.div
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ delay: 0.15, type: "spring", stiffness: 240, damping: 26 }}
        >
          <h1 className="bg-gradient-to-b from-foreground to-muted-foreground bg-clip-text text-4xl font-semibold tracking-tight text-transparent sm:text-5xl">
            Ask <span className="text-gradient">TecAssist</span> anything
          </h1>
          <p className="mx-auto mt-4 max-w-md text-sm text-muted-foreground">
            Your AI technical assistant, grounded in the documents you upload —
            every answer cites its sources.
          </p>
        </motion.div>

        {loading ? (
          <div className="mt-12 grid grid-cols-1 gap-3 sm:grid-cols-2">
            {Array.from({ length: 4 }).map((_, index) => (
              <Skeleton key={index} className="h-20 rounded-2xl" />
            ))}
          </div>
        ) : (
          <div className="mt-12 grid grid-cols-1 gap-3 sm:grid-cols-2">
            {SUGGESTIONS.map((suggestion, index) => (
              <motion.button
                key={suggestion.title}
                initial={{ opacity: 0, y: 18, rotateX: -8 }}
                animate={{ opacity: 1, y: 0, rotateX: 0 }}
                transition={{
                  delay: 0.28 + index * 0.08,
                  type: "spring",
                  stiffness: 240,
                  damping: 22,
                }}
                whileHover={{ y: -4, scale: 1.02 }}
                whileTap={{ scale: 0.98 }}
                style={{ transformPerspective: 600 }}
                onClick={() => onSuggest(suggestion.prompt)}
                className="glass group flex items-start gap-3 rounded-2xl p-4 text-left transition-colors hover:border-jade/35 hover:shadow-lg hover:shadow-jade/10"
              >
                <div className="flex size-9 shrink-0 items-center justify-center rounded-xl bg-gradient-to-br from-jade/20 to-gold/20 text-primary transition-transform duration-300 group-hover:scale-110">
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
