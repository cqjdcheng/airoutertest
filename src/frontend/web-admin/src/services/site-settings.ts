import { apiRequest } from "./api";

export type SiteSettings = {
  siteName: string;
  siteIconUrl?: string;
};

export async function fetchSiteSettings() {
  return apiRequest<SiteSettings>("/api/v1/admin/site-settings");
}

export async function updateSiteSettings(payload: SiteSettings) {
  return apiRequest<void>("/api/v1/admin/site-settings", {
    method: "PUT",
    body: JSON.stringify(payload)
  });
}
