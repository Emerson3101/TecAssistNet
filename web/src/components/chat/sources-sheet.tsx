"use client";

import { motion } from "motion/react";
import { BookOpen } from "lucide-react";
import type { CitationResponse } from "@/lib/types";
import { ScrollArea } from "@/components/ui/scroll-area";
import { Sheet, SheetContent, SheetHeader, SheetTitle } from "@/components/ui/sheet";

export function SourcesSheet({
  open,
  onOpenChange,
  citations,
}: {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  citations: CitationResponse[];
}) {
  return (
    <Sheet open={open} onOpenChange={onOpenChange}>
      <SheetContent side="right" className="w-full gap-0 p-0 sm:max-w-md">
        <SheetHeader className="border-b border-border p-4">
          <SheetTitle className="flex items-center gap-2 text-base">
            <BookOpen className="size-4 text-primary" />
            Sources
          </SheetTitle>
        </SheetHeader>

        <ScrollArea className="h-[calc(100dvh-4rem)]">
          <div className="flex flex-col gap-4 p-4">
            {citations.map((citation, index) => (
              <motion.div
                key={citation.chunkId}
                initial={{ opacity: 0, x: 16 }}
                animate={{ opacity: 1, x: 0 }}
                transition={{ delay: index * 0.05 }}
                className="glass rounded-2xl p-4"
              >
                <div className="flex items-center justify-between gap-2">
                  <p className="truncate text-sm font-medium">
                    [{index + 1}] {citation.documentTitle}
                  </p>
                </div>

                {citation.score > 0 && (
                  <>
                    <div className="mt-2 h-1.5 overflow-hidden rounded-full bg-muted">
                      <motion.div
                        initial={{ width: 0 }}
                        animate={{ width: `${Math.max(2, Math.round(citation.score * 100))}%` }}
                        transition={{ delay: 0.15 + index * 0.05, type: "spring", stiffness: 90, damping: 20 }}
                        className="h-full rounded-full bg-gradient-to-r from-jade to-gold"
                      />
                    </div>
                    <p className="mt-1 text-right text-[11px] text-muted-foreground">
                      similarity {(citation.score * 100).toFixed(0)}%
                    </p>
                  </>
                )}

                <p className="mt-2 text-sm leading-relaxed text-muted-foreground">
                  {citation.snippet}
                </p>
              </motion.div>
            ))}
          </div>
        </ScrollArea>
      </SheetContent>
    </Sheet>
  );
}
