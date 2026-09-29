"use client";

import { useState } from "react";
import Link from "next/link";
import { usePathname } from "next/navigation";
import { formatRelativeTime } from "@/lib/format";
import { motion } from "motion/react";
import {
  Check,
  Loader2,
  MessageSquare,
  MoreHorizontal,
  Pencil,
  Trash2,
} from "lucide-react";
import { api } from "@/lib/api";
import { notifyConversationsChanged } from "@/components/conversations/use-conversations";
import type { ConversationResponse } from "@/lib/types";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
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
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";

export function ConversationItem({
  conversation,
  onNavigate,
}: {
  conversation: ConversationResponse;
  onNavigate?: () => void;
}) {
  const pathname = usePathname();
  const isActive = pathname === `/chat/${conversation.id}`;

  const [renameOpen, setRenameOpen] = useState(false);
  const [deleteOpen, setDeleteOpen] = useState(false);
  const [title, setTitle] = useState(conversation.title ?? "");
  const [renaming, setRenaming] = useState(false);
  const [deleting, setDeleting] = useState(false);

  async function handleRename() {
    setRenaming(true);
    try {
      await api.renameConversation(conversation.id, title.trim());
      notifyConversationsChanged();
      setRenameOpen(false);
    } catch (error) {
      setTitle(conversation.title ?? "");
      throw error;
    } finally {
      setRenaming(false);
    }
  }

  async function handleDelete() {
    setDeleting(true);
    try {
      await api.deleteConversation(conversation.id);
      notifyConversationsChanged();
      setDeleteOpen(false);
      if (isActive) {
        onNavigate?.();
        window.location.href = "/chat";
      }
    } finally {
      setDeleting(false);
    }
  }

  return (
    <>
      <motion.div
        layout
        initial={{ opacity: 0, y: 6 }}
        animate={{ opacity: 1, y: 0 }}
        exit={{ opacity: 0, height: 0, marginBottom: 0 }}
        transition={{ type: "spring", stiffness: 320, damping: 30 }}
        className="group relative"
      >
        <Link
          href={`/chat/${conversation.id}`}
          onClick={onNavigate}
          className={`flex items-center gap-2.5 rounded-xl px-3 py-2.5 text-sm transition-colors ${
            isActive
              ? "bg-primary/15 text-foreground"
              : "text-muted-foreground hover:bg-sidebar-accent hover:text-foreground"
          }`}
        >
          <MessageSquare
            className={`size-4 shrink-0 ${isActive ? "text-primary" : ""}`}
          />
          <div className="min-w-0 flex-1">
            <p className="truncate leading-5">
              {conversation.title ?? "New conversation"}
            </p>
            <p className="text-xs leading-4 text-muted-foreground/70">
              {formatRelativeTime(conversation.lastMessageAt ?? conversation.createdAt)} ·{" "}
              {conversation.messageCount} message{conversation.messageCount === 1 ? "" : "s"}
            </p>
          </div>
        </Link>
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Button
              variant="ghost"
              size="icon"
              className="absolute right-1.5 top-1/2 size-7 -translate-y-1/2 opacity-0 transition-opacity group-hover:opacity-100 data-[state=open]:opacity-100"
              aria-label="Conversation actions"
            >
              <MoreHorizontal className="size-4" />
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end" side="right">
            <DropdownMenuItem onSelect={() => setRenameOpen(true)}>
              <Pencil /> Rename
            </DropdownMenuItem>
            <DropdownMenuItem
              variant="destructive"
              onSelect={() => setDeleteOpen(true)}
            >
              <Trash2 /> Delete
            </DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>
      </motion.div>

      <Dialog open={renameOpen} onOpenChange={setRenameOpen}>
        <DialogContent className="rounded-2xl">
          <DialogHeader>
            <DialogTitle>Rename conversation</DialogTitle>
          </DialogHeader>
          <Input
            value={title}
            onChange={(event) => setTitle(event.target.value)}
            maxLength={120}
            className="h-10"
            onKeyDown={(event) => {
              if (event.key === "Enter" && title.trim()) {
                void handleRename();
              }
            }}
          />
          <DialogFooter>
            <Button variant="ghost" onClick={() => setRenameOpen(false)}>
              Cancel
            </Button>
            <Button
              onClick={() => void handleRename()}
              disabled={!title.trim() || renaming}
            >
              {renaming ? <Loader2 className="size-4 animate-spin" /> : <Check />}
              Save
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <AlertDialog open={deleteOpen} onOpenChange={setDeleteOpen}>
        <AlertDialogContent className="rounded-2xl">
          <AlertDialogHeader>
            <AlertDialogTitle>Delete this conversation?</AlertDialogTitle>
            <AlertDialogDescription>
              &ldquo;{conversation.title ?? "New conversation"}&rdquo; and its message
              history will be permanently removed.
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
