"use client";

import { motion } from "motion/react";

export function TypingIndicator() {
  return (
    <div className="flex items-center gap-1.5 py-1" aria-label="Assistant is thinking">
      {[0, 1, 2].map((index) => (
        <motion.span
          key={index}
          className="size-2 rounded-full bg-primary/70"
          animate={{ y: [0, -4, 0], opacity: [0.4, 1, 0.4] }}
          transition={{
            duration: 0.9,
            repeat: Number.POSITIVE_INFINITY,
            delay: index * 0.15,
            ease: "easeInOut",
          }}
        />
      ))}
      <span className="ml-2 text-xs text-muted-foreground">Thinking...</span>
    </div>
  );
}
