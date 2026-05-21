import { apiRequest } from "./api";

export type RelaySiteListItem = {
  id: number;
  slug: string;
  name: string;
  baseUrl: string;
  websiteUrl?: string;
  description?: string;
  status: string;
  supportsRefund: boolean;
  supportsInvoice: boolean;
  hasDocs: boolean;
  autoTestEnabled: boolean;
  hasTestApiKey: boolean;
  testIntervalMinutes: number;
  lastAutoTestAt?: string;
  docsUrl?: string;
  inviteUrl?: string;
  recentReview?: string;
  createdAtUtc: string;
  updatedAtUtc?: string;
};

export type RelaySiteOffer = {
  id?: number;
  modelId?: number;
  modelSlug?: string;
  vendor?: string;
  officialModelId?: string;
  requestName?: string;
  apiType?: "openai" | "anthropic";
  displayName?: string;
  officialInputPriceUsd?: number;
  officialOutputPriceUsd?: number;
  siteInputPriceUsd?: number;
  siteOutputPriceUsd?: number;
  effectiveInputPriceUsd?: number;
  effectiveOutputPriceUsd?: number;
  rechargeRatio?: number;
  bonusRatio?: number;
  sourceType?: string;
  status?: string;
  autoTestEnabled?: boolean;
  hasTestApiKey?: boolean;
  testApiKey?: string;
  crawledAt?: string;
  reviewedAt?: string;
};

export type RelayPricingPreviewItem = {
  officialModelId: string;
  requestName: string;
  apiType: "openai" | "anthropic";
  displayName: string;
  billingType: "tokens" | "times";
  groupId: string;
  groupName: string;
  groupRate: number;
  modelRate: number;
  completionRatio: number;
  siteInputPriceUsd?: number;
  siteOutputPriceUsd?: number;
  sitePerCallPriceUsd?: number;
  effectiveInputPriceUsd?: number;
  effectiveOutputPriceUsd?: number;
  effectivePerCallPriceUsd?: number;
  rechargeRatio: number;
  bonusRatio: number;
  sourceType: string;
  status: string;
};

export type RelaySiteTestRecord = {
  id: number;
  modelSlug: string;
  modelName: string;
  testType: string;
  status: string;
  firstTokenMs?: number;
  fullResponseMs?: number;
  riskScore: number;
  riskLevel: string;
  errorMessage?: string;
  testedAt: string;
};

export type RelaySiteDetail = RelaySiteListItem & {
  offers: RelaySiteOffer[];
  recentTests: RelaySiteTestRecord[];
};

export type PagedResult<T> = {
  items: T[];
  page: number;
  pageSize: number;
  total: number;
};

export async function fetchSites(page = 1, pageSize = 20) {
  return apiRequest<PagedResult<RelaySiteListItem>>(`/api/v1/admin/sites?page=${page}&pageSize=${pageSize}`);
}

export async function createSite(payload: {
  slug?: string;
  name: string;
  baseUrl: string;
  websiteUrl?: string;
  description?: string;
  supportsRefund?: boolean;
  supportsInvoice?: boolean;
  hasDocs?: boolean;
  docsUrl?: string;
  inviteUrl?: string;
  recentReview?: string;
  autoTestEnabled?: boolean;
  testApiKey?: string;
  testIntervalMinutes?: number;
  rechargeRatio?: number;
  bonusRatio?: number;
  offers?: RelaySiteOffer[];
}) {
  return apiRequest<{ id: number }>("/api/v1/admin/sites", {
    method: "POST",
    body: JSON.stringify(payload)
  });
}

export async function fetchSite(id: number) {
  return apiRequest<RelaySiteDetail>(`/api/v1/admin/sites/${id}`);
}

export async function updateSite(id: number, payload: {
  slug?: string;
  name: string;
  baseUrl: string;
  websiteUrl?: string;
  description?: string;
  supportsRefund?: boolean;
  supportsInvoice?: boolean;
  hasDocs?: boolean;
  docsUrl?: string;
  inviteUrl?: string;
  recentReview?: string;
  autoTestEnabled?: boolean;
  testApiKey?: string;
  testIntervalMinutes?: number;
  rechargeRatio?: number;
  bonusRatio?: number;
  offers?: RelaySiteOffer[];
}) {
  return apiRequest<void>(`/api/v1/admin/sites/${id}`, {
    method: "PUT",
    body: JSON.stringify(payload)
  });
}

export async function updateSiteStatus(id: number, status: string) {
  return apiRequest<void>(`/api/v1/admin/sites/${id}/status`, {
    method: "PATCH",
    body: JSON.stringify({ status })
  });
}

export async function deleteSite(id: number) {
  return apiRequest<void>(`/api/v1/admin/sites/${id}`, { method: "DELETE" });
}

export async function previewSitePricing(payload: {
  baseUrl: string;
  providerType?: string;
  apiKey?: string;
  rechargeRatio?: number;
  bonusRatio?: number;
  rateBaseline?: number;
  groupId?: string;
}) {
  return apiRequest<RelayPricingPreviewItem[]>("/api/v1/admin/sites/pricing-preview", {
    method: "POST",
    body: JSON.stringify(payload)
  });
}
