import { apiRequest } from "./api";

export type ModelListItem = {
  id: number;
  slug: string;
  vendor: string;
  officialModelId: string;
  displayName: string;
  status: string;
  officialInputPriceUsd?: number;
  officialOutputPriceUsd?: number;
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
  slug?: string;
  vendor: string;
  officialModelId: string;
  displayName: string;
  description?: string;
  status?: string;
  officialInputPriceUsd?: number;
  officialOutputPriceUsd?: number;
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
  slug?: string;
  vendor: string;
  officialModelId: string;
  displayName: string;
  description?: string;
  status?: string;
  officialInputPriceUsd?: number;
  officialOutputPriceUsd?: number;
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
