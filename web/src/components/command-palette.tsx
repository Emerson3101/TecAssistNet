"use client";

import { useEffect } from "react";
import { useRouter } from "next/navigation";
import { useTheme } from "next-themes";
import {
  FileText,
  LogOut,
  MessageSquare,
  Moon,
  Plus,
  Search,
  Sun,
} from "lucide-react";
import { createClient } from "@/lib/supabase/client";
import { useConversations } from "@/components/conversations/use-conversations";
import {
  CommandDialog,
  CommandEmpty,
  CommandGroup,
  CommandInput,
  CommandItem,
  CommandList,
  CommandSeparator,
} from "@/components/ui/command";

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

  function navigate(path: string) {
    onOpenChange(false);
    router.push(path);
  }

  async function handleSignOut() {
    onOpenChange(false);
    const supabase = createClient();
    await supabase.auth.signOut();
    router.push("/login");
    router.refresh();
  }

  return (
    <CommandDialog open={open} onOpenChange={onOpenChange}>
      <CommandInput placeholder="Type a command or search chats..." />
      <CommandList>
        <CommandEmpty>No results found.</CommandEmpty>
        <CommandGroup heading="Actions">
          <CommandItem onSelect={() => navigate("/chat")}>
            <Plus /> New chat
          </CommandItem>
          <CommandItem onSelect={() => navigate("/documents")}>
            <FileText /> Go to documents
          </CommandItem>
          <CommandItem
            onSelect={() => setTheme(resolvedTheme === "dark" ? "light" : "dark")}
          >
            {resolvedTheme === "dark" ? (
              <>
                <Sun /> Switch to light mode
              </>
            ) : (
              <>
                <Moon /> Switch to dark mode
              </>
            )}
          </CommandItem>
          <CommandItem className="text-destructive" onSelect={() => void handleSignOut()}>
            <LogOut /> Sign out
          </CommandItem>
        </CommandGroup>
        <CommandSeparator />
        <CommandGroup heading="Recent chats">
          {conversations?.slice(0, 8).map((conversation) => (
            <CommandItem
              key={conversation.id}
              value={`${conversation.title ?? "new conversation"} ${conversation.id}`}
              onSelect={() => navigate(`/chat/${conversation.id}`)}
            >
              <MessageSquare />
              <span className="truncate">
                {conversation.title ?? "New conversation"}
              </span>
            </CommandItem>
          ))}
          {conversations !== null && conversations.length === 0 && (
            <div className="flex items-center gap-2 px-2 py-3 text-sm text-muted-foreground">
              <Search className="size-4" />
              No conversations yet
            </div>
          )}
        </CommandGroup>
      </CommandList>
    </CommandDialog>
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
