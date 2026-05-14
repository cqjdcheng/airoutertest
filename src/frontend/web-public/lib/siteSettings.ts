import { cache } from "react";
import { apiBaseUrl } from "@/lib/api";
import { defaultPublicSiteSettings, type PublicSiteSettings } from "@/lib/siteBranding";

export const getPublicSiteSettings = cache(async (): Promise<PublicSiteSettings> => {
  try {
    const response = await fetch(`${apiBaseUrl}/api/v1/public/site-settings`, {
      cache: "no-store"
    });

    if (!response.ok) {
      return defaultPublicSiteSettings;
    }

    const payload = (await response.json()) as {
      data?: PublicSiteSettings | null;
    };

    return payload.data?.siteName ? payload.data : defaultPublicSiteSettings;
  } catch {
    return defaultPublicSiteSettings;
  }
});
