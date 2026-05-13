"use client";

import Link from "next/link";
import { useState } from "react";
import { money, percent, riskLabel, riskTone, score } from "@/lib/format";

export type CheapestRankingGroup = {
  modelSlug: string;
  items: Array<{
    siteSlug: string;
    siteName: string;
    effectiveInputPriceUsd?: number;
    effectiveOutputPriceUsd?: number;
    availability24h?: number;
    stability7d?: number;
    riskScore?: number;
    riskLevel: string;
    supportsInvoice: boolean;
    supportsRefund: boolean;
    hasDocs: boolean;
  }>;
};

export function HomeCheapestTabs({ groups }: { groups: CheapestRankingGroup[] }) {
  const [activeSlug, setActiveSlug] = useState(groups[0]?.modelSlug ?? "gpt-5.5");
  const activeGroup = groups.find((group) => group.modelSlug === activeSlug) ?? groups[0];

  return (
    <section className="data-table">
      <div className="flex flex-wrap items-center justify-between gap-4 border-b border-[var(--line)] px-5 py-4">
        <div>
          <h2 className="text-xl font-semibold tracking-tight">主流模型最便宜中转排行</h2>
          <p className="mt-1 text-sm text-[var(--text-secondary)]">按实际折算价排序，可切换主流模型查看中转站。</p>
        </div>
        <div className="flex max-w-full gap-2 overflow-x-auto">
          {groups.map((group) => (
            <button
              className="secondary-button"
              data-active={group.modelSlug === activeSlug}
              key={group.modelSlug}
              onClick={() => setActiveSlug(group.modelSlug)}
              type="button"
            >
              {group.modelSlug}
            </button>
          ))}
        </div>
      </div>

      <div className="overflow-x-auto">
        <table className="recommend-table">
          <thead>
            <tr>
              <th>中转站</th>
              <th>价格</th>
              <th>稳定性</th>
              <th>风险</th>
              <th>企业属性</th>
              <th>操作</th>
            </tr>
          </thead>
          <tbody>
            {activeGroup?.items.length ? (
              activeGroup.items.map((item) => (
                <tr key={`${activeGroup.modelSlug}-${item.siteSlug}`}>
                  <td>
                    <strong>{item.siteName}</strong>
                    <span>{item.siteSlug}</span>
                  </td>
                  <td>
                    {money(item.effectiveInputPriceUsd)} / {money(item.effectiveOutputPriceUsd)}
                  </td>
                  <td>
                    24h {percent(item.availability24h)}
                    <span>7d {percent(item.stability7d)}</span>
                  </td>
                  <td>
                    <span className="status-pill" data-tone={riskTone(item.riskLevel)}>
                      {riskLabel(item.riskLevel)}
                    </span>
                    <span>风险分 {score(item.riskScore)}</span>
                  </td>
                  <td>
                    {[item.supportsInvoice ? "开票" : null, item.supportsRefund ? "退款" : null, item.hasDocs ? "文档" : null]
                      .filter(Boolean)
                      .join(" / ") || "-"}
                  </td>
                  <td>
                    <Link href={`/sites/${item.siteSlug}`} className="text-button">
                      详情
                    </Link>
                  </td>
                </tr>
              ))
            ) : (
              <tr>
                <td colSpan={6}>暂无价格快照，请先运行后台排行重建。</td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </section>
  );
}
