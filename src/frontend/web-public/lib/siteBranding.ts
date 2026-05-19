export type PublicSiteSettings = {
  siteName: string;
  siteIconUrl?: string | null;
  faviconUrl?: string | null;
};

export const defaultPublicSiteSettings: PublicSiteSettings = {
  siteName: "CheapAI",
  siteIconUrl: null,
  faviconUrl: null
};
