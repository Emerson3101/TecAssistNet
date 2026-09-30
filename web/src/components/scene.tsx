"use client";

import { useEffect, useRef } from "react";
import { useTheme } from "next-themes";

export function OrbitalScene({ className }: { className?: string }) {
  return (
    <div aria-hidden className={`orbital-scene ${className ?? ""}`}>
      <div className="orbital-ring ring-c" />
      <div className="orbital-ring ring-a">
        <div className="ring-dot" />
      </div>
      <div className="orbital-ring ring-b" />
      <div className="orbital-core" />
    </div>
  );
}

export function GridFloor() {
  return <div aria-hidden className="grid-floor" />;
}

const DARK_PALETTE = [
  { r: 154, g: 245, b: 196 },
  { r: 252, g: 214, b: 130 },
  { r: 137, g: 205, b: 255 },
];

const LIGHT_PALETTE = [
  { r: 22, g: 138, b: 100 },
  { r: 196, g: 140, b: 42 },
  { r: 58, g: 118, b: 192 },
];

export function ParticlesCanvas({ className }: { className?: string }) {
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const { resolvedTheme } = useTheme();

  useEffect(() => {
    const canvas = canvasRef.current;
    if (!canvas) {
      return;
    }

    const context = canvas.getContext("2d");
    if (!context) {
      return;
    }

    const palette = resolvedTheme === "light" ? LIGHT_PALETTE : DARK_PALETTE;
    const reducedMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;

    let width = 0;
    let height = 0;
    let frame = 0;

    interface Particle {
      x: number;
      y: number;
      vx: number;
      vy: number;
      radius: number;
      color: { r: number; g: number; b: number };
    }

    let particles: Particle[] = [];

    function resize() {
      if (!canvas) {
        return;
      }
      const rect = canvas.getBoundingClientRect();
      const dpr = Math.min(window.devicePixelRatio || 1, 2);
      width = rect.width;
      height = rect.height;
      canvas.width = Math.max(1, Math.round(rect.width * dpr));
      canvas.height = Math.max(1, Math.round(rect.height * dpr));
      context!.setTransform(dpr, 0, 0, dpr, 0, 0);
    }

    function seed() {
      const density = Math.min(72, Math.max(28, Math.round((width * height) / 26000)));
      particles = Array.from({ length: density }, () => {
        const drift = palette[Math.floor(Math.random() * palette.length)];
        return {
          x: Math.random() * width,
          y: Math.random() * height,
          vx: (Math.random() - 0.5) * 0.16,
          vy: (Math.random() - 0.5) * 0.16,
          radius: 0.7 + Math.random() * 1.7,
          color: drift,
        };
      });
    }

    function draw() {
      if (!context) {
        return;
      }
      context.clearRect(0, 0, width, height);

      for (const particle of particles) {
        for (const other of particles) {
          const dx = particle.x - other.x;
          const dy = particle.y - other.y;
          const distance = Math.hypot(dx, dy);
          if (distance < 120) {
            const alpha = (1 - distance / 120) * 0.14;
            context.strokeStyle = `rgba(${particle.color.r}, ${particle.color.g}, ${particle.color.b}, ${alpha})`;
            context.lineWidth = 0.7;
            context.beginPath();
            context.moveTo(particle.x, particle.y);
            context.lineTo(other.x, other.y);
            context.stroke();
          }
        }
      }

      for (const particle of particles) {
        context.fillStyle = `rgba(${particle.color.r}, ${particle.color.g}, ${particle.color.b}, 0.5)`;
        context.beginPath();
        context.arc(particle.x, particle.y, particle.radius, 0, Math.PI * 2);
        context.fill();
      }
    }

    function step() {
      if (!context) {
        return;
      }
      for (const particle of particles) {
        particle.x += particle.vx;
        particle.y += particle.vy;
        if (particle.x < -8) particle.x = width + 8;
        if (particle.x > width + 8) particle.x = -8;
        if (particle.y < -8) particle.y = height + 8;
        if (particle.y > height + 8) particle.y = -8;
      }
      draw();
      frame = requestAnimationFrame(step);
    }

    const observer = new ResizeObserver(() => {
      resize();
      seed();
      if (reducedMotion) {
        draw();
      }
    });
    observer.observe(canvas);

    resize();
    seed();

    if (reducedMotion) {
      draw();
    } else {
      frame = requestAnimationFrame(step);
    }

    return () => {
      cancelAnimationFrame(frame);
      observer.disconnect();
    };
  }, [resolvedTheme]);

  return <canvas ref={canvasRef} aria-hidden className={className} />;
}
