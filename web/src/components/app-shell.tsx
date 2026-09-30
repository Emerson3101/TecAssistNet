"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import { Menu, Sparkles } from "lucide-react";
import { SidebarContent } from "@/components/conversations/sidebar-content";
import { CommandPalette, useCommandPalette } from "@/components/command-palette";
import { Button } from "@/components/ui/button";
import { Sheet, SheetContent, SheetTitle, SheetTrigger } from "@/components/ui/sheet";
import { cn } from "@/lib/utils";

const DEFAULT_WIDTH = 288;
const MIN_WIDTH = 232;
const MAX_WIDTH = 424;
const COLLAPSED_WIDTH = 68;

const WIDTH_STORAGE_KEY = "tecasist.sidebar.width";
const COLLAPSED_STORAGE_KEY = "tecasist.sidebar.collapsed";

export function AppShell({
  user,
  children,
}: {
  user: { email: string | null } | null;
  children: React.ReactNode;
}) {
  const [mobileOpen, setMobileOpen] = useState(false);
  const [paletteOpen, setPaletteOpen] = useState(false);
  const [sidebarWidth, setSidebarWidth] = useState(DEFAULT_WIDTH);
  const [collapsed, setCollapsed] = useState(false);
  const [resizing, setResizing] = useState(false);
  const widthRef = useRef(sidebarWidth);

  useCommandPalette(() => setPaletteOpen((open) => !open));

  useEffect(() => {
    widthRef.current = sidebarWidth;
  }, [sidebarWidth]);

  useEffect(() => {
    const storedWidth = Number(window.localStorage.getItem(WIDTH_STORAGE_KEY));
    if (!Number.isNaN(storedWidth) && storedWidth >= MIN_WIDTH && storedWidth <= MAX_WIDTH) {
      setSidebarWidth(storedWidth);
    }
    setCollapsed(window.localStorage.getItem(COLLAPSED_STORAGE_KEY) === "true");
  }, []);

  useEffect(() => {
    window.localStorage.setItem(WIDTH_STORAGE_KEY, String(sidebarWidth));
  }, [sidebarWidth]);

  useEffect(() => {
    window.localStorage.setItem(COLLAPSED_STORAGE_KEY, String(collapsed));
  }, [collapsed]);

  const startResize = useCallback(
    (event: React.PointerEvent<HTMLDivElement>) => {
      event.preventDefault();
      const startX = event.clientX;
      const startWidth = collapsed ? COLLAPSED_WIDTH : widthRef.current;
      setCollapsed(false);
      setResizing(true);

      const handleMove = (moveEvent: PointerEvent) => {
        const next = Math.min(
          MAX_WIDTH,
          Math.max(MIN_WIDTH, startWidth + moveEvent.clientX - startX)
        );
        setSidebarWidth(next);
      };

      const handleUp = () => {
        setResizing(false);
        window.removeEventListener("pointermove", handleMove);
        window.removeEventListener("pointerup", handleUp);
      };

      window.addEventListener("pointermove", handleMove);
      window.addEventListener("pointerup", handleUp);
    },
    [collapsed]
  );

  const resetWidth = useCallback(() => {
    setCollapsed(false);
    setSidebarWidth(DEFAULT_WIDTH);
  }, []);

  return (
    <div
      className={cn(
        "relative flex h-dvh overflow-hidden",
        resizing && "cursor-col-resize select-none"
      )}
    >
      <div className="app-glow" aria-hidden />

      <aside
        style={{ width: collapsed ? COLLAPSED_WIDTH : sidebarWidth }}
        className={cn(
          "relative z-10 hidden shrink-0 flex-col border-r border-sidebar-border bg-sidebar lg:flex",
          resizing ? "transition-none" : "transition-[width] duration-300 ease-[cubic-bezier(0.32,0.72,0,1)]"
        )}
      >
        <SidebarContent
          user={user}
          collapsed={collapsed}
          onToggleCollapse={() => setCollapsed((isCollapsed) => !isCollapsed)}
        />

        <div
          onPointerDown={startResize}
          onDoubleClick={resetWidth}
          aria-hidden
          className="group/resizer absolute inset-y-0 -right-1.5 z-30 hidden w-3 cursor-col-resize lg:block"
        >
          <div className="mx-auto h-full w-px bg-gradient-to-b from-jade/0 via-jade/60 to-jade/0 opacity-0 transition-opacity duration-200 group-hover/resizer:opacity-100" />
        </div>
      </aside>

      <Sheet open={mobileOpen} onOpenChange={setMobileOpen}>
        <div className="flex h-dvh min-w-0 flex-1 flex-col">
          <header className="flex items-center gap-2 border-b border-border bg-background/80 px-3 py-2 backdrop-blur lg:hidden">
            <SheetTrigger asChild>
              <Button variant="ghost" size="icon" className="size-9 rounded-xl">
                <Menu className="size-5" />
              </Button>
            </SheetTrigger>
            <div className="flex items-center gap-2">
              <div className="flex size-6 items-center justify-center rounded-lg bg-gradient-to-br from-jade to-gold">
                <Sparkles className="size-3 text-black/85" />
              </div>
              <span className="font-semibold tracking-tight">
                Tec<span className="text-gradient">Assist</span>
              </span>
            </div>
          </header>
          <main className="relative flex min-h-0 flex-1 flex-col">{children}</main>
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
