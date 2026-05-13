import { apiRequest } from "./api";
import type { PagedResult } from "./sites";

export type SiteSubmissionListItem = {
  id: number;
  siteName: string;
  siteUrl: string;
  contact?: string;
  description?: string;
  reviewStatus: string;
  reviewNote?: string;
  createdAt: string;
};

export type ArticleListItem = {
  id: number;
  slug: string;
  title: string;
  summary?: string;
  status: string;
  publishedAt?: string;
};

export function fetchSubmissions(page = 1, pageSize = 20) {
  return apiRequest<PagedResult<SiteSubmissionListItem>>(`/api/v1/admin/submissions?page=${page}&pageSize=${pageSize}`);
}

export function approveSubmission(id: number, reviewNote = "已通过") {
  return apiRequest<void>(`/api/v1/admin/submissions/${id}/approve`, {
    method: "POST",
    body: JSON.stringify({ reviewNote })
  });
}

export function rejectSubmission(id: number, reviewNote = "不符合收录要求") {
  return apiRequest<void>(`/api/v1/admin/submissions/${id}/reject`, {
    method: "POST",
    body: JSON.stringify({ reviewNote })
  });
}

export function fetchArticles(page = 1, pageSize = 20) {
  return apiRequest<PagedResult<ArticleListItem>>(`/api/v1/admin/articles?page=${page}&pageSize=${pageSize}`);
}

export function createArticle(payload: {
  slug: string;
  title: string;
  summary?: string;
  contentMd: string;
  status?: string;
}) {
  return apiRequest<{ id: number }>("/api/v1/admin/articles", {
    method: "POST",
    body: JSON.stringify(payload)
  });
}

export function publishArticle(id: number) {
  return apiRequest<void>(`/api/v1/admin/articles/${id}/publish`, { method: "PATCH", body: "{}" });
}

export function archiveArticle(id: number) {
  return apiRequest<void>(`/api/v1/admin/articles/${id}/archive`, { method: "PATCH", body: "{}" });
}
