"use client";

import { useMemo, useState } from "react";
import Link from "next/link";
import { formatDateTime, money, riskLabel, riskTone, score } from "@/lib/format";
import type { SiteDetailResponse } from "./page";

type TabKey = "models" | "pricing" | "tests";

type Props = {
  supportedModels: SiteDetailResponse["supportedModels"];
  pricing: SiteDetailResponse["pricing"];
  latestTests: SiteDetailResponse["latestTests"];
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
                        <Link className="text-button" href={`/tests?detail=${encodeURIComponent(item.publicId)}`}>
                          查看详情
                        </Link>
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

function testTypeLabel(testType: string) {
  if (testType === "platform" || testType === "system" || testType === "auto") return "平台测试";
  if (testType === "user" || testType === "self") return "用户自测";
  return testType || "未知测试";
}

function formatMs(value?: number | null) {
  return typeof value === "number" ? `${value}ms` : "-";
}
