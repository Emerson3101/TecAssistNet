"use client";

import { GridFloor, ParticlesCanvas } from "@/components/scene";

export function ChatBackdrop({ subtle = false }: { subtle?: boolean }) {
  return (
    <div aria-hidden className="pointer-events-none absolute inset-0 overflow-hidden">
      <ParticlesCanvas
        className={`absolute inset-0 size-full transition-opacity duration-500 ${
          subtle ? "opacity-[0.16]" : "opacity-40"
        }`}
      />
      {!subtle && <GridFloor />}
      <div className="absolute inset-0 bg-[radial-gradient(85%_60%_at_50%_38%,transparent_55%,var(--background)_100%)] opacity-70" />
    </div>
  );
}
