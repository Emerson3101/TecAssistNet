"use client";

import { useEffect, useMemo, useRef, useState } from "react";
import { useRouter } from "next/navigation";
import { useTheme } from "next-themes";
import {
  FileText,
  LogOut,
  MessageSquare,
  Moon,
  Plus,
  Search,
  Settings,
  Sun,
  type LucideIcon,
} from "lucide-react";
import { createClient } from "@/lib/supabase/client";
import { useConversations } from "@/components/conversations/use-conversations";
import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { cn } from "@/lib/utils";

interface PaletteItem {
  id: string;
  icon: LucideIcon;
  label: string;
  group: "Actions" | "Recent chats";
  keywords: string;
  run: () => void | Promise<void>;
  destructive?: boolean;
}

export function CommandPalette({
  open,
  onOpenChange,
}: {
  open: boolean;
  onOpenChange: (open: boolean) => void;
}) {
  const router = useRouter();
  const { resolvedTheme, setTheme } = useTheme();
  const { conversations } = useConversations();
  const [query, setQuery] = useState("");
  const [activeIndex, setActiveIndex] = useState(0);
  const listRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (open) {
      setQuery("");
      setActiveIndex(0);
    }
  }, [open]);

  const items = useMemo<PaletteItem[]>(() => {
    function navigate(path: string) {
      onOpenChange(false);
      router.push(path);
    }

    const actions: PaletteItem[] = [
      {
        id: "action-new-chat",
        icon: Plus,
        label: "New chat",
        group: "Actions",
        keywords: "new chat start",
        run: () => navigate("/chat"),
      },
      {
        id: "action-documents",
        icon: FileText,
        label: "Go to documents",
        group: "Actions",
        keywords: "documents upload files",
        run: () => navigate("/documents"),
      },
      {
        id: "action-settings",
        icon: Settings,
        label: "Go to settings",
        group: "Actions",
        keywords: "settings preferences background theme options",
        run: () => navigate("/settings"),
      },
      {
        id: "action-theme",
        icon: resolvedTheme === "dark" ? Sun : Moon,
        label: `Switch to ${resolvedTheme === "dark" ? "light" : "dark"} mode`,
        group: "Actions",
        keywords: "theme dark light toggle",
        run: () => {
          onOpenChange(false);
          setTheme(resolvedTheme === "dark" ? "light" : "dark");
        },
      },
      {
        id: "action-sign-out",
        icon: LogOut,
        label: "Sign out",
        group: "Actions",
        keywords: "sign out logout exit",
        destructive: true,
        run: async () => {
          onOpenChange(false);
          const supabase = createClient();
          await supabase.auth.signOut();
          router.push("/login");
          router.refresh();
        },
      },
    ];

    const chats: PaletteItem[] = (conversations ?? []).slice(0, 8).map(
      (conversation) => ({
        id: conversation.id,
        icon: MessageSquare,
        label: conversation.title ?? "New conversation",
        group: "Recent chats",
        keywords: conversation.id,
        run: () => navigate(`/chat/${conversation.id}`),
      })
    );

    return [...actions, ...chats];
  }, [conversations, onOpenChange, resolvedTheme, router, setTheme]);

  const filtered = useMemo(() => {
    const needle = query.trim().toLowerCase();
    if (!needle) {
      return items;
    }
    return items.filter(
      (item) =>
        item.label.toLowerCase().includes(needle) ||
        item.keywords.includes(needle)
    );
  }, [items, query]);

  useEffect(() => {
    setActiveIndex(0);
  }, [query]);

  useEffect(() => {
    const active = listRef.current?.querySelector<HTMLElement>("[data-active='true']");
    active?.scrollIntoView({ block: "nearest" });
  }, [activeIndex]);

  function handleKeyDown(event: React.KeyboardEvent<HTMLInputElement>) {
    if (event.key === "ArrowDown") {
      event.preventDefault();
      setActiveIndex((index) => (filtered.length ? (index + 1) % filtered.length : 0));
    } else if (event.key === "ArrowUp") {
      event.preventDefault();
      setActiveIndex((index) =>
        filtered.length ? (index - 1 + filtered.length) % filtered.length : 0
      );
    } else if (event.key === "Enter") {
      event.preventDefault();
      const item = filtered[activeIndex];
      if (item) {
        void item.run();
      }
    }
  }

  const groups: PaletteItem["group"][] = ["Actions", "Recent chats"];

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent
        className="top-[20%] translate-y-0 gap-0 overflow-hidden rounded-2xl p-0"
        showCloseButton={false}
      >
        <DialogHeader className="sr-only">
          <DialogTitle>Command palette</DialogTitle>
        </DialogHeader>

        <div className="flex items-center gap-2.5 border-b border-border px-4 py-3">
          <Search className="size-4 shrink-0 text-muted-foreground" />
          <input
            autoFocus
            value={query}
            onChange={(event) => setQuery(event.target.value)}
            onKeyDown={handleKeyDown}
            placeholder="Type a command or search chats..."
            className="w-full bg-transparent text-sm outline-none placeholder:text-muted-foreground"
          />
        </div>

        <div ref={listRef} className="max-h-80 overflow-y-auto p-2">
          {filtered.length === 0 && (
            <p className="py-6 text-center text-sm text-muted-foreground">
              No results found.
            </p>
          )}

          {groups.map((group) => {
            const groupItems = filtered.filter((item) => item.group === group);
            if (groupItems.length === 0) {
              return null;
            }
            return (
              <div key={group} className="mb-1 last:mb-0">
                <p className="px-2.5 py-1.5 text-[11px] font-medium uppercase tracking-wide text-muted-foreground">
                  {group}
                </p>
                {groupItems.map((item) => {
                  const itemIndex = filtered.indexOf(item);
                  const active = itemIndex === activeIndex;
                  return (
                    <button
                      key={item.id}
                      data-active={active}
                      onClick={() => void item.run()}
                      onMouseEnter={() => setActiveIndex(itemIndex)}
                      className={cn(
                        "flex w-full items-center gap-2.5 rounded-lg px-2.5 py-2 text-left text-sm transition-colors",
                        active
                          ? item.destructive
                            ? "bg-destructive/15 text-destructive"
                            : "bg-muted text-foreground"
                          : item.destructive
                            ? "text-destructive/90"
                            : "text-foreground"
                      )}
                    >
                      <item.icon
                        className={cn(
                          "size-4 shrink-0",
                          item.destructive && !active
                            ? "text-destructive/80"
                            : "text-muted-foreground"
                        )}
                      />
                      <span className="truncate">{item.label}</span>
                    </button>
                  );
                })}
              </div>
            );
          })}
        </div>

        <div className="flex items-center justify-between border-t border-border px-4 py-2 text-[11px] text-muted-foreground">
          <span>
            <kbd className="rounded border border-border bg-muted px-1 py-0.5 text-[9px]">
              ↑↓
            </kbd>{" "}
            navigate ·{" "}
            <kbd className="rounded border border-border bg-muted px-1 py-0.5 text-[9px]">
              ↵
            </kbd>{" "}
            select
          </span>
          <span>
            <kbd className="rounded border border-border bg-muted px-1 py-0.5 text-[9px]">
              Esc
            </kbd>{" "}
            close
          </span>
        </div>
      </DialogContent>
    </Dialog>
  );
}

export function useCommandPalette(onTrigger: () => void) {
  useEffect(() => {
    function handler(event: KeyboardEvent) {
      if ((event.metaKey || event.ctrlKey) && event.key.toLowerCase() === "k") {
        event.preventDefault();
        onTrigger();
      }
    }

    window.addEventListener("keydown", handler);
    return () => window.removeEventListener("keydown", handler);
  }, [onTrigger]);
}
