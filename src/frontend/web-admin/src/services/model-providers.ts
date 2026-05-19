import { apiRequest } from "./api";

export type ModelProviderListItem = {
  id: number;
  slug: string;
  name: string;
  websiteUrl?: string;
  description?: string;
  status: string;
  sortOrder: number;
  createdAtUtc?: string;
  updatedAtUtc?: string;
};

export type PagedResult<T> = {
  items: T[];
  page: number;
  pageSize: number;
  total: number;
};

export async function fetchModelProviders(page = 1, pageSize = 100) {
  return apiRequest<PagedResult<ModelProviderListItem>>(`/api/v1/admin/model-providers?page=${page}&pageSize=${pageSize}`);
}

export async function fetchActiveModelProviders() {
  return apiRequest<ModelProviderListItem[]>("/api/v1/admin/model-providers/active");
}

export async function createModelProvider(payload: {
  slug?: string;
  name: string;
  websiteUrl?: string;
  description?: string;
  status?: string;
  sortOrder?: number;
}) {
  return apiRequest<{ id: number }>("/api/v1/admin/model-providers", {
    method: "POST",
    body: JSON.stringify(payload)
  });
}

export async function updateModelProvider(id: number, payload: {
  slug?: string;
  name: string;
  websiteUrl?: string;
  description?: string;
  status?: string;
  sortOrder?: number;
}) {
  return apiRequest<void>(`/api/v1/admin/model-providers/${id}`, {
    method: "PUT",
    body: JSON.stringify(payload)
  });
}

export async function updateModelProviderStatus(id: number, status: string) {
  return apiRequest<void>(`/api/v1/admin/model-providers/${id}/status`, {
    method: "PATCH",
    body: JSON.stringify({ status })
  });
}

export async function deleteModelProvider(id: number) {
  return apiRequest<void>(`/api/v1/admin/model-providers/${id}`, { method: "DELETE" });
}
