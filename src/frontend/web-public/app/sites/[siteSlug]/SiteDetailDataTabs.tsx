"use client";

import { useMemo, useState, type CSSProperties } from "react";
import { getJson, type PublicEnvelope } from "@/lib/api";
import { formatDateTime, score, trustScore } from "@/lib/format";
import { selfTestConfidenceLabel, selfTestDeductionLabel, selfTestDisplayIndex, selfTestStatusLabel } from "@/lib/selfTestLabels";

type TabKey = "models" | "tests";

type SupportedModel = {
  modelSlug: string;
  modelName: string;
};

type SiteTestRecord = {
  id: number;
  publicId: string;
  modelSlug: string;
  modelName: string;
  testType: string;
  status: string;
  firstTokenMs?: number;
  fullResponseMs?: number;
  riskScore?: number;
  riskLevel: string;
  matchScore?: number;
  errorMessage?: string | null;
  testedAt?: string | null;
};

type Props = {
  supportedModels: SupportedModel[];
  latestTests: SiteTestRecord[];
};

type TestProbeResult = {
  code: string;
  name: string;
  category: string;
  status: string;
  confidence: string;
  scoreImpact: number;
  riskImpact: number;
  evidence: string;
};

type TestRecordDetail = SiteTestRecord & {
  siteName: string;
  siteUrl?: string | null;
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

const pageSizeMap: Record<TabKey, number> = {
  models: 24,
  tests: 10
};

const tabLabels: Record<TabKey, string> = {
  models: "支持模型",
  tests: "最近测试记录"
};

export function SiteDetailDataTabs({ supportedModels, latestTests }: Props) {
  const [activeTab, setActiveTab] = useState<TabKey>("models");
  const [pages, setPages] = useState<Record<TabKey, number>>({
    models: 1,
    tests: 1
  });
  const [selectedTest, setSelectedTest] = useState<TestRecordDetail | null>(null);
  const [testDetailLoading, setTestDetailLoading] = useState(false);
  const [testDetailError, setTestDetailError] = useState("");

  const totalItems = getTotalItems(activeTab, supportedModels.length, latestTests.length);
  const pageSize = pageSizeMap[activeTab];
  const totalPages = Math.max(1, Math.ceil(totalItems / pageSize));
  const currentPage = Math.min(pages[activeTab], totalPages);
  const pageStart = (currentPage - 1) * pageSize;
  const pageEnd = pageStart + pageSize;

  const visibleModels = useMemo(() => supportedModels.slice(pageStart, pageEnd), [supportedModels, pageStart, pageEnd]);
  const visibleTests = useMemo(() => latestTests.slice(pageStart, pageEnd), [latestTests, pageStart, pageEnd]);

  function updatePage(nextPage: number) {
    setPages((current) => ({
      ...current,
      [activeTab]: Math.min(Math.max(1, nextPage), totalPages)
    }));
  }

  async function openTestDetail(item: SiteTestRecord) {
    setSelectedTest(toDetailFallback(item));
    setTestDetailError("");

    if (!item.publicId) {
      return;
    }

    setTestDetailLoading(true);
    const response = await getJson<PublicEnvelope<TestRecordDetail>>(`/api/v1/public/tests/${encodeURIComponent(item.publicId)}`);
    if (response?.data) {
      setSelectedTest(response.data);
    } else {
      setTestDetailError("详情加载失败，当前仅显示列表中的基础信息。");
    }
    setTestDetailLoading(false);
  }

  return (
    <div className="site-detail-tabs data-table">
      <div className="site-detail-tabs__header">
        <div>
          <p className="eyebrow">Detail Data</p>
          <h2 className="mt-2 text-xl font-semibold tracking-tight">站点明细数据</h2>
          <p className="mt-1 text-sm text-[var(--text-secondary)]">完整模型和最近测试记录集中在这里，避免影响上方关键判断。</p>
        </div>
        <div className="site-detail-tabs__switch" role="tablist" aria-label="站点明细">
          {(["models", "tests"] as TabKey[]).map((tab) => (
            <button
              key={tab}
              type="button"
              role="tab"
              aria-selected={activeTab === tab}
              data-active={activeTab === tab}
              onClick={() => setActiveTab(tab)}
            >
              {tabLabels[tab]}
              <span>{getTotalItems(tab, supportedModels.length, latestTests.length)}</span>
            </button>
          ))}
        </div>
      </div>

      {activeTab === "models" ? (
        <div className="site-detail-model-grid">
          {visibleModels.length ? (
            visibleModels.map((item) => (
              <div key={item.modelSlug} className="site-detail-model-card">
                <strong>{item.modelName}</strong>
                <span>{item.modelSlug}</span>
              </div>
            ))
          ) : (
            <div className="empty-state">暂无模型数据</div>
          )}
        </div>
      ) : null}

      {activeTab === "tests" ? (
        <div className="overflow-x-auto">
          <table className="recommend-table site-test-table">
            <thead>
              <tr>
                <th>模型</th>
                <th>口径 / 状态</th>
                <th>速度</th>
                <th>分数</th>
                <th>时间</th>
                <th>详情</th>
              </tr>
            </thead>
            <tbody>
              {visibleTests.length ? (
                visibleTests.map((item) => (
                  <tr key={`${item.publicId || item.id}-${item.modelSlug}-${item.testedAt}`}>
                    <td>
                      <strong>{item.modelName}</strong>
                      <span>{item.modelSlug}</span>
                    </td>
                    <td>
                      <span>{testTypeLabel(item.testType)}</span>
                      <strong>{statusLabel(item.status)}</strong>
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
                      {item.publicId ? (
                        <button className="text-button" type="button" onClick={() => openTestDetail(item)}>
                          查看详情
                        </button>
                      ) : (
                        "-"
                      )}
                    </td>
                  </tr>
                ))
              ) : (
                <tr>
                  <td colSpan={6}>暂无平台测试数据。</td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      ) : null}

      <div className="site-detail-pagination">
        <span>
          第 {currentPage} / {totalPages} 页，共 {totalItems} 条
        </span>
        <div>
          <button type="button" className="secondary-button" onClick={() => updatePage(currentPage - 1)} disabled={currentPage <= 1}>
            上一页
          </button>
          <button type="button" className="secondary-button" onClick={() => updatePage(currentPage + 1)} disabled={currentPage >= totalPages}>
            下一页
          </button>
        </div>
      </div>

      {selectedTest ? (
        <SiteTestDetailModal
          detail={selectedTest}
          loading={testDetailLoading}
          error={testDetailError}
          onClose={() => {
            setSelectedTest(null);
            setTestDetailError("");
          }}
        />
      ) : null}
    </div>
  );
}

function getTotalItems(tab: TabKey, modelCount: number, testCount: number) {
  if (tab === "models") return modelCount;
  return testCount;
}

function statusLabel(status: string) {
  if (status === "success" || status === "succeeded") return "成功";
  if (status === "failed" || status === "error") return "请求失败";
  return status || "未知";
}

function statusTone(status: string) {
  if (status === "success" || status === "succeeded") return "success";
  if (status === "failed" || status === "error") return "danger";
  return "neutral";
}

function testTypeLabel(testType: string) {
  if (testType === "platform" || testType === "system" || testType === "auto") return "平台测试";
  if (testType === "user" || testType === "self") return "用户自测";
  return testType || "未知测试";
}

function formatMs(value?: number | null) {
  return typeof value === "number" ? `${value}ms` : "-";
}

function formatOptionalNumber(value?: number | null) {
  return typeof value === "number" ? value.toLocaleString("zh-CN") : "-";
}

function DetailMetric({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <span>{label}</span>
      <strong>{value}</strong>
    </div>
  );
}

function toDetailFallback(item: SiteTestRecord): TestRecordDetail {
  return {
    ...item,
    siteName: "",
    siteUrl: null,
    resultSummary: item.errorMessage ?? "正在加载详细检测结果。",
    matchScore: item.matchScore ?? trustScore(null, item.riskScore) ?? 0,
    inputTokens: null,
    outputTokens: null,
    totalTokens: null,
    estimatedTokens: 0,
    tokensPerSecond: null,
    isStream: true,
    checks: []
  };
}

function SiteTestDetailModal({
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
  const resolvedScore = trustScore(detail.matchScore, detail.riskScore) ?? 0;
  const scoreRingStyle = { "--score": `${resolvedScore}%` } as CSSProperties;

  return (
    <div className="challenge-modal" role="dialog" aria-modal="true" aria-labelledby="site-test-detail-title">
      <div className="challenge-modal__backdrop" onClick={onClose} />
      <div className="challenge-modal__panel test-detail-modal">
        <div className="scan-card__header">
          <div>
            <p className="eyebrow">{testTypeLabel(detail.testType)}</p>
            <h2 id="site-test-detail-title">{detail.modelName}</h2>
          </div>
          <button className="challenge-modal__close" onClick={onClose} type="button">
            关闭
          </button>
        </div>

        {error ? <div className="relay-error mt-4">{error}</div> : null}

        <div className="test-detail-hero mt-5">
          <div className="score-ring score-ring--large" style={scoreRingStyle}>
            <strong>{score(resolvedScore, 0)}</strong>
            <span>分</span>
          </div>
          <div className="test-detail-summary">
            <span className="status-pill" data-tone={statusTone(detail.status)}>
              {loading ? "加载详情中" : statusLabel(detail.status)}
            </span>
            <span className="status-pill" data-tone="neutral">
              {formatDateTime(detail.testedAt)}
            </span>
          </div>
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

        <div className="test-detail-checks">
          <h3>检测项</h3>
          {detail.checks?.length ? (
            <div className="probe-grid">
              {detail.checks.map((check, index) => (
                <article className="probe-card" data-status={check.status} key={`${check.code}-${check.name}`}>
                  <div>
                    <strong>{selfTestDisplayIndex(index)}</strong>
                    <span>{check.category}</span>
                  </div>
                  <h4>{check.name}</h4>
                  <p>{check.evidence}</p>
                  <small>{selfTestStatusLabel(check.status)} · {selfTestConfidenceLabel(check.confidence)} · {selfTestDeductionLabel(check)}</small>
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
