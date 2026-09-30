"use client";

import { motion } from "motion/react";

export function TypingIndicator() {
  return (
    <div className="flex items-center gap-2.5 py-1" aria-label="Assistant is thinking">
      <span className="relative flex size-4 items-center justify-center">
        <span className="absolute inset-0 animate-ping rounded-full bg-jade/25" />
        <span className="size-1.5 rounded-full bg-jade" />
      </span>
      {[0, 1, 2].map((index) => (
        <motion.span
          key={index}
          className="size-1.5 rounded-full bg-gradient-to-r from-jade to-gold"
          animate={{ y: [0, -5, 0], opacity: [0.35, 1, 0.35] }}
          transition={{
            duration: 0.85,
            repeat: Number.POSITIVE_INFINITY,
            delay: index * 0.14,
            ease: "easeInOut",
          }}
        />
      ))}
      <motion.span
        className="ml-1.5 text-xs text-muted-foreground"
        animate={{ opacity: [0.45, 0.9, 0.45] }}
        transition={{ duration: 1.6, repeat: Number.POSITIVE_INFINITY }}
      >
        Thinking...
      </motion.span>
    </div>
  );
}
