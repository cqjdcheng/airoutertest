"use client";

import Link from "next/link";
import { useState } from "react";
import { money, percent, riskLabel, riskTone, score } from "@/lib/format";

export type CheapestRankingGroup = {
  modelSlug: string;
  modelName?: string;
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
    <section className="market-panel">
      <div className="market-panel__tabs">
        {groups.map((group) => (
          <button
            className="secondary-button"
            data-active={group.modelSlug === activeSlug}
            key={group.modelSlug}
            onClick={() => setActiveSlug(group.modelSlug)}
            type="button"
          >
            {group.modelName || group.modelSlug}
          </button>
        ))}
      </div>

      <div className="market-table-shell overflow-x-auto">
        <table className="recommend-table home-ranking-table">
          <thead>
            <tr>
              <th>站点</th>
              <th>中转价（USD/M）</th>
              <th>稳定 / 风险</th>
              <th>特点</th>
              <th>操作</th>
            </tr>
          </thead>
          <tbody>
            {activeGroup?.items.length ? (
              activeGroup.items.map((item, index) => {
                const enterpriseBadges = [
                  item.supportsInvoice ? "开票" : null,
                  item.supportsRefund ? "退款" : null,
                  item.hasDocs ? "文档" : null
                ].filter((badge): badge is string => Boolean(badge));

                return (
                  <tr key={`${activeGroup.modelSlug}-${item.siteSlug}`}>
                    <td>
                      <strong>#{index + 1} {item.siteName}</strong>
                      <span>{item.siteSlug}</span>
                    </td>
                    <td>
                      <strong>{money(item.effectiveInputPriceUsd)} / {money(item.effectiveOutputPriceUsd)}</strong>
                      <span>输入 / 输出</span>
                    </td>
                    <td>
                      {/* <span>可用 {percent(item.availability24h)}</span>
                      <span>稳定 {percent(item.stability7d)}</span> */}
                      <span className="status-pill" data-tone={riskTone(item.riskLevel)}>
                        {riskLabel(item.riskLevel)} {score(item.riskScore)}
                      </span>
                    </td>
                    <td>
                      {enterpriseBadges.length ? enterpriseBadges.join(" / ") : "-"}
                    </td>
                    <td>
                      <Link href={`/sites/${item.siteSlug}`} className="text-button">
                        查看站点
                      </Link>
                    </td>
                  </tr>
                );
              })
            ) : (
              <tr>
                <td colSpan={5}>暂无价格快照，请先运行后台排行重建。</td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </section>
  );
}
