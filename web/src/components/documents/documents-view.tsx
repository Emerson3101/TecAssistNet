"use client";

import { useCallback, useEffect, useState } from "react";
import { toast } from "sonner";
import { motion } from "motion/react";
import { FileUp, MessagesSquare, Plus, Sparkles, UploadCloud } from "lucide-react";
import { api } from "@/lib/api";
import type { DocumentResponse } from "@/lib/types";
import { UploadZone } from "@/components/documents/upload-zone";
import { DocumentCard } from "@/components/documents/document-card";
import { TextDocumentDialog } from "@/components/documents/text-document-dialog";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";

export function DocumentsView() {
  const [documents, setDocuments] = useState<DocumentResponse[] | null>(null);
  const [textDialogOpen, setTextDialogOpen] = useState(false);

  const refresh = useCallback(async () => {
    try {
      setDocuments(await api.listDocuments());
    } catch (error) {
      toast.error(
        error instanceof Error ? error.message : "Failed to load documents."
      );
    }
  }, []);

  useEffect(() => {
    void refresh();
  }, [refresh]);

  useEffect(() => {
    const busy =
      documents?.some(
        (document) => document.status === "pending" || document.status === "processing"
      ) ?? false;
    if (!busy) {
      return;
    }

    const timer = setTimeout(() => void refresh(), 3000);
    return () => clearTimeout(timer);
  }, [documents, refresh]);

  const busyCount =
    documents?.filter(
      (document) => document.status === "pending" || document.status === "processing"
    ).length ?? 0;

  const readyCount =
    documents?.filter((document) => document.status === "ready").length ?? 0;

  return (
    <div className="scrollbar-thin min-h-0 flex-1 overflow-y-auto">
      <div className="mx-auto w-full max-w-4xl px-4 py-8 lg:px-6">
        <motion.div
          initial={{ opacity: 0, y: 16 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ type: "spring", stiffness: 260, damping: 26 }}
          className="flex flex-wrap items-end justify-between gap-4"
        >
          <div>
            <h1 className="text-2xl font-semibold tracking-tight">Documents</h1>
            <p className="mt-1 text-sm text-muted-foreground">
              {readyCount} indexed ·{" "}
              {busyCount > 0 ? (
                <span className="text-primary">{busyCount} processing...</span>
              ) : (
                `${documents?.length ?? 0} total`
              )}
            </p>
          </div>
          <Button
            onClick={() => setTextDialogOpen(true)}
            className="rounded-xl bg-gradient-to-r from-jade to-gold font-semibold text-black/85 shadow-lg shadow-jade/25 transition-all hover:shadow-gold/40 hover:brightness-110"
          >
            <Plus className="size-4" />
            Paste text
          </Button>
        </motion.div>

        <motion.div
          initial={{ opacity: 0, y: 16 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ delay: 0.08, type: "spring", stiffness: 260, damping: 26 }}
          className="mt-6"
        >
          <UploadZone onUploaded={() => void refresh()} />
        </motion.div>

        {documents === null ? (
          <div className="mt-6 grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
            {Array.from({ length: 6 }).map((_, index) => (
              <Skeleton key={index} className="h-36 rounded-2xl" />
            ))}
          </div>
        ) : documents.length === 0 ? (
          <motion.div
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            transition={{ delay: 0.2 }}
            className="glass mt-6 rounded-3xl p-8 text-center"
          >
            <div className="mx-auto flex size-12 items-center justify-center rounded-2xl bg-primary/10 text-primary">
              <FileUp className="size-5" />
            </div>
            <h2 className="mt-4 font-medium">No documents yet</h2>
            <p className="mx-auto mt-1 max-w-sm text-sm text-muted-foreground">
              Drop a PDF, Markdown or text file above — it will be chunked, embedded
              and made searchable in seconds.
            </p>

            <div className="mx-auto mt-8 grid max-w-lg grid-cols-1 gap-3 text-left sm:grid-cols-3">
              {[
                { icon: UploadCloud, step: "1. Upload", detail: "PDF, Markdown or plain text" },
                { icon: Sparkles, step: "2. Ingest", detail: "Chunked + embedded automatically" },
                { icon: MessagesSquare, step: "3. Ask", detail: "Chat with cited answers" },
              ].map((item) => (
                <div key={item.step} className="rounded-2xl border border-border/60 p-3">
                  <item.icon className="size-4 text-primary" />
                  <p className="mt-2 text-xs font-medium">{item.step}</p>
                  <p className="text-[11px] leading-snug text-muted-foreground">
                    {item.detail}
                  </p>
                </div>
              ))}
            </div>
          </motion.div>
        ) : (
          <motion.div layout className="mt-6 grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
            {documents.map((document, index) => (
              <DocumentCard
                key={document.id}
                document={document}
                index={index}
                onChanged={() => void refresh()}
              />
            ))}
          </motion.div>
        )}
      </div>

      <TextDocumentDialog
        open={textDialogOpen}
        onOpenChange={setTextDialogOpen}
        onUploaded={() => void refresh()}
      />
    </div>
  );
}
