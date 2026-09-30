"use client";

import { createContext, useCallback, useContext, useEffect, useState } from "react";

const STORAGE_KEY = "tecasist.settings.animatedBackground";

interface SettingsContextValue {
  animatedBackground: boolean;
  setAnimatedBackground: (enabled: boolean) => void;
}

const SettingsContext = createContext<SettingsContextValue>({
  animatedBackground: true,
  setAnimatedBackground: () => {},
});

export function SettingsProvider({ children }: { children: React.ReactNode }) {
  const [animatedBackground, setAnimated] = useState(true);

  useEffect(() => {
    const stored = window.localStorage.getItem(STORAGE_KEY);
    if (stored !== null) {
      setAnimated(stored === "true");
    }
  }, []);

  const setAnimatedBackground = useCallback((enabled: boolean) => {
    setAnimated(enabled);
    window.localStorage.setItem(STORAGE_KEY, String(enabled));
  }, []);

  return (
    <SettingsContext.Provider value={{ animatedBackground, setAnimatedBackground }}>
      {children}
    </SettingsContext.Provider>
  );
}

export function useSettings() {
  return useContext(SettingsContext);
}
