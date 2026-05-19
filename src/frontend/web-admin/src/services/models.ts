import { apiRequest } from "./api";

export type ModelListItem = {
  id: number;
  providerId?: number;
  providerName?: string;
  slug: string;
  vendor: string;
  officialModelId: string;
  requestName: string;
  apiType: "openai" | "anthropic";
  displayName: string;
  status: string;
  isHot: boolean;
  sortOrder: number;
  officialInputPriceUsd?: number;
  officialOutputPriceUsd?: number;
  capabilityScore?: number;
  capabilitySource?: string;
  capabilityUpdatedAtUtc?: string;
  description?: string;
  createdAtUtc?: string;
  updatedAtUtc?: string;
};

export type PagedResult<T> = {
  items: T[];
  page: number;
  pageSize: number;
  total: number;
};

export async function fetchModels(page = 1, pageSize = 20) {
  return apiRequest<PagedResult<ModelListItem>>(`/api/v1/admin/models?page=${page}&pageSize=${pageSize}`);
}

export async function createModel(payload: {
  providerId?: number;
  slug?: string;
  vendor?: string;
  officialModelId: string;
  requestName: string;
  apiType?: "openai" | "anthropic";
  displayName: string;
  description?: string;
  status?: string;
  isHot?: boolean;
  sortOrder?: number;
  officialInputPriceUsd?: number;
  officialOutputPriceUsd?: number;
  capabilityScore?: number;
  capabilitySource?: string;
}) {
  return apiRequest<{ id: number }>("/api/v1/admin/models", {
    method: "POST",
    body: JSON.stringify(payload)
  });
}

export async function fetchModel(id: number) {
  return apiRequest<ModelListItem>(`/api/v1/admin/models/${id}`);
}

export async function updateModel(id: number, payload: {
  providerId?: number;
  slug?: string;
  vendor?: string;
  officialModelId: string;
  requestName: string;
  apiType?: "openai" | "anthropic";
  displayName: string;
  description?: string;
  status?: string;
  isHot?: boolean;
  sortOrder?: number;
  officialInputPriceUsd?: number;
  officialOutputPriceUsd?: number;
  capabilityScore?: number;
  capabilitySource?: string;
}) {
  return apiRequest<void>(`/api/v1/admin/models/${id}`, {
    method: "PUT",
    body: JSON.stringify(payload)
  });
}

export async function updateModelStatus(id: number, status: string) {
  return apiRequest<void>(`/api/v1/admin/models/${id}/status`, {
    method: "PATCH",
    body: JSON.stringify({ status })
  });
}

export async function deleteModel(id: number) {
  return apiRequest<void>(`/api/v1/admin/models/${id}`, { method: "DELETE" });
}

export async function updateModelMetadata(id: number, payload: {
  isHot: boolean;
  sortOrder: number;
}) {
  return apiRequest<void>(`/api/v1/admin/models/${id}/metadata`, {
    method: "PATCH",
    body: JSON.stringify(payload)
  });
}

export type ModelImportPreviewItem = {
  providerSlug: string;
  providerName: string;
  vendor: string;
  officialModelId: string;
  requestName: string;
  apiType: "openai" | "anthropic";
  displayName: string;
  description?: string;
  officialInputPriceUsd?: number;
  officialOutputPriceUsd?: number;
  capabilityScore?: number;
  capabilitySource?: string;
};

export async function previewModelImport(payload: {
  baseUrl: string;
  apiKey?: string;
  vendor: string;
}) {
  return apiRequest<ModelImportPreviewItem[]>("/api/v1/admin/models/import-preview", {
    method: "POST",
    body: JSON.stringify(payload)
  });
}

export async function previewOpenRouterModels() {
  return apiRequest<ModelImportPreviewItem[]>("/api/v1/admin/models/openrouter-preview", {
    method: "POST",
    body: "{}"
  });
}

export async function importModels(payload: {
  models: Array<{
    providerId?: number;
    providerSlug?: string;
    providerName?: string;
    slug?: string;
    vendor?: string;
    officialModelId: string;
    requestName: string;
    apiType?: "openai" | "anthropic";
    displayName: string;
    description?: string;
    status?: string;
    isHot?: boolean;
    sortOrder?: number;
    officialInputPriceUsd?: number;
    officialOutputPriceUsd?: number;
    capabilityScore?: number;
    capabilitySource?: string;
  }>;
}) {
  return apiRequest<{ createdCount: number; updatedCount: number }>("/api/v1/admin/models/import", {
    method: "POST",
    body: JSON.stringify(payload)
  });
}
