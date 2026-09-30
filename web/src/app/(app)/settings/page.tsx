"use client";

import { useEffect, useState } from "react";
import { motion } from "motion/react";
import { useTheme } from "next-themes";
import { useSettings } from "@/components/settings-provider";
import { cn } from "@/lib/utils";

interface SegmentedOption<T extends string> {
  value: T;
  label: string;
}

function Segmented<T extends string>({
  groupId,
  value,
  options,
  onChange,
}: {
  groupId: string;
  value: T;
  options: SegmentedOption<T>[];
  onChange: (value: T) => void;
}) {
  return (
    <div className="flex rounded-xl border border-border/60 bg-secondary/40 p-1">
      {options.map((option) => {
        const active = option.value === value;
        return (
          <button
            key={option.value}
            type="button"
            onClick={() => onChange(option.value)}
            className="relative rounded-lg px-4 py-1.5 text-sm outline-none"
            aria-pressed={active}
          >
            {active && (
              <motion.span
                layoutId={`${groupId}-indicator`}
                className="absolute inset-0 rounded-lg bg-gradient-to-r from-jade to-gold"
                transition={{ type: "spring", stiffness: 320, damping: 28 }}
              />
            )}
            <span
              className={cn(
                "relative z-10 transition-colors",
                active ? "font-semibold text-black/85" : "text-muted-foreground"
              )}
            >
              {option.label}
            </span>
          </button>
        );
      })}
    </div>
  );
}

export default function SettingsPage() {
  const { animatedBackground, setAnimatedBackground } = useSettings();
  const { theme, setTheme } = useTheme();
  const [mounted, setMounted] = useState(false);

  useEffect(() => setMounted(true), []);

  return (
    <div className="scrollbar-thin min-h-0 flex-1 overflow-y-auto">
      <div className="mx-auto w-full max-w-2xl px-4 py-8 lg:px-6">
        <motion.div
          initial={{ opacity: 0, y: 16 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ type: "spring", stiffness: 260, damping: 26 }}
        >
          <h1 className="text-2xl font-semibold tracking-tight">Settings</h1>
          <p className="mt-1 text-sm text-muted-foreground">
            Preferences are stored in this browser only.
          </p>
        </motion.div>

        <motion.div
          initial={{ opacity: 0, y: 16 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ delay: 0.08, type: "spring", stiffness: 260, damping: 26 }}
          className="glass mt-6 rounded-3xl p-6"
        >
          <h2 className="text-sm font-medium uppercase tracking-wide text-muted-foreground">
            Appearance
          </h2>

          <div className="mt-5 flex flex-wrap items-center justify-between gap-4">
            <div className="min-w-0">
              <p className="text-sm font-medium">Animated background</p>
              <p className="mt-0.5 max-w-md text-xs leading-relaxed text-muted-foreground">
                Keep the drifting particle field and orbital art behind chats and
                pages. Turn it off for a calm, solid backdrop (also easier on the
                battery).
              </p>
            </div>
            <Segmented
              groupId="background"
              value={animatedBackground ? "animated" : "solid"}
              options={[
                { value: "animated", label: "Animated" },
                { value: "solid", label: "Solid color" },
              ]}
              onChange={(next) => setAnimatedBackground(next === "animated")}
            />
          </div>

          <div className="mt-6 flex flex-wrap items-center justify-between gap-4 border-t border-border/60 pt-6">
            <div className="min-w-0">
              <p className="text-sm font-medium">Theme</p>
              <p className="mt-0.5 max-w-md text-xs leading-relaxed text-muted-foreground">
                Light, dark, or follow your system preference.
              </p>
            </div>
            {mounted ? (
              <Segmented
                groupId="theme"
                value={(theme === "system" ? "system" : theme === "light" ? "light" : "dark") as "system" | "light" | "dark"}
                options={[
                  { value: "light", label: "Light" },
                  { value: "dark", label: "Dark" },
                  { value: "system", label: "System" },
                ]}
                onChange={(next) => setTheme(next)}
              />
            ) : (
              <div className="h-10 w-64 animate-pulse rounded-xl bg-muted" />
            )}
          </div>
        </motion.div>
      </div>
    </div>
  );
}
