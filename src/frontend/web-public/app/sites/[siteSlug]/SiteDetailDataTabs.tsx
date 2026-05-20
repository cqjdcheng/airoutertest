"use client";

import { useMemo, useState } from "react";
import Link from "next/link";
import { getJson, type PublicEnvelope } from "@/lib/api";
import { formatDateTime, money, riskLabel, riskTone, score } from "@/lib/format";
import type { SiteDetailResponse } from "./page";

type TabKey = "models" | "pricing" | "tests";
type SiteTestRecord = SiteDetailResponse["latestTests"][number];

type Props = {
  supportedModels: SiteDetailResponse["supportedModels"];
  pricing: SiteDetailResponse["pricing"];
  latestTests: SiteDetailResponse["latestTests"];
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
  pricing: 12,
  tests: 10
};

const tabLabels: Record<TabKey, string> = {
  models: "支持模型",
  pricing: "价格明细",
  tests: "最近测试记录"
};

export function SiteDetailDataTabs({ supportedModels, pricing, latestTests }: Props) {
  const [activeTab, setActiveTab] = useState<TabKey>("models");
  const [pages, setPages] = useState<Record<TabKey, number>>({
    models: 1,
    pricing: 1,
    tests: 1
  });
  const [selectedTest, setSelectedTest] = useState<TestRecordDetail | null>(null);
  const [testDetailLoading, setTestDetailLoading] = useState(false);
  const [testDetailError, setTestDetailError] = useState("");

  const totalItems = getTotalItems(activeTab, supportedModels.length, pricing.length, latestTests.length);
  const pageSize = pageSizeMap[activeTab];
  const totalPages = Math.max(1, Math.ceil(totalItems / pageSize));
  const currentPage = Math.min(pages[activeTab], totalPages);
  const pageStart = (currentPage - 1) * pageSize;
  const pageEnd = pageStart + pageSize;

  const visibleModels = useMemo(() => supportedModels.slice(pageStart, pageEnd), [supportedModels, pageStart, pageEnd]);
  const visiblePricing = useMemo(() => pricing.slice(pageStart, pageEnd), [pricing, pageStart, pageEnd]);
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
          <p className="mt-1 text-sm text-[var(--text-secondary)]">完整模型、价格和最近测试记录集中在这里，避免影响上方关键判断。</p>
        </div>
        <div className="site-detail-tabs__switch" role="tablist" aria-label="站点明细">
          {(["models", "pricing", "tests"] as TabKey[]).map((tab) => (
            <button
              key={tab}
              type="button"
              role="tab"
              aria-selected={activeTab === tab}
              data-active={activeTab === tab}
              onClick={() => setActiveTab(tab)}
            >
              {tabLabels[tab]}
              <span>{getTotalItems(tab, supportedModels.length, pricing.length, latestTests.length)}</span>
            </button>
          ))}
        </div>
      </div>

      {activeTab === "models" ? (
        <div className="site-detail-model-grid">
          {visibleModels.length ? (
            visibleModels.map((item) => (
              <Link key={item.modelSlug} href={`/rankings?model=${encodeURIComponent(item.modelSlug)}`} className="site-detail-model-card">
                <strong>{item.modelName}</strong>
                <span>{item.modelSlug}</span>
              </Link>
            ))
          ) : (
            <div className="empty-state">暂无模型数据</div>
          )}
        </div>
      ) : null}

      {activeTab === "pricing" ? (
        <div className="overflow-x-auto">
          <table className="recommend-table">
            <thead>
              <tr>
                <th>模型</th>
                <th>折算价</th>
                <th>价格状态</th>
                <th>操作</th>
              </tr>
            </thead>
            <tbody>
              {visiblePricing.length ? (
                visiblePricing.map((item) => (
                  <tr key={`${item.modelSlug}-${item.effectiveInputPriceUsd}-${item.effectiveOutputPriceUsd}`}>
                    <td>
                      <strong>{item.modelName}</strong>
                      <span>{item.modelSlug}</span>
                    </td>
                    <td>
                      <strong>
                        {money(item.effectiveInputPriceUsd)} / {money(item.effectiveOutputPriceUsd)}
                      </strong>
                      <span>输入 / 输出</span>
                    </td>
                    <td>
                      <span className="status-pill" data-tone={hasValidPrice(item) ? "success" : "neutral"}>
                        {hasValidPrice(item) ? "有效价格" : "待补充"}
                      </span>
                    </td>
                    <td>
                      <Link href={`/rankings?model=${encodeURIComponent(item.modelSlug)}`} className="text-button">
                        查看排行
                      </Link>
                    </td>
                  </tr>
                ))
              ) : (
                <tr>
                  <td colSpan={4}>暂无价格数据</td>
                </tr>
              )}
            </tbody>
          </table>
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
                <th>风险</th>
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
                      <span className="status-pill" data-tone={riskTone(item.riskLevel)}>
                        {riskLabel(item.riskLevel)} {score(item.riskScore)}
                      </span>
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

function getTotalItems(tab: TabKey, modelCount: number, pricingCount: number, testCount: number) {
  if (tab === "models") return modelCount;
  if (tab === "pricing") return pricingCount;
  return testCount;
}

function hasValidPrice(item: SiteDetailResponse["pricing"][number]) {
  return (
    typeof item.effectiveInputPriceUsd === "number" &&
    item.effectiveInputPriceUsd >= 0 &&
    typeof item.effectiveOutputPriceUsd === "number" &&
    item.effectiveOutputPriceUsd >= 0
  );
}

function statusLabel(status: string) {
  if (status === "success" || status === "succeeded") return "成功";
  if (status === "failed" || status === "error") return "失败";
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

function probeStatusLabel(status: string) {
  if (status === "pass") return "通过";
  if (status === "warn") return "可疑";
  if (status === "fail") return "失败";
  return "未知";
}

function confidenceLabel(confidence: string) {
  if (confidence === "high") return "高置信";
  if (confidence === "medium") return "中置信";
  if (confidence === "low") return "低置信";
  return "未知置信";
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
    matchScore: 0,
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

        <div className="test-detail-summary mt-5">
          <span className="status-pill" data-tone={statusTone(detail.status)}>
            {loading ? "加载详情中" : statusLabel(detail.status)}
          </span>
          <span className="status-pill" data-tone={riskTone(detail.riskLevel)}>
            {riskLabel(detail.riskLevel)} / {score(detail.riskScore)}
          </span>
          <span className="status-pill" data-tone="neutral">
            {formatDateTime(detail.testedAt)}
          </span>
        </div>

        <p className="test-detail-copy">{detail.resultSummary || detail.errorMessage || "暂无摘要。"}</p>

        <div className="test-detail-grid">
          <DetailMetric label="匹配度" value={score(detail.matchScore)} />
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
              {detail.checks.map((check) => (
                <article className="probe-card" data-status={check.status} key={`${check.code}-${check.name}`}>
                  <div>
                    <strong>{check.code}</strong>
                    <span>{check.category}</span>
                  </div>
                  <h4>{check.name}</h4>
                  <p>{check.evidence}</p>
                  <small>{probeStatusLabel(check.status)} / {confidenceLabel(check.confidence)}</small>
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
