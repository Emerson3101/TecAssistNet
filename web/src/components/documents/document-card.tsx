"use client";

import { useState } from "react";
import { toast } from "sonner";
import { motion } from "motion/react";
import {
  CheckCircle2,
  File,
  FileCode,
  FileText,
  Loader2,
  MoreHorizontal,
  Trash2,
  XCircle,
} from "lucide-react";
import { api } from "@/lib/api";
import { formatRelativeTime } from "@/lib/format";
import type { DocumentResponse } from "@/lib/types";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";

function SourceIcon({ sourceType }: { sourceType: DocumentResponse["sourceType"] }) {
  if (sourceType === "pdf") {
    return <File className="size-5 text-ember" />;
  }
  if (sourceType === "markdown") {
    return <FileCode className="size-5 text-gold" />;
  }
  return <FileText className="size-5 text-jade" />;
}

function StatusPill({ status }: { status: DocumentResponse["status"] }) {
  if (status === "pending" || status === "processing") {
    return (
      <Badge
        variant="outline"
        className="gap-1.5 rounded-full border-amber-500/30 bg-amber-500/10 text-amber-500"
      >
        <Loader2 className="size-3 animate-spin" />
        {status === "pending" ? "Queued" : "Ingesting"}
      </Badge>
    );
  }
  if (status === "ready") {
    return (
      <Badge
        variant="outline"
        className="gap-1.5 rounded-full border-emerald-500/30 bg-emerald-500/10 text-emerald-500"
      >
        <CheckCircle2 className="size-3" />
        Indexed
      </Badge>
    );
  }
  return (
    <Badge
      variant="outline"
      className="gap-1.5 rounded-full border-red-500/30 bg-red-500/10 text-red-400"
    >
      <XCircle className="size-3" />
      Failed
    </Badge>
  );
}

export function DocumentCard({
  document,
  index,
  onChanged,
}: {
  document: DocumentResponse;
  index: number;
  onChanged: () => void;
}) {
  const [deleteOpen, setDeleteOpen] = useState(false);
  const [deleting, setDeleting] = useState(false);

  async function handleDelete() {
    setDeleting(true);
    try {
      await api.deleteDocument(document.id);
      toast.success(`"${document.title}" deleted.`);
      setDeleteOpen(false);
      onChanged();
    } catch (error) {
      toast.error(
        error instanceof Error ? error.message : "Could not delete the document."
      );
    } finally {
      setDeleting(false);
    }
  }

  return (
    <>
      <motion.div
        layout
        initial={{ opacity: 0, y: 16 }}
        animate={{ opacity: 1, y: 0 }}
        exit={{ opacity: 0, scale: 0.95 }}
        transition={{ delay: index * 0.04, type: "spring", stiffness: 280, damping: 28 }}
        whileHover={{ y: -3, rotateX: 3 }}
        style={{ transformPerspective: 700 }}
        className={`glass group relative flex flex-col gap-3 rounded-2xl p-4 transition-colors hover:border-jade/25 ${
          document.status === "processing" || document.status === "pending"
            ? "shimmer"
            : ""
        }`}
      >
        <div className="flex items-start justify-between gap-2">
          <div className="flex size-10 shrink-0 items-center justify-center rounded-xl bg-secondary">
            <SourceIcon sourceType={document.sourceType} />
          </div>
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button
                variant="ghost"
                size="icon"
                className="size-7 shrink-0 opacity-0 transition-opacity group-hover:opacity-100 data-[state=open]:opacity-100"
                aria-label="Document actions"
              >
                <MoreHorizontal className="size-4" />
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
              <DropdownMenuItem variant="destructive" onSelect={() => setDeleteOpen(true)}>
                <Trash2 /> Delete
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
        </div>

        <div className="min-w-0">
          <p className="truncate font-medium leading-5" title={document.title}>
            {document.title}
          </p>
          <p className="mt-0.5 text-xs text-muted-foreground">
            {formatRelativeTime(document.createdAt)} ·{" "}
            {document.chunkCount > 0
              ? `${document.chunkCount} chunk${document.chunkCount === 1 ? "" : "s"}`
              : "not indexed"}
          </p>
        </div>

        <div className="flex items-center justify-between">
          <StatusPill status={document.status} />
          <span className="text-[10px] uppercase tracking-wide text-muted-foreground/70">
            {document.sourceType}
          </span>
        </div>
      </motion.div>

      <AlertDialog open={deleteOpen} onOpenChange={setDeleteOpen}>
        <AlertDialogContent className="rounded-2xl">
          <AlertDialogHeader>
            <AlertDialogTitle>Delete this document?</AlertDialogTitle>
            <AlertDialogDescription>
              &ldquo;{document.title}&rdquo; and all of its indexed chunks will be
              permanently removed.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancel</AlertDialogCancel>
            <AlertDialogAction
              className="bg-destructive text-white hover:bg-destructive/90"
              disabled={deleting}
              onClick={(event) => {
                event.preventDefault();
                void handleDelete();
              }}
            >
              {deleting ? (
                <Loader2 className="size-4 animate-spin" />
              ) : (
                <Trash2 className="size-4" />
              )}
              Delete
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </>
  );
}
