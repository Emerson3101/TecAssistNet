"use client";

import { BookOpen, Quote } from "lucide-react";
import type { CitationResponse } from "@/lib/types";
import { Badge } from "@/components/ui/badge";

export function CitationChips({
  citations,
  onOpen,
}: {
  citations: CitationResponse[];
  onOpen: () => void;
}) {
  if (citations.length === 0) {
    return null;
  }

  return (
    <div className="flex flex-wrap items-center gap-1.5">
      <button
        onClick={onOpen}
        className="inline-flex items-center gap-1.5 rounded-full border bg-background/50 px-2.5 py-1 text-xs text-muted-foreground transition-colors hover:border-primary/40 hover:text-foreground"
      >
        <BookOpen className="size-3" />
        {citations.length} source{citations.length === 1 ? "" : "s"}
      </button>

      {citations.slice(0, 3).map((citation, index) => (
        <Badge
          key={citation.chunkId}
          variant="outline"
          className="max-w-56 cursor-pointer truncate rounded-full border-primary/25 bg-primary/5 font-normal text-foreground hover:border-primary/50"
          onClick={onOpen}
        >
          <Quote className="size-2.5 shrink-0 text-primary" />
          <span className="truncate">{citation.documentTitle}</span>
          <span className="shrink-0 text-[10px] text-muted-foreground">
            {index + 1}
          </span>
        </Badge>
      ))}

      {citations.length > 3 && (
        <button
          onClick={onOpen}
          className="text-xs text-muted-foreground underline-offset-2 hover:underline"
        >
          +{citations.length - 3} more
        </button>
      )}
    </div>
  );
}
