import Link from "next/link";
import { PublicHeader } from "@/app/components/PublicHeader";
import { PublicPageHero } from "@/app/components/PublicPageHero";
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
    <main className="public-shell">
      <PublicHeader />

      <section className="public-container public-main">
        <PublicPageHero
          eyebrow="Relay Directory"
          title="中转站大全"
          description="根据可用性、稳定性、风险分和企业属性计算中转站综合评分，适合“我先找站，再看覆盖模型和售后”的决策方式。"
          aside={
            <>
              <div className="metric-card">
                <span>收录站点</span>
                <strong>{items.length}</strong>
              </div>
            
            </>
          }
        />

       

        <section className="directory-list">
          {items.length ? (
            items.map((item, index) => (
              <article className="directory-card" key={item.siteSlug}>
                <div className="directory-card__split">
                  <div>
                    <div className="flex flex-wrap items-center gap-3">
                      <span className="status-pill" data-tone="neutral">
                        #{index + 1}
                      </span>
                      <Link href={`/sites/${item.siteSlug}`} className="text-2xl font-semibold tracking-tight text-[var(--text-primary)] transition hover:text-[var(--brand)]">
                        {item.siteName}
                      </Link>
                      <span className="status-pill" data-tone={riskTone(item.riskLevel)}>
                        {riskLabel(item.riskLevel)}
                      </span>
                    </div>
                    <p className="mt-3 text-sm leading-7 text-[var(--text-secondary)]">{item.description ?? "暂无站点简介。"}</p>
                    <div className="mt-4 flex flex-wrap gap-2">
                      {item.supportsInvoice ? <span className="status-pill" data-tone="neutral">支持开票</span> : null}
                      {item.supportsRefund ? <span className="status-pill" data-tone="neutral">支持退款</span> : null}
                      {item.hasDocs ? <span className="status-pill" data-tone="neutral">有文档</span> : null}
                      <span className="status-pill" data-tone="neutral">
                        覆盖 {item.coveredModelCount} 个模型
                      </span>
                    </div>
                  </div>

                  <div className="directory-card__score">
                    <div>综合评分</div>
                    <div>{score(item.siteScore)}</div>
                    <p>
                      可用 {percent(item.availabilityScore)}
                      <br />
                      稳定 {percent(item.stabilityScore)}
                      <br />
                      风险 {score(item.riskScore)}
                      <br />
                      最近 {formatDateTime(item.latestTestAt)}
                    </p>
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
    <div className="metric-card">
      <span>{label}</span>
      <strong>{value}</strong>
    </div>
  );
}
