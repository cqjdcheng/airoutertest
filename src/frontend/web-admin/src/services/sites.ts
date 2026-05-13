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
  docsUrl?: string;
  inviteUrl?: string;
  recentReview?: string;
  createdAtUtc: string;
  updatedAtUtc?: string;
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
}) {
  return apiRequest<{ id: number }>("/api/v1/admin/sites", {
    method: "POST",
    body: JSON.stringify(payload)
  });
}

export async function fetchSite(id: number) {
  return apiRequest<RelaySiteListItem>(`/api/v1/admin/sites/${id}`);
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
