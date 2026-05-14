import Link from "next/link";
import { BackLink, PublicHeader } from "@/app/components/PublicHeader";
import { PublicPageHero } from "@/app/components/PublicPageHero";
import { getJson, type PublicEnvelope } from "@/lib/api";
import { formatDateTime, money, percent, riskLabel, riskTone, score } from "@/lib/format";

export const dynamic = "force-dynamic";

type RankingItem = {
  siteSlug: string;
  siteName: string;
  effectiveInputPriceUsd?: number;
  effectiveOutputPriceUsd?: number;
  availability24h?: number;
  stability7d?: number;
  firstTokenMs?: number;
  fullResponseMs?: number;
  riskScore?: number;
  riskLevel: string;
  supportsInvoice: boolean;
  supportsRefund: boolean;
  hasDocs: boolean;
};

type RankingResponse = {
  model: {
    slug: string;
    displayName: string;
  };
  rankingType: string;
  window: string;
  snapshotAt?: string | null;
  result: {
    items: RankingItem[];
    page: number;
    pageSize: number;
    total: number;
  };
};

export default async function ModelRankingPage({ params }: { params: Promise<{ modelSlug: string }> }) {
  const { modelSlug } = await params;
  const response = await getJson<PublicEnvelope<RankingResponse>>(
    `/api/v1/public/rankings/models/${modelSlug}?rankingType=price&window=7d&page=1&pageSize=20`
  );
  const payload = response?.data;
  const items = payload?.result.items ?? [];
  const bestItem = items[0];
  const enterpriseReadyCount = items.filter((item) => item.supportsInvoice && item.supportsRefund && item.hasDocs).length;
  const lowRiskCount = items.filter((item) => item.riskLevel === "low").length;

  return (
    <main className="public-shell">
      <PublicHeader featuredModel={modelSlug} />

      <section className="public-container public-main">
        <BackLink />
        <PublicPageHero
          eyebrow="Model Ranking"
          title={`${payload?.model.displayName ?? modelSlug} 价格排行`}
          description="当前榜单按实际折算价展示，同时保留稳定性、速度、风险和企业属性，避免只按低价排序。"
          aside={
            <>
              <div className="metric-card">
                <span>快照时间</span>
                <strong>{formatDateTime(payload?.snapshotAt)}</strong>
              </div>
              <div className="mt-3 flex flex-wrap gap-2">
                <span className="status-pill" data-tone="neutral">
                  {payload?.window ?? "7d"}
                </span>
                <span className="status-pill" data-tone="success">
                  平台定时测试
                </span>
              </div>
            </>
          }
        />

        <section className="metric-overview">
          <MetricCard
            label="最低折算价"
            value={bestItem ? `${money(bestItem.effectiveInputPriceUsd)} / ${money(bestItem.effectiveOutputPriceUsd)}` : "-"}
            hint="按输入价 + 输出价排序"
          />
          <MetricCard label="可选站点" value={payload?.result.total ?? 0} hint="来自排行快照，不实时拼装" />
          <MetricCard label="低风险" value={lowRiskCount} hint="风险分规则累加并封顶 100" />
          <MetricCard label="企业友好" value={enterpriseReadyCount} hint="同时支持开票、退款、文档" />
        </section>

        <section className="insight-grid">
          {[
            ["个人开发者", "优先看实际折算价和低风险标签，避免只被站点标价吸引。"],
            ["企业采购", "优先筛选开票、退款、文档和稳定性，价格只作为次级条件。"],
            ["极致低价", "低价入口需要同时看风险分和最近测试时间，防止缓存假响应或模型降级。"]
          ].map(([title, description]) => (
            <article className="insight-card" key={title}>
              <h2>{title}</h2>
              <p>{description}</p>
            </article>
          ))}
        </section>

        <section className="data-table">
          <div className="hidden grid-cols-12 border-b border-[var(--line)] px-5 py-3 text-xs font-semibold uppercase tracking-wide text-[var(--text-tertiary)] md:grid">
            <div className="col-span-3">站点</div>
            <div className="col-span-2">价格 USD</div>
            <div className="col-span-2">稳定性</div>
            <div className="col-span-2">速度</div>
            <div className="col-span-2">风险</div>
            <div className="col-span-1">企业</div>
          </div>

          {items.length > 0 ? (
            <div className="divide-y divide-[var(--line)]">
              {items.map((item, index) => (
                <article key={`${item.siteSlug}-${item.riskLevel}-${index}`} className="grid gap-4 px-5 py-5 text-sm md:grid-cols-12 md:items-center">
                  <div className="md:col-span-3">
                    <div className="flex items-center gap-3">
                      <span className="inline-flex size-8 shrink-0 items-center justify-center rounded-full bg-[var(--surface-soft)] text-xs font-semibold text-[var(--text-secondary)]">
                        #{index + 1}
                      </span>
                      <div>
                        <Link className="font-semibold text-[var(--text-primary)] transition hover:text-[var(--brand)]" href={`/sites/${item.siteSlug}`}>
                          {item.siteName}
                        </Link>
                        <div className="mt-1 text-[var(--text-secondary)]">{item.siteSlug}</div>
                      </div>
                    </div>
                  </div>
                  <div className="grid grid-cols-2 gap-3 md:col-span-2 md:block">
                    <div>
                      <div className="text-[var(--text-tertiary)] md:hidden">输入</div>
                      <div className="font-semibold">{money(item.effectiveInputPriceUsd)}</div>
                    </div>
                    <div>
                      <div className="text-[var(--text-tertiary)] md:hidden">输出</div>
                      <div className="text-[var(--text-secondary)]">{money(item.effectiveOutputPriceUsd)}</div>
                    </div>
                  </div>
                  <div className="grid grid-cols-2 gap-3 md:col-span-2 md:block">
                    <div>24h {percent(item.availability24h)}</div>
                    <div className="text-[var(--text-secondary)]">7d {percent(item.stability7d)}</div>
                  </div>
                  <div className="grid grid-cols-2 gap-3 md:col-span-2 md:block">
                    <div>首 token {item.firstTokenMs ?? "-"}ms</div>
                    <div className="text-[var(--text-secondary)]">完整 {item.fullResponseMs ?? "-"}ms</div>
                  </div>
                  <div className="md:col-span-2">
                    <span className="status-pill" data-tone={riskTone(item.riskLevel)}>
                      {riskLabel(item.riskLevel)}
                    </span>
                    <div className="mt-2 text-[var(--text-secondary)]">风险分 {score(item.riskScore)}</div>
                  </div>
                  <div className="flex flex-wrap gap-2 md:col-span-1">
                    {item.supportsInvoice ? <span className="status-pill" data-tone="neutral">开票</span> : null}
                    {item.supportsRefund ? <span className="status-pill" data-tone="neutral">退款</span> : null}
                    {item.hasDocs ? <span className="status-pill" data-tone="neutral">文档</span> : null}
                  </div>
                </article>
              ))}
            </div>
          ) : (
            <div className="px-5 py-16 text-sm text-[var(--text-secondary)]">暂无榜单数据，请先确认排行快照已生成。</div>
          )}
        </section>
      </section>
    </main>
  );
}

function MetricCard({ label, value, hint }: { label: string; value: string | number; hint: string }) {
  return (
    <div className="metric-card">
      <span>{label}</span>
      <strong>{value}</strong>
      <p className="mt-2 text-xs text-[var(--text-tertiary)]">{hint}</p>
    </div>
  );
}
