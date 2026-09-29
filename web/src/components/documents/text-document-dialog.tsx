"use client";

import { useState } from "react";
import { toast } from "sonner";
import { Loader2, Plus } from "lucide-react";
import { api } from "@/lib/api";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";

export function TextDocumentDialog({
  open,
  onOpenChange,
  onUploaded,
}: {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onUploaded: () => void;
}) {
  const [title, setTitle] = useState("");
  const [content, setContent] = useState("");
  const [sourceType, setSourceType] = useState<"text" | "markdown">("text");
  const [submitting, setSubmitting] = useState(false);

  function reset() {
    setTitle("");
    setContent("");
    setSourceType("text");
    setSubmitting(false);
  }

  async function handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!title.trim() || !content.trim()) {
      return;
    }
    setSubmitting(true);
    try {
      const document = await api.uploadDocumentText({
        title: title.trim(),
        content,
        sourceType,
      });
      toast.success(`"${document.title}" accepted — ingestion started.`);
      reset();
      onOpenChange(false);
      onUploaded();
    } catch (error) {
      toast.error(
        error instanceof Error ? error.message : "Could not create the document."
      );
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <Dialog
      open={open}
      onOpenChange={(next) => {
        if (!submitting) {
          onOpenChange(next);
          if (!next) {
            reset();
          }
        }
      }}
    >
      <DialogContent className="max-h-[85dvh] overflow-y-auto rounded-2xl sm:max-w-lg">
        <DialogHeader>
          <DialogTitle>Add a text document</DialogTitle>
          <DialogDescription>
            Paste notes, specs or reports — they will be indexed just like an
            uploaded file.
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit} className="flex flex-col gap-4">
          <div className="flex flex-col gap-2">
            <Label htmlFor="document-title">Title</Label>
            <Input
              id="document-title"
              value={title}
              onChange={(event) => setTitle(event.target.value)}
              placeholder="e.g. Q3 compliance notes"
              maxLength={200}
              required
              className="h-10 rounded-xl"
            />
          </div>

          <div className="flex flex-col gap-2">
            <Label>Format</Label>
            <div className="grid grid-cols-2 gap-2">
              {(["text", "markdown"] as const).map((type) => (
                <button
                  key={type}
                  type="button"
                  onClick={() => setSourceType(type)}
                  className={`rounded-xl border px-3 py-2 text-sm transition-colors ${
                    sourceType === type
                      ? "border-primary/60 bg-primary/10 font-medium text-foreground"
                      : "border-border text-muted-foreground hover:border-primary/30 hover:text-foreground"
                  }`}
                >
                  {type === "text" ? "Plain text" : "Markdown"}
                </button>
              ))}
            </div>
          </div>

          <div className="flex flex-col gap-2">
            <Label htmlFor="document-content">Content</Label>
            <Textarea
              id="document-content"
              value={content}
              onChange={(event) => setContent(event.target.value)}
              placeholder="Paste your document content here..."
              required
              className="min-h-[180px] resize-y rounded-xl"
            />
          </div>

          <DialogFooter>
            <Button
              type="button"
              variant="ghost"
              onClick={() => onOpenChange(false)}
              disabled={submitting}
            >
              Cancel
            </Button>
            <Button
              type="submit"
              disabled={!title.trim() || !content.trim() || submitting}
              className="bg-gradient-to-r from-violet-500 to-cyan-500 text-white shadow-lg shadow-violet-500/25 hover:brightness-110"
            >
              {submitting ? (
                <Loader2 className="size-4 animate-spin" />
              ) : (
                <Plus className="size-4" />
              )}
              Add document
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
