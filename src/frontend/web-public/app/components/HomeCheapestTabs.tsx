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
            {group.modelSlug}
          </button>
        ))}
      </div>

      <div className="market-list">
        {activeGroup?.items.length ? (
          activeGroup.items.map((item, index) => {
            const enterpriseBadges = [
              item.supportsInvoice ? "开票" : null,
              item.supportsRefund ? "退款" : null,
              item.hasDocs ? "文档" : null
            ].filter(Boolean);

            return (
              <article className="market-row" key={`${activeGroup.modelSlug}-${item.siteSlug}`}>
                <span className="market-row__rank">#{index + 1}</span>

                <div className="market-row__body">
                  <h3>{item.siteName}</h3>
                  <p>
                    24h 可用 {percent(item.availability24h)}，7d 稳定 {percent(item.stability7d)}，风险分 {score(item.riskScore)}。
                  </p>
                  <div className="market-row__meta">
                    <span>{item.siteSlug}</span>
                    <span className="status-pill" data-tone={riskTone(item.riskLevel)}>
                      {riskLabel(item.riskLevel)}
                    </span>
                  </div>
                  {enterpriseBadges.length ? (
                    <div className="market-row__badges">
                      {enterpriseBadges.map((badge) => (
                        <span className="status-pill" data-tone="neutral" key={badge}>
                          {badge}
                        </span>
                      ))}
                    </div>
                  ) : null}
                </div>

                <div className="market-row__price">
                  <strong>
                    {money(item.effectiveInputPriceUsd)} / {money(item.effectiveOutputPriceUsd)}
                  </strong>
                  <span>输入价 / 输出价</span>
                </div>

                <div className="market-row__cta">
                  <Link href={`/sites/${item.siteSlug}`} className="secondary-button">
                    查看站点
                  </Link>
                </div>
              </article>
            );
          })
        ) : (
          <div className="empty-state">暂无价格快照，请先运行后台排行重建。</div>
        )}
      </div>
    </section>
  );
}
