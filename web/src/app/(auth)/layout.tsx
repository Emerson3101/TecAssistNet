import { Aurora, GridOverlay } from "@/components/aurora";

export default function AuthLayout({ children }: { children: React.ReactNode }) {
  return (
    <div className="relative flex min-h-dvh items-center justify-center px-4 py-10">
      <Aurora />
      <GridOverlay />
      {children}
    </div>
  );
}
