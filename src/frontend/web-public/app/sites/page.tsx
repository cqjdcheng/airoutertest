import Link from "next/link";
import { PublicHeader } from "@/app/components/PublicHeader";
import { getJson, type PublicEnvelope } from "@/lib/api";
import { formatDateTime, percent, riskLabel, riskTone, score } from "@/lib/format";

export const dynamic = "force-dynamic";

type SiteItem = {
  siteSlug: string;
  siteName: string;
  description?: string;
  supportsInvoice: boolean;
  supportsRefund: boolean;
  hasDocs: boolean;
  siteScore: number;
  availabilityScore: number;
  stabilityScore: number;
  riskScore: number;
  riskLevel: string;
  coveredModelCount: number;
  latestTestAt?: string;
};

type PagedResult<T> = {
  items: T[];
  total: number;
};

export default async function SitesPage() {
  const response = await getJson<PublicEnvelope<PagedResult<SiteItem>>>("/api/v1/public/sites?page=1&pageSize=100");
  const items = response?.data.items ?? [];

  return (
    <main className="min-h-screen">
      <PublicHeader />
      <section className="public-container py-10 lg:py-14">
        <p className="eyebrow">Relay Directory</p>
        <h1 className="page-title mt-4">中转站大全</h1>
        <p className="body-lead mt-5 max-w-3xl">
          根据可用性、稳定性、风险分和企业属性计算中转站综合评分，默认按评分从高到低展示。
        </p>

        <section className="mt-8 grid gap-4 sm:grid-cols-4">
          <Metric label="收录站点" value={items.length} />
          <Metric label="企业友好" value={items.filter((item) => item.supportsInvoice && item.supportsRefund && item.hasDocs).length} />
          <Metric label="低风险" value={items.filter((item) => item.riskLevel === "low").length} />
          <Metric label="平均分" value={items.length ? Math.round(items.reduce((sum, item) => sum + item.siteScore, 0) / items.length) : 0} />
        </section>

        <section className="mt-8 grid gap-5">
          {items.length ? (
            items.map((item, index) => (
              <article className="panel-card p-6" key={item.siteSlug}>
                <div className="grid gap-5 lg:grid-cols-[1fr_220px]">
                  <div>
                    <div className="flex flex-wrap items-center gap-3">
                      <span className="status-pill" data-tone="neutral">#{index + 1}</span>
                      <Link href={`/sites/${item.siteSlug}`} className="text-2xl font-semibold tracking-tight text-[var(--text-primary)] hover:text-[var(--brand)]">
                        {item.siteName}
                      </Link>
                      <span className="status-pill" data-tone={riskTone(item.riskLevel)}>{riskLabel(item.riskLevel)}</span>
                    </div>
                    <p className="mt-3 text-sm leading-7 text-[var(--text-secondary)]">{item.description ?? "暂无站点简介。"}</p>
                    <div className="mt-4 flex flex-wrap gap-2">
                      {item.supportsInvoice ? <span className="status-pill" data-tone="neutral">支持开票</span> : null}
                      {item.supportsRefund ? <span className="status-pill" data-tone="neutral">支持退款</span> : null}
                      {item.hasDocs ? <span className="status-pill" data-tone="neutral">有文档</span> : null}
                      <span className="status-pill" data-tone="neutral">覆盖 {item.coveredModelCount} 个模型</span>
                    </div>
                  </div>
                  <div className="rounded-3xl bg-[var(--surface-muted)] p-5">
                    <div className="text-sm text-[var(--text-secondary)]">综合评分</div>
                    <div className="mt-2 text-4xl font-semibold">{score(item.siteScore)}</div>
                    <div className="mt-4 text-sm leading-7 text-[var(--text-secondary)]">
                      可用 {percent(item.availabilityScore)}
                      <br />
                      稳定 {percent(item.stabilityScore)}
                      <br />
                      风险 {score(item.riskScore)}
                      <br />
                      最近 {formatDateTime(item.latestTestAt)}
                    </div>
                  </div>
                </div>
              </article>
            ))
          ) : (
            <div className="panel-card p-6 text-sm text-[var(--text-secondary)]">暂无中转站数据。</div>
          )}
        </section>
      </section>
    </main>
  );
}

function Metric({ label, value }: { label: string; value: string | number }) {
  return (
    <div className="panel-card p-5">
      <div className="text-sm text-[var(--text-secondary)]">{label}</div>
      <div className="mt-2 text-3xl font-semibold">{value}</div>
    </div>
  );
}
