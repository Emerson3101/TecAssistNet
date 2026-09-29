"use client";

import { useState } from "react";
import { Menu, Sparkles } from "lucide-react";
import { SidebarContent } from "@/components/conversations/sidebar-content";
import { CommandPalette, useCommandPalette } from "@/components/command-palette";
import { Button } from "@/components/ui/button";
import { Sheet, SheetContent, SheetTitle, SheetTrigger } from "@/components/ui/sheet";

export function AppShell({
  user,
  children,
}: {
  user: { email: string | null } | null;
  children: React.ReactNode;
}) {
  const [mobileOpen, setMobileOpen] = useState(false);
  const [paletteOpen, setPaletteOpen] = useState(false);
  useCommandPalette(() => setPaletteOpen((open) => !open));

  return (
    <div className="flex min-h-dvh">
      <aside className="hidden w-72 shrink-0 border-r border-sidebar-border bg-sidebar lg:block">
        <SidebarContent user={user} />
      </aside>

      <Sheet open={mobileOpen} onOpenChange={setMobileOpen}>
        <div className="flex min-w-0 flex-1 flex-col">
          <header className="flex items-center gap-2 border-b border-border bg-background/80 px-3 py-2 backdrop-blur lg:hidden">
            <SheetTrigger asChild>
              <Button variant="ghost" size="icon" className="size-9 rounded-xl">
                <Menu className="size-5" />
              </Button>
            </SheetTrigger>
            <div className="flex items-center gap-2">
              <div className="flex size-6 items-center justify-center rounded-lg bg-gradient-to-br from-violet-500 to-cyan-500">
                <Sparkles className="size-3 text-white" />
              </div>
              <span className="font-semibold tracking-tight">TecAssist</span>
            </div>
          </header>
          <main className="flex min-h-0 flex-1 flex-col">{children}</main>
        </div>

        <SheetContent side="left" className="w-80 gap-0 border-sidebar-border bg-sidebar p-0">
          <SheetTitle className="sr-only">Navigation</SheetTitle>
          <SidebarContent user={user} onNavigate={() => setMobileOpen(false)} />
        </SheetContent>
      </Sheet>

      <CommandPalette open={paletteOpen} onOpenChange={setPaletteOpen} />
    </div>
  );
}
