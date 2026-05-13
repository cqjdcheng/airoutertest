import { apiRequest } from "./api";
import type { PagedResult } from "./sites";

export type RelayOfferListItem = {
  id: number;
  siteName: string;
  siteSlug: string;
  modelName: string;
  modelSlug: string;
  officialInputPriceUsd?: number;
  officialOutputPriceUsd?: number;
  siteInputPriceUsd?: number;
  siteOutputPriceUsd?: number;
  effectiveInputPriceUsd?: number;
  effectiveOutputPriceUsd?: number;
  status: string;
  crawledAt?: string;
  reviewedAt?: string;
};

export type TestRecordListItem = {
  id: number;
  siteName: string;
  modelName: string;
  testType: string;
  status: string;
  firstTokenMs?: number;
  fullResponseMs?: number;
  errorMessage?: string;
  testedAt: string;
};

export type RiskEvidenceListItem = {
  id: number;
  siteName: string;
  modelName: string;
  ruleCode: string;
  riskLevel: string;
  riskScore: number;
  evidenceSummary: string;
  reviewStatus: string;
  createdAt: string;
};

export type RiskEvidenceDetail = RiskEvidenceListItem & {
  testRecordId?: number;
  evidenceJson?: string;
  reviewedAt?: string;
  updatedAt: string;
};

export type JobExecutionLogListItem = {
  id: number;
  jobCategory: string;
  jobId?: number;
  status: string;
  message: string;
  startedAt?: string;
  finishedAt?: string;
  createdAt: string;
};

export type JobActionResponse = {
  jobCategory: string;
  jobId?: number;
  status: string;
  affectedCount: number;
  message: string;
};

export function fetchOffers(page = 1, pageSize = 20) {
  return apiRequest<PagedResult<RelayOfferListItem>>(`/api/v1/admin/offers?page=${page}&pageSize=${pageSize}`);
}

export function fetchTestRecords(page = 1, pageSize = 20) {
  return apiRequest<PagedResult<TestRecordListItem>>(`/api/v1/admin/test-records?page=${page}&pageSize=${pageSize}`);
}

export function fetchRisks(page = 1, pageSize = 20) {
  return apiRequest<PagedResult<RiskEvidenceListItem>>(`/api/v1/admin/risks?page=${page}&pageSize=${pageSize}`);
}

export function fetchRisk(id: number) {
  return apiRequest<RiskEvidenceDetail>(`/api/v1/admin/risks/${id}`);
}

export function reviewRisk(id: number, reviewStatus: "pending" | "confirmed" | "false_positive" | "ignored") {
  return apiRequest<void>(`/api/v1/admin/risks/${id}/review`, {
    method: "POST",
    body: JSON.stringify({ reviewStatus })
  });
}

export function fetchJobLogs(page = 1, pageSize = 20) {
  return apiRequest<PagedResult<JobExecutionLogListItem>>(`/api/v1/admin/job-logs?page=${page}&pageSize=${pageSize}`);
}

export function verifyOffer(id: number) {
  return apiRequest<void>(`/api/v1/admin/offers/${id}/verify`, { method: "POST", body: "{}" });
}

export function runManualCrawl() {
  return apiRequest<JobActionResponse>("/api/v1/admin/crawl-jobs/manual-run", { method: "POST", body: "{}" });
}

export function runManualTest() {
  return apiRequest<JobActionResponse>("/api/v1/admin/test-jobs/manual-run", { method: "POST", body: "{}" });
}

export function recalculateRisks() {
  return apiRequest<JobActionResponse>("/api/v1/admin/risks/recalculate", { method: "POST", body: "{}" });
}

export function rebuildRankings() {
  return apiRequest<JobActionResponse>("/api/v1/admin/rankings/rebuild", { method: "POST", body: "{}" });
}
