"use client";

import { useRef, useState } from "react";
import { toast } from "sonner";
import { AnimatePresence, motion } from "motion/react";
import { CloudUpload, FileUp, Loader2 } from "lucide-react";
import { api } from "@/lib/api";

const MAX_BYTES = 25 * 1024 * 1024;
const ALLOWED_EXTENSIONS = [".pdf", ".md", ".markdown", ".txt"];

export function UploadZone({ onUploaded }: { onUploaded: () => void }) {
  const inputRef = useRef<HTMLInputElement>(null);
  const [dragging, setDragging] = useState(false);
  const [uploading, setUploading] = useState(false);

  async function upload(file: File) {
    const extension = file.name.slice(file.name.lastIndexOf(".")).toLowerCase();
    if (!ALLOWED_EXTENSIONS.includes(extension)) {
      toast.error(`Unsupported file type "${extension}". Allowed: .pdf, .md, .txt`);
      return;
    }
    if (file.size > MAX_BYTES) {
      toast.error("That file exceeds the 25 MB limit.");
      return;
    }
    if (file.size === 0) {
      toast.error("That file is empty.");
      return;
    }

    setUploading(true);
    try {
      const document = await api.uploadDocumentFile(file);
      toast.success(`"${document.title}" accepted — ingestion started.`);
      onUploaded();
    } catch (error) {
      toast.error(
        error instanceof Error ? error.message : "The upload failed."
      );
    } finally {
      setUploading(false);
      if (inputRef.current) {
        inputRef.current.value = "";
      }
    }
  }

  return (
    <div
      onDragOver={(event) => {
        event.preventDefault();
        if (!uploading) {
          setDragging(true);
        }
      }}
      onDragLeave={() => setDragging(false)}
      onDrop={(event) => {
        event.preventDefault();
        setDragging(false);
        const file = event.dataTransfer.files?.[0];
        if (file && !uploading) {
          void upload(file);
        }
      }}
      onClick={() => !uploading && inputRef.current?.click()}
      role="button"
      tabIndex={0}
      onKeyDown={(event) => {
        if (event.key === "Enter" || event.key === " ") {
          event.preventDefault();
          inputRef.current?.click();
        }
      }}
      className={`relative cursor-pointer overflow-hidden rounded-3xl border-2 border-dashed p-8 text-center transition-all ${
        uploading
          ? "cursor-wait border-primary/40 bg-primary/5"
          : dragging
            ? "scale-[1.01] border-primary/60 bg-primary/10 shadow-lg shadow-violet-500/10"
            : "border-border hover:border-primary/35 hover:bg-primary/[0.03]"
      }`}
    >
      <input
        ref={inputRef}
        type="file"
        accept=".pdf,.md,.markdown,.txt"
        className="hidden"
        onChange={(event) => {
          const file = event.target.files?.[0];
          if (file) {
            void upload(file);
          }
        }}
      />

      <AnimatePresence mode="wait" initial={false}>
        {uploading ? (
          <motion.div
            key="uploading"
            initial={{ opacity: 0, y: 8 }}
            animate={{ opacity: 1, y: 0 }}
            exit={{ opacity: 0, y: -8 }}
            className="flex flex-col items-center gap-3"
          >
            <div className="flex size-12 items-center justify-center rounded-2xl bg-primary/15 text-primary">
              <Loader2 className="size-5 animate-spin" />
            </div>
            <p className="text-sm font-medium">Uploading & extracting text...</p>
            <p className="text-xs text-muted-foreground">
              Large PDFs can take a moment.
            </p>
          </motion.div>
        ) : (
          <motion.div
            key="idle"
            initial={{ opacity: 0, y: 8 }}
            animate={{ opacity: 1, y: 0 }}
            exit={{ opacity: 0, y: -8 }}
            className="flex flex-col items-center gap-3"
          >
            <motion.div
              animate={dragging ? { y: -4, scale: 1.1 } : { y: 0, scale: 1 }}
              transition={{ type: "spring", stiffness: 300, damping: 20 }}
              className="flex size-12 items-center justify-center rounded-2xl bg-gradient-to-br from-violet-500/15 to-cyan-500/15 text-primary"
            >
              {dragging ? (
                <FileUp className="size-5" />
              ) : (
                <CloudUpload className="size-5" />
              )}
            </motion.div>
            <p className="text-sm font-medium">
              {dragging ? "Drop it here" : "Drag a file here, or click to browse"}
            </p>
            <p className="text-xs text-muted-foreground">
              .pdf · .md · .txt — up to 25 MB
            </p>
          </motion.div>
        )}
      </AnimatePresence>
    </div>
  );
}
