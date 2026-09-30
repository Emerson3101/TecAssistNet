"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { useTheme } from "next-themes";
import { toast } from "sonner";
import { Loader2, LogOut, Moon, Sun } from "lucide-react";
import { createClient } from "@/lib/supabase/client";
import { Avatar, AvatarFallback } from "@/components/ui/avatar";
import { Button } from "@/components/ui/button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";

export function UserMenu({
  email,
  compact = false,
}: {
  email: string | null;
  compact?: boolean;
}) {
  const router = useRouter();
  const { theme, setTheme } = useTheme();
  const [signingOut, setSigningOut] = useState(false);
  const [mounted, setMounted] = useState(false);

  useEffect(() => setMounted(true), []);

  async function handleSignOut() {
    setSigningOut(true);
    try {
      const supabase = createClient();
      await supabase.auth.signOut();
      router.push("/login");
      router.refresh();
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Sign out failed.");
    } finally {
      setSigningOut(false);
    }
  }

  const initial = (email?.[0] ?? "?").toUpperCase();

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button
          variant="ghost"
          className={
            compact
              ? "size-9 rounded-xl p-0"
              : "h-auto w-full justify-start gap-3 rounded-xl px-2 py-2"
          }
          aria-label={compact ? "Account menu" : undefined}
        >
          <Avatar className="size-8">
            <AvatarFallback className="bg-gradient-to-br from-jade to-gold text-xs font-semibold text-black/85">
              {initial}
            </AvatarFallback>
          </Avatar>
          {!compact && (
            <div className="min-w-0 flex-1 text-left">
              <p className="truncate text-sm leading-4">Signed in</p>
              <p className="truncate text-xs leading-4 text-muted-foreground">
                {email ?? "user"}
              </p>
            </div>
          )}
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align={compact ? "center" : "start"} className="w-60 rounded-xl">
        <DropdownMenuLabel className="truncate text-xs text-muted-foreground">
          {email ?? "user"}
        </DropdownMenuLabel>
        <DropdownMenuSeparator />
        <DropdownMenuItem
          onClick={() => mounted && setTheme(theme === "dark" ? "light" : "dark")}
        >
          {mounted && theme === "dark" ? (
            <>
              <Sun /> Light mode
            </>
          ) : (
            <>
              <Moon /> Dark mode
            </>
          )}
        </DropdownMenuItem>
        <DropdownMenuItem
          variant="destructive"
          onClick={() => void handleSignOut()}
          disabled={signingOut}
        >
          {signingOut ? (
            <>
              <Loader2 className="animate-spin" /> Signing out...
            </>
          ) : (
            <>
              <LogOut /> Sign out
            </>
          )}
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
