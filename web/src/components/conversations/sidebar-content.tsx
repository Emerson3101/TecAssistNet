"use client";

import { useMemo, useState } from "react";
import Link from "next/link";
import { usePathname } from "next/navigation";
import { motion } from "motion/react";
import {
  FileText,
  PanelLeftClose,
  PanelLeftOpen,
  Plus,
  Search,
  Settings,
  Sparkles,
} from "lucide-react";
import { useConversations } from "@/components/conversations/use-conversations";
import { ConversationItem } from "@/components/conversations/conversation-item";
import { UserMenu } from "@/components/user-menu";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { ScrollArea } from "@/components/ui/scroll-area";
import { Separator } from "@/components/ui/separator";
import { Skeleton } from "@/components/ui/skeleton";
import {
  Tooltip,
  TooltipContent,
  TooltipProvider,
  TooltipTrigger,
} from "@/components/ui/tooltip";
import { cn } from "@/lib/utils";

export function SidebarContent({
  user,
  collapsed = false,
  onNavigate,
  onToggleCollapse,
}: {
  user: { email: string | null } | null;
  collapsed?: boolean;
  onNavigate?: () => void;
  onToggleCollapse?: () => void;
}) {
  const pathname = usePathname();
  const { conversations } = useConversations();
  const [filter, setFilter] = useState("");

  const documentsActive = pathname.startsWith("/documents");
  const settingsActive = pathname === "/settings";

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

  if (collapsed) {
    return (
      <TooltipProvider>
        <div className="flex h-full flex-col items-center gap-2.5 py-4">
          <Tooltip>
            <TooltipTrigger asChild>
              <Button
                variant="ghost"
                size="icon"
                className="size-9 rounded-xl text-muted-foreground hover:text-foreground"
                onClick={onToggleCollapse}
                aria-label="Expand sidebar"
              >
                <PanelLeftOpen className="size-4" />
              </Button>
            </TooltipTrigger>
            <TooltipContent side="right">Expand sidebar</TooltipContent>
          </Tooltip>
          <Separator className="w-8" />

          <Tooltip>
            <TooltipTrigger asChild>
              <Button
                asChild
                variant="ghost"
                size="icon"
                className="size-9 rounded-xl"
              >
                <Link href="/chat" onClick={onNavigate} aria-label="New chat">
                  <Plus className="size-4" strokeWidth={2.5} />
                </Link>
              </Button>
            </TooltipTrigger>
            <TooltipContent side="right">New chat</TooltipContent>
          </Tooltip>

          <Tooltip>
            <TooltipTrigger asChild>
              <Button
                asChild
                variant="ghost"
                size="icon"
                className={cn(
                  "size-9 rounded-xl",
                  documentsActive && "bg-jade/12 text-foreground"
                )}
              >
                <Link href="/documents" onClick={onNavigate} aria-label="Documents">
                  <FileText className="size-4" />
                </Link>
              </Button>
            </TooltipTrigger>
            <TooltipContent side="right">Documents</TooltipContent>
          </Tooltip>

          <Tooltip>
            <TooltipTrigger asChild>
              <Button
                asChild
                variant="ghost"
                size="icon"
                className={cn(
                  "size-9 rounded-xl",
                  settingsActive && "bg-jade/12 text-foreground"
                )}
              >
                <Link href="/settings" onClick={onNavigate} aria-label="Settings">
                  <Settings className="size-4" />
                </Link>
              </Button>
            </TooltipTrigger>
            <TooltipContent side="right">Settings</TooltipContent>
          </Tooltip>

          <div className="flex-1" />

          <UserMenu email={user?.email ?? null} compact />
        </div>
      </TooltipProvider>
    );
  }

  return (
    <div className="flex h-full flex-col gap-4 p-4">
      <div className="flex items-center justify-between">
        <Link
          href="/chat"
          onClick={onNavigate}
          className="group flex items-center gap-2.5 px-1 py-1"
        >
          <motion.div
            whileHover={{ rotate: 10, scale: 1.06 }}
            transition={{ type: "spring", stiffness: 320, damping: 16 }}
            className="flex size-8 items-center justify-center rounded-xl bg-gradient-to-br from-jade to-gold shadow-md shadow-jade/25"
          >
            <Sparkles className="size-4 text-black/85" />
          </motion.div>
          <span className="text-lg font-semibold tracking-tight">
            Tec<span className="text-gradient">Assist</span>
          </span>
        </Link>
        <Button
          variant="ghost"
          size="icon"
          className="size-8 rounded-lg text-muted-foreground hover:text-foreground"
          onClick={onToggleCollapse}
          aria-label="Collapse sidebar"
        >
          <PanelLeftClose className="size-4" />
        </Button>
      </div>

      <Button
        asChild
        className="h-10 rounded-xl bg-gradient-to-r from-jade to-gold font-semibold text-black/85 shadow-lg shadow-jade/20 transition-all hover:shadow-gold/35 hover:brightness-110 active:scale-[0.99]"
      >
        <Link href="/chat" onClick={onNavigate}>
          <Plus className="size-4" strokeWidth={2.5} />
          New chat
        </Link>
      </Button>

      <Link
        href="/documents"
        onClick={onNavigate}
        className={`relative flex items-center gap-2.5 rounded-xl px-3 py-2.5 text-sm transition-colors ${
          documentsActive
            ? "bg-jade/12 text-foreground"
            : "text-muted-foreground hover:bg-sidebar-accent hover:text-foreground"
        }`}
      >
        {documentsActive && (
          <motion.span
            layoutId="nav-indicator"
            className="absolute left-0 top-1/2 h-5 w-[3px] -translate-y-1/2 rounded-full bg-gradient-to-b from-jade to-gold"
          />
        )}
        <FileText className="size-4" />
        Documents
      </Link>

      <Link
        href="/settings"
        onClick={onNavigate}
        className={`relative flex items-center gap-2.5 rounded-xl px-3 py-2.5 text-sm transition-colors ${
          settingsActive
            ? "bg-jade/12 text-foreground"
            : "text-muted-foreground hover:bg-sidebar-accent hover:text-foreground"
        }`}
      >
        {settingsActive && (
          <motion.span
            layoutId="nav-indicator"
            className="absolute left-0 top-1/2 h-5 w-[3px] -translate-y-1/2 rounded-full bg-gradient-to-b from-jade to-gold"
          />
        )}
        <Settings className="size-4" />
        Settings
      </Link>

      <div className="relative">
        <Search className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
        <Input
          value={filter}
          onChange={(event) => setFilter(event.target.value)}
          placeholder="Search chats..."
          className="h-9 rounded-xl border-transparent bg-background/60 pl-9 text-sm focus-visible:ring-jade/25"
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
