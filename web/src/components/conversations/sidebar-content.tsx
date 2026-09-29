"use client";

import { useMemo, useState } from "react";
import Link from "next/link";
import { usePathname } from "next/navigation";
import { motion } from "motion/react";
import { FileText, Plus, Search, Sparkles } from "lucide-react";
import { useConversations } from "@/components/conversations/use-conversations";
import { ConversationItem } from "@/components/conversations/conversation-item";
import { UserMenu } from "@/components/user-menu";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { ScrollArea } from "@/components/ui/scroll-area";
import { Skeleton } from "@/components/ui/skeleton";

export function SidebarContent({
  user,
  onNavigate,
}: {
  user: { email: string | null } | null;
  onNavigate?: () => void;
}) {
  const pathname = usePathname();
  const { conversations } = useConversations();
  const [filter, setFilter] = useState("");

  const visible = useMemo(() => {
    if (!conversations) {
      return null;
    }
    const query = filter.trim().toLowerCase();
    if (!query) {
      return conversations;
    }
    return conversations.filter((conversation) =>
      (conversation.title ?? "new conversation").toLowerCase().includes(query)
    );
  }, [conversations, filter]);

  return (
    <div className="flex h-full flex-col gap-4 p-4">
      <Link
        href="/chat"
        onClick={onNavigate}
        className="flex items-center gap-2.5 px-1 py-1"
      >
        <div className="flex size-8 items-center justify-center rounded-xl bg-gradient-to-br from-violet-500 to-cyan-500 shadow-md shadow-violet-500/25">
          <Sparkles className="size-4 text-white" />
        </div>
        <span className="text-lg font-semibold tracking-tight">
          Tec<span className="text-gradient">Assist</span>
        </span>
      </Link>

      <Button
        asChild
        className="h-10 rounded-xl bg-gradient-to-r from-violet-500 to-cyan-500 font-medium text-white shadow-lg shadow-violet-500/20 transition-all hover:shadow-violet-500/35 hover:brightness-110"
      >
        <Link href="/chat" onClick={onNavigate}>
          <Plus className="size-4" />
          New chat
        </Link>
      </Button>

      <Link
        href="/documents"
        onClick={onNavigate}
        className={`flex items-center gap-2.5 rounded-xl px-3 py-2.5 text-sm transition-colors ${
          pathname.startsWith("/documents")
            ? "bg-primary/15 text-foreground"
            : "text-muted-foreground hover:bg-sidebar-accent hover:text-foreground"
        }`}
      >
        <FileText className="size-4" />
        Documents
      </Link>

      <div className="relative">
        <Search className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
        <Input
          value={filter}
          onChange={(event) => setFilter(event.target.value)}
          placeholder="Search chats..."
          className="h-9 rounded-xl bg-background/60 pl-9 text-sm"
        />
      </div>

      <ScrollArea className="-mr-2 min-h-0 flex-1 pr-2">
        <div className="flex flex-col gap-1">
          {visible === null &&
            Array.from({ length: 5 }).map((_, index) => (
              <Skeleton key={index} className="h-12 rounded-xl" />
            ))}

          {visible !== null && visible.length === 0 && (
            <p className="px-3 py-6 text-center text-xs text-muted-foreground">
              {filter ? "No chats match your search." : "No conversations yet — start one!"}
            </p>
          )}

          <motion.div layout className="flex flex-col gap-1">
            {visible?.map((conversation) => (
              <ConversationItem
                key={conversation.id}
                conversation={conversation}
                onNavigate={onNavigate}
              />
            ))}
          </motion.div>
        </div>
      </ScrollArea>

      <UserMenu email={user?.email ?? null} />
    </div>
  );
}
