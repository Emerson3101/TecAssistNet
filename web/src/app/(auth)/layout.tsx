import { GridFloor, OrbitalScene, ParticlesCanvas } from "@/components/scene";

export default function AuthLayout({ children }: { children: React.ReactNode }) {
  return (
    <div className="relative flex min-h-dvh items-center justify-center overflow-hidden px-4 py-10">
      <div className="app-glow" aria-hidden />
      <ParticlesCanvas className="absolute inset-0 size-full opacity-60" />
      <GridFloor />
      <div
        aria-hidden
        className="pointer-events-none absolute -right-32 top-1/2 hidden size-[34rem] -translate-y-1/2 lg:block"
      >
        <OrbitalScene className="size-full opacity-70" />
      </div>
      <div
        aria-hidden
        className="pointer-events-none absolute -left-40 -top-24 hidden size-80 opacity-40 md:block"
      >
        <OrbitalScene className="size-full" />
      </div>
      {children}
    </div>
  );
}
