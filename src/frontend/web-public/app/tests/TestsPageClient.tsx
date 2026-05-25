"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { RelayTestPanel, type ModelOption } from "@/app/components/RelayTestPanel";
import { formatDateTime, score, trustScore } from "@/lib/format";
import { getJson, type PublicEnvelope } from "@/lib/api";
import { readSelfTestHistory, subscribeSelfTestHistory, type SelfTestHistoryItem } from "@/lib/selfTestHistory";

export type TestRecord = {
  id: number;
  publicId?: string;
  sourceKey?: string;
  siteSlug: string;
  siteName: string;
  siteUrl?: string | null;
  modelSlug: string;
  modelName: string;
  testType: string;
  status: string;
  firstTokenMs?: number | null;
  fullResponseMs?: number | null;
  errorMessage?: string | null;
  riskScore: number;
  riskLevel: string;
  matchScore?: number | null;
  testedAt: string;
};

type TestProbeResult = {
  code: string;
  name: string;
  category: string;
  status: "pass" | "warn" | "fail" | "unknown" | string;
  confidence: "high" | "medium" | "low" | string;
  scoreImpact: number;
  riskImpact: number;
  evidence: string;
};

type TestRecordDetail = TestRecord & {
  resultSummary: string;
  matchScore: number;
  inputTokens?: number | null;
  outputTokens?: number | null;
  totalTokens?: number | null;
  estimatedTokens: number;
  tokensPerSecond?: number | null;
  isStream: boolean;
  checks: TestProbeResult[];
};

type PagedResult<T> = {
  items: T[];
  page: number;
  pageSize: number;
  total: number;
};

export function TestsPageClient({
  payload,
  page,
  maxPages,
  initialDetailId,
  modelOptions = []
}: {
  payload: PagedResult<TestRecord> | null | undefined;
  page: number;
  maxPages: number;
  initialDetailId?: string;
  modelOptions?: ModelOption[];
}) {
  const [activeTab, setActiveTab] = useState<"mine" | "all">("all");
  const [historyItems, setHistoryItems] = useState<SelfTestHistoryItem[]>([]);
  const [myRecords, setMyRecords] = useState<TestRecord[]>([]);
  const [allRecords, setAllRecords] = useState<TestRecord[]>(payload?.items ?? []);
  const [allRecordsLoading, setAllRecordsLoading] = useState(false);
  const [allRecordsTotal, setAllRecordsTotal] = useState(payload?.total ?? 0);
  const [myRecordsLoading, setMyRecordsLoading] = useState(false);
  const [selectedDetail, setSelectedDetail] = useState<TestRecordDetail | null>(null);
  const [detailLoading, setDetailLoading] = useState(false);
  const [detailError, setDetailError] = useState("");

  useEffect(() => {
    setAllRecords(payload?.items ?? []);
    setAllRecordsTotal(payload?.total ?? 0);
  }, [payload]);

  useEffect(() => {
    const syncHistory = () => setHistoryItems(readSelfTestHistory());
    syncHistory();
    return subscribeSelfTestHistory(syncHistory);
  }, []);

  useEffect(() => {
    let cancelled = false;

    async function loadMyRecords() {
      if (historyItems.length === 0) {
        setMyRecords([]);
        setMyRecordsLoading(false);
        return;
      }

      setMyRecordsLoading(true);
      const records = await Promise.all(historyItems.map(loadHistoryRecord));

      if (!cancelled) {
        setMyRecords(records);
        setMyRecordsLoading(false);
      }
    }

    loadMyRecords();

    return () => {
      cancelled = true;
    };
  }, [historyItems]);

  useEffect(() => {
    if (!initialDetailId) {
      return;
    }

    openDetailById(initialDetailId);
  }, [initialDetailId]);

  async function openDetail(record: TestRecord) {
    setDetailError("");
    setSelectedDetail(toDetailFallback(record));

    const detailId = record.publicId || (record.id ? String(record.id) : "");
    if (!detailId) {
      return;
    }

    setDetailLoading(true);
    const response = await getJson<PublicEnvelope<TestRecordDetail>>(`/api/v1/public/tests/${encodeURIComponent(detailId)}`);
    if (response?.data) {
      setSelectedDetail(response.data);
    } else {
      setDetailError("详情加载失败，当前仅显示列表中的基础信息。");
    }
    setDetailLoading(false);
  }

  async function openDetailById(detailId: string) {
    setDetailError("");
    setDetailLoading(true);
    const response = await getJson<PublicEnvelope<TestRecordDetail>>(`/api/v1/public/tests/${encodeURIComponent(detailId)}`);
    if (response?.data) {
      setSelectedDetail(response.data);
    } else {
      setDetailError("详情加载失败。");
    }
    setDetailLoading(false);
  }

  async function refreshAllRecords(targetPage = page) {
    setAllRecordsLoading(true);
    const response = await getJson<PublicEnvelope<PagedResult<TestRecord>>>(`/api/v1/public/tests/latest?page=${targetPage}&pageSize=20`);
    if (response?.data) {
      setAllRecords(response.data.items);
      setAllRecordsTotal(response.data.total);
    }
    setAllRecordsLoading(false);
  }

  function handleSelfTestCompleted() {
    setHistoryItems(readSelfTestHistory());
    refreshAllRecords(1);
    setActiveTab("all");
  }

  const visibleRecords = activeTab === "mine" ? myRecords : allRecords;
  const visibleLoading = activeTab === "mine" ? myRecordsLoading : allRecordsLoading;
  const resolvedMaxPages = Math.max(1, Math.ceil(Math.min(allRecordsTotal, 200) / 20));

  return (
    <>
      <section className="page-hero">
        <div>
          <p className="eyebrow">Testing Protocol</p>
          <h1 className="page-title">中转测试</h1>
          <p className="body-lead mt-5">
            直接填写接口地址、API Key 和模型名完成一次真实请求。
          </p>
        </div>
{/* 
        <aside className="page-hero__aside">
          <div className="metric-card ">
            <span>所有测试次数</span>
            <strong>{payload?.total ?? 0}</strong>
          </div>
          <div className="metric-card mt-3">
            <span>本机我的测试</span>
            <strong>{historyItems.length}</strong>
          </div>
        </aside> */}
      </section>

      <section className="home-workbench">
   
        <div className="market-panel home-workbench__panel">
          <RelayTestPanel compact showHistory={false} modelOptions={modelOptions} onTestCompleted={handleSelfTestCompleted} />
        </div>
      </section>

      <section className="data-table">
        <div className="flex flex-wrap items-center justify-between gap-3 border-b border-[var(--line)] px-5 py-4">
          <div>
            <h2 className="text-xl font-semibold tracking-tight">测试结果</h2>
            <p className="mt-1 text-sm text-[var(--text-secondary)]">按最新测试时间排序。</p>
          </div>
          <div className="tests-switch">
            <button className="secondary-button" data-active={activeTab === "mine"} onClick={() => setActiveTab("mine")} type="button">
              我的测试
            </button>
            <button className="secondary-button" data-active={activeTab === "all"} onClick={() => setActiveTab("all")} type="button">
              所有测试
            </button>
          </div>
        </div>

        <TestRecordTable
          emptyText={activeTab === "mine" ? "本机还没有自测记录。" : "暂无测试记录。"}
          items={visibleRecords}
          loading={visibleLoading}
          onDetail={openDetail}
        />

        {activeTab === "all" ? <Pagination page={page} maxPages={Math.max(maxPages, resolvedMaxPages)} /> : null}
      </section>

      {selectedDetail ? (
        <TestDetailModal
          detail={selectedDetail}
          error={detailError}
          loading={detailLoading}
          onClose={() => {
            setSelectedDetail(null);
            setDetailError("");
          }}
        />
      ) : null}
    </>
  );
}

async function loadHistoryRecord(item: SelfTestHistoryItem): Promise<TestRecord> {
  if (item.testRecordId) {
    const response = await getJson<PublicEnvelope<TestRecordDetail>>(`/api/v1/public/tests/${item.testRecordId}`);
    return response?.data ? detailToRecord(response.data, item.id) : historyToRecord(item);
  }

  return historyToRecord(item);
}

function TestRecordTable({
  items,
  loading,
  emptyText,
  onDetail
}: {
  items: TestRecord[];
  loading: boolean;
  emptyText: string;
  onDetail: (record: TestRecord) => void;
}) {
  return (
    <div className="overflow-x-auto">
      <table className="recommend-table test-record-table">
        <thead>
          <tr>
            <th>站点 / 模型</th>
            <th>测试口径</th>
            <th>速度</th>
            <th>分数</th>
            <th>时间</th>
            <th>详情</th>
          </tr>
        </thead>
        <tbody>
          {loading ? (
            <tr>
              <td colSpan={6}>正在加载测试记录...</td>
            </tr>
          ) : null}

          {!loading && items.length
            ? items.map((item) => (
                <tr key={`${item.id || item.sourceKey}-${item.testedAt}`}>
                  <td>
                    {item.siteSlug ? (
                      <Link href={`/sites/${item.siteSlug}`}>
                        <strong>{item.siteName}</strong>
                      </Link>
                    ) : (
                      <strong>{item.siteName}</strong>
                    )}
                    <span>{item.modelName}</span>
                    {item.siteUrl ? <span>{item.siteUrl}</span> : null}
                  </td>
                  <td>
                    <span className="status-pill" data-tone={statusTone(item.status)}>
                      {statusLabel(item.status)}
                    </span>
                    <span>{testTypeLabel(item.testType)}</span>
                  </td>
                  <td>
                    首 token {formatMs(item.firstTokenMs)}
                    <span>完整响应 {formatMs(item.fullResponseMs)}</span>
                  </td>
                  <td>
                    <span className="status-pill" data-tone="neutral">
                      {score(trustScore(item.matchScore, item.riskScore))}
                    </span>
                    <span>越高越可信</span>
                  </td>
                  <td>{formatDateTime(item.testedAt)}</td>
                  <td>
                    <button className="text-button" onClick={() => onDetail(item)} type="button">
                      查看详情
                    </button>
                  </td>
                </tr>
              ))
            : null}

          {!loading && items.length === 0 ? (
            <tr>
              <td colSpan={6}>{emptyText}</td>
            </tr>
          ) : null}
        </tbody>
      </table>
    </div>
  );
}

function Pagination({ page, maxPages }: { page: number; maxPages: number }) {
  return (
    <div className="flex justify-between gap-3 px-5 py-4">
      <Link className="secondary-button" aria-disabled={page <= 1} href={`/tests?page=${Math.max(1, page - 1)}`}>
        上一页
      </Link>
      <span className="status-pill" data-tone="neutral">
        第 {page} / {maxPages} 页
      </span>
      <Link className="secondary-button" aria-disabled={page >= maxPages} href={`/tests?page=${Math.min(maxPages, page + 1)}`}>
        下一页
      </Link>
    </div>
  );
}

function TestDetailModal({
  detail,
  loading,
  error,
  onClose
}: {
  detail: TestRecordDetail;
  loading: boolean;
  error: string;
  onClose: () => void;
}) {
  return (
    <div className="challenge-modal" role="dialog" aria-modal="true" aria-labelledby="test-detail-title">
      <div className="challenge-modal__backdrop" onClick={onClose} />
      <div className="challenge-modal__panel test-detail-modal">
        <div className="scan-card__header">
          <div>
            <p className="eyebrow">{testTypeLabel(detail.testType)}</p>
            <h2 id="test-detail-title">{detail.siteName} / {detail.modelName}</h2>
          </div>
          <button className="challenge-modal__close" onClick={onClose} type="button">
            关闭
          </button>
        </div>

        {error ? <div className="relay-error mt-4">{error}</div> : null}

        <div className="test-detail-summary mt-5">
          <span className="status-pill" data-tone={statusTone(detail.status)}>
            {loading ? "加载详情中" : statusLabel(detail.status)}
          </span>
          <span className="status-pill" data-tone="neutral">
            分数 {score(trustScore(detail.matchScore, detail.riskScore))}
          </span>
          <span className="status-pill" data-tone="neutral">
            {formatDateTime(detail.testedAt)}
          </span>
        </div>

        <p className="test-detail-copy">{detail.resultSummary || detail.errorMessage || "暂无摘要。"}</p>

        <div className="test-detail-grid">
          <DetailMetric label="分数" value={score(trustScore(detail.matchScore, detail.riskScore))} />
          <DetailMetric label="首 token" value={formatMs(detail.firstTokenMs)} />
          <DetailMetric label="完整响应" value={formatMs(detail.fullResponseMs)} />
          <DetailMetric label="Tokens/s" value={formatOptionalNumber(detail.tokensPerSecond)} />
          <DetailMetric label="输入 Token" value={formatOptionalNumber(detail.inputTokens)} />
          <DetailMetric label="输出 Token" value={formatOptionalNumber(detail.outputTokens)} />
          <DetailMetric label="总 Token" value={formatOptionalNumber(detail.totalTokens)} />
          <DetailMetric label="预估 Token" value={formatOptionalNumber(detail.estimatedTokens)} />
        </div>

        {detail.errorMessage ? <div className="relay-error mt-4">{detail.errorMessage}</div> : null}

        <div className="test-detail-checks">
          <h3>检测项</h3>
          {detail.checks.length ? (
            <div className="probe-grid">
              {detail.checks.map((check) => (
                <article className="probe-card" data-status={check.status} key={`${check.code}-${check.name}`}>
                  <div>
                    <strong>{check.code}</strong>
                    <span>{check.category}</span>
                  </div>
                  <h4>{check.name}</h4>
                  <p>{check.evidence}</p>
                  <small>{probeStatusLabel(check.status)} · {confidenceLabel(check.confidence)}</small>
                </article>
              ))}
            </div>
          ) : (
            <div className="empty-state">当前记录没有检测项明细。</div>
          )}
        </div>
      </div>
    </div>
  );
}

function DetailMetric({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <span>{label}</span>
      <strong>{value}</strong>
    </div>
  );
}

function detailToRecord(detail: TestRecordDetail, sourceKey?: string): TestRecord {
  return {
    id: detail.id,
    publicId: detail.publicId,
    sourceKey,
    siteSlug: detail.siteSlug,
    siteName: detail.siteName,
    siteUrl: detail.siteUrl,
    modelSlug: detail.modelSlug,
    modelName: detail.modelName,
    testType: detail.testType,
    status: detail.status,
    firstTokenMs: detail.firstTokenMs,
    fullResponseMs: detail.fullResponseMs,
    errorMessage: detail.errorMessage,
    riskScore: detail.riskScore,
    riskLevel: detail.riskLevel,
    matchScore: detail.matchScore,
    testedAt: detail.testedAt
  };
}

function historyToRecord(item: SelfTestHistoryItem): TestRecord {
  return {
    id: item.testRecordId ?? 0,
    publicId: item.testRecordId ? String(item.testRecordId) : "",
    sourceKey: item.id,
    siteSlug: item.siteSlug ?? "",
    siteName: item.siteName || hostFromUrl(item.siteUrl) || item.siteUrl,
    siteUrl: item.siteUrl,
    modelSlug: item.modelSlug ?? "",
    modelName: item.modelName,
    testType: "user",
    status: normalizeStatus(item.status),
    firstTokenMs: item.firstTokenMs,
    fullResponseMs: item.fullResponseMs,
    errorMessage: undefined,
    riskScore: item.riskScore,
    riskLevel: item.riskLevel,
    matchScore: item.matchScore,
    testedAt: item.testedAt
  };
}

function toDetailFallback(record: TestRecord): TestRecordDetail {
  return {
    ...record,
    resultSummary: record.errorMessage ?? "该记录正在加载详细检测结果。",
    matchScore: record.matchScore ?? trustScore(null, record.riskScore) ?? 0,
    inputTokens: null,
    outputTokens: null,
    totalTokens: null,
    estimatedTokens: 0,
    tokensPerSecond: null,
    isStream: true,
    checks: []
  };
}

function normalizeStatus(status: string) {
  return status === "succeeded" ? "success" : status;
}

function statusLabel(status: string) {
  const normalized = normalizeStatus(status);
  if (normalized === "success") return "成功";
  if (normalized === "failed" || normalized === "error") return "请求失败";
  if (normalized === "running") return "测试中";
  return normalized || "未知";
}

function statusTone(status: string) {
  const normalized = normalizeStatus(status);
  if (normalized === "success") return "success";
  if (normalized === "failed" || normalized === "error") return "danger";
  if (normalized === "running") return "warning";
  return "neutral";
}

function testTypeLabel(testType: string) {
  if (testType === "user" || testType === "self") return "用户主动测试";
  if (testType === "platform" || testType === "system" || testType === "auto") return "系统自动测试";
  return testType || "未知口径";
}

function probeStatusLabel(status: string) {
  if (status === "pass") return "正常";
  if (status === "warn") return "需关注";
  if (status === "fail") return "请求失败";
  return "未检测";
}

function confidenceLabel(confidence: string) {
  if (confidence === "high") return "依据充分";
  if (confidence === "medium") return "一般参考";
  if (confidence === "low") return "辅助参考";
  return "参考信息";
}

function formatMs(value?: number | null) {
  return typeof value === "number" ? `${value}ms` : "-";
}

function formatOptionalNumber(value?: number | null) {
  return typeof value === "number" ? value.toLocaleString("zh-CN") : "-";
}

function hostFromUrl(value: string) {
  try {
    return new URL(value).host;
  } catch {
    return "";
  }
}
