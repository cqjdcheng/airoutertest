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

export type TestProbeResult = {
  code: string;
  name: string;
  category: string;
  status: string;
  confidence: string;
  scoreImpact: number;
  riskImpact: number;
  evidence: string;
};

export type TestRecordDetail = TestRecordListItem & {
  siteUrl?: string;
  modelSlug?: string;
  riskScore: number;
  riskLevel: string;
  resultSummary?: string;
  matchScore: number;
  inputTokens?: number;
  outputTokens?: number;
  totalTokens?: number;
  estimatedTokens: number;
  tokensPerSecond?: number;
  isStream: boolean;
  checks: TestProbeResult[];
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

export type ScheduledJob = {
  key: string;
  name: string;
  rule: string;
  status: string;
  lastRunAt?: string;
  nextRunAt?: string;
};

export function fetchOffers(page = 1, pageSize = 20) {
  return apiRequest<PagedResult<RelayOfferListItem>>(`/api/v1/admin/offers?page=${page}&pageSize=${pageSize}`);
}

export function fetchTestRecords(page = 1, pageSize = 20) {
  return apiRequest<PagedResult<TestRecordListItem>>(`/api/v1/admin/test-records?page=${page}&pageSize=${pageSize}`);
}

export function fetchTestRecord(id: number) {
  return apiRequest<TestRecordDetail>(`/api/v1/admin/test-records/${id}`);
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

export function fetchScheduledJobs() {
  return apiRequest<ScheduledJob[]>("/api/v1/admin/scheduled-jobs");
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
