"use client";

import { createContext, useContext, type ReactNode } from "react";
import { defaultPublicSiteSettings, type PublicSiteSettings } from "@/lib/siteBranding";

const PublicBrandContext = createContext<PublicSiteSettings>(defaultPublicSiteSettings);

export function PublicBrandProvider({
  initialBranding,
  children
}: Readonly<{
  initialBranding: PublicSiteSettings;
  children: ReactNode;
}>) {
  return <PublicBrandContext.Provider value={initialBranding}>{children}</PublicBrandContext.Provider>;
}

export function usePublicBranding() {
  return useContext(PublicBrandContext);
}
