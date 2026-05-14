export type PublicSiteSettings = {
  siteName: string;
  siteIconUrl?: string | null;
};

export const defaultPublicSiteSettings: PublicSiteSettings = {
  siteName: "CheapAI",
  siteIconUrl: null
};
