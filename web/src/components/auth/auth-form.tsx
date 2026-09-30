"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { toast } from "sonner";
import { AnimatePresence, motion } from "motion/react";
import { Eye, EyeOff, Loader2, LogIn, Mail, Sparkles, UserPlus } from "lucide-react";
import { createClient } from "@/lib/supabase/client";
import { OrbitalScene } from "@/components/scene";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";

const TAGLINES = [
  "Grounded answers, always cited.",
  "Your documents, distilled into conversation.",
  "Streaming responses, live and verifiable.",
  "Retrieval you can trust — sources on demand.",
];

export function AuthForm({ mode }: { mode: "login" | "signup" }) {
  const router = useRouter();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [showPassword, setShowPassword] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [needsConfirmation, setNeedsConfirmation] = useState(false);
  const [taglineIndex, setTaglineIndex] = useState(0);

  const isLogin = mode === "login";

  useEffect(() => {
    const timer = setInterval(() => {
      setTaglineIndex((index) => (index + 1) % TAGLINES.length);
    }, 3400);
    return () => clearInterval(timer);
  }, []);

  async function handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setSubmitting(true);
    setNeedsConfirmation(false);

    const supabase = createClient();
    try {
      if (isLogin) {
        const { error } = await supabase.auth.signInWithPassword({ email, password });
        if (error) {
          toast.error(error.message);
          return;
        }
        router.push("/chat");
        router.refresh();
      } else {
        const { data, error } = await supabase.auth.signUp({ email, password });
        if (error) {
          toast.error(error.message);
          return;
        }
        if (data.session) {
          router.push("/chat");
          router.refresh();
        } else {
          setNeedsConfirmation(true);
        }
      }
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Something went wrong.");
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <motion.div
      initial={{ opacity: 0, y: 28, scale: 0.97 }}
      animate={{ opacity: 1, y: 0, scale: 1 }}
      transition={{ type: "spring", stiffness: 240, damping: 26 }}
      className="relative z-10 w-full max-w-4xl"
    >
      <div className="glass grid overflow-hidden rounded-3xl shadow-2xl shadow-black/30 md:grid-cols-[1.05fr_1fr]">
        <div className="relative hidden flex-col justify-between overflow-hidden bg-gradient-to-br from-[oklch(0.22_0.045_205)] via-[oklch(0.2_0.05_195)] to-[oklch(0.24_0.06_175)] p-10 md:flex">
          <div
            aria-hidden
            className="absolute -right-20 -top-24 size-72 opacity-80 float-soft"
          >
            <OrbitalScene className="size-full" />
          </div>
          <div
            aria-hidden
            className="absolute -bottom-24 -left-16 size-56 opacity-40"
          >
            <OrbitalScene className="size-full" />
          </div>

          <div className="relative">
            <div className="flex size-11 items-center justify-center rounded-2xl bg-gradient-to-br from-jade to-gold shadow-lg shadow-jade/30">
              <Sparkles className="size-5 text-black/85" />
            </div>
            <h2 className="mt-6 text-3xl font-semibold leading-tight tracking-tight">
              Your documents,
              <br />
              <span className="text-gradient">finally conversational.</span>
            </h2>
          </div>

          <div className="relative min-h-20">
            <AnimatePresence mode="wait">
              <motion.p
                key={taglineIndex}
                initial={{ opacity: 0, y: 14, filter: "blur(6px)" }}
                animate={{ opacity: 1, y: 0, filter: "blur(0px)" }}
                exit={{ opacity: 0, y: -14, filter: "blur(6px)" }}
                transition={{ duration: 0.45, ease: "easeOut" }}
                className="text-sm text-white/75"
              >
                {TAGLINES[taglineIndex]}
              </motion.p>
            </AnimatePresence>
          </div>

          <div className="relative flex items-center gap-2 text-xs text-white/45">
            <span className="inline-block size-1.5 animate-pulse rounded-full bg-jade" />
            Streaming RAG · Supabase Auth · pgvector
          </div>
        </div>

        <div className="relative p-8 sm:p-10">
          <motion.div
            initial={{ opacity: 0, y: 12 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ delay: 0.08 }}
            className="mb-8 flex flex-col items-center gap-3 text-center md:items-start md:text-left"
          >
            <div className="flex size-12 items-center justify-center rounded-2xl bg-gradient-to-br from-jade to-gold shadow-lg shadow-jade/25 md:hidden">
              <Sparkles className="size-6 text-black/85" />
            </div>
            <div>
              <h1 className="text-2xl font-semibold tracking-tight">
                {isLogin ? "Welcome back" : "Create your account"}
              </h1>
              <p className="mt-1 text-sm text-muted-foreground">
                {isLogin
                  ? "Sign in to chat with your documents."
                  : "Start grounding your answers in minutes."}
              </p>
            </div>
          </motion.div>

          <form onSubmit={handleSubmit} className="flex flex-col gap-5">
            <motion.div
              initial={{ opacity: 0, y: 10 }}
              animate={{ opacity: 1, y: 0 }}
              transition={{ delay: 0.14 }}
              className="flex flex-col gap-2"
            >
              <Label htmlFor="email">Email</Label>
              <div className="relative">
                <Mail className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
                <Input
                  id="email"
                  type="email"
                  required
                  autoComplete="email"
                  placeholder="you@example.com"
                  value={email}
                  onChange={(event) => setEmail(event.target.value)}
                  className="h-11 rounded-xl border-transparent bg-secondary/60 pl-10 transition-all focus-visible:border-jade/50 focus-visible:ring-jade/25"
                />
              </div>
            </motion.div>

            <motion.div
              initial={{ opacity: 0, y: 10 }}
              animate={{ opacity: 1, y: 0 }}
              transition={{ delay: 0.2 }}
              className="flex flex-col gap-2"
            >
              <Label htmlFor="password">Password</Label>
              <div className="relative">
                <Input
                  id="password"
                  type={showPassword ? "text" : "password"}
                  required
                  minLength={6}
                  autoComplete={isLogin ? "current-password" : "new-password"}
                  placeholder="••••••••"
                  value={password}
                  onChange={(event) => setPassword(event.target.value)}
                  className="h-11 rounded-xl border-transparent bg-secondary/60 pr-10 transition-all focus-visible:border-jade/50 focus-visible:ring-jade/25"
                />
                <button
                  type="button"
                  onClick={() => setShowPassword((visible) => !visible)}
                  className="absolute right-3 top-1/2 -translate-y-1/2 text-muted-foreground transition-colors hover:text-foreground"
                  aria-label={showPassword ? "Hide password" : "Show password"}
                >
                  {showPassword ? (
                    <EyeOff className="size-4" />
                  ) : (
                    <Eye className="size-4" />
                  )}
                </button>
              </div>
            </motion.div>

            <motion.div
              initial={{ opacity: 0, y: 10 }}
              animate={{ opacity: 1, y: 0 }}
              transition={{ delay: 0.26 }}
            >
              <Button
                type="submit"
                disabled={submitting}
                className="relative h-11 w-full overflow-hidden rounded-xl bg-gradient-to-r from-jade to-gold text-base font-semibold text-black/85 shadow-lg shadow-jade/25 transition-all hover:shadow-lg hover:shadow-gold/30 hover:brightness-110 active:scale-[0.99] disabled:opacity-60"
              >
                {submitting ? (
                  <Loader2 className="size-4 animate-spin" />
                ) : isLogin ? (
                  <LogIn className="size-4" />
                ) : (
                  <UserPlus className="size-4" />
                )}
                {isLogin ? "Sign in" : "Create account"}
              </Button>
            </motion.div>
          </form>

          <AnimatePresence>
            {needsConfirmation && (
              <motion.p
                initial={{ opacity: 0, height: 0 }}
                animate={{ opacity: 1, height: "auto" }}
                exit={{ opacity: 0, height: 0 }}
                className="mt-4 rounded-xl border border-jade/30 bg-jade/10 px-4 py-3 text-center text-sm"
              >
                Account created — check <span className="font-medium">{email}</span> to
                confirm your email, then sign in.
              </motion.p>
            )}
          </AnimatePresence>

          <motion.p
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            transition={{ delay: 0.32 }}
            className="mt-6 text-center text-sm text-muted-foreground md:text-left"
          >
            {isLogin ? "No account yet?" : "Already have an account?"}{" "}
            <Link
              href={isLogin ? "/signup" : "/login"}
              className="font-medium text-primary underline-offset-4 hover:underline"
            >
              {isLogin ? "Create one" : "Sign in"}
            </Link>
          </motion.p>
        </div>
      </div>
    </motion.div>
  );
}
