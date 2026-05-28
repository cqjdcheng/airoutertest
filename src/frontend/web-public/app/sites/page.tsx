import Link from "next/link";
import { PublicHeader } from "@/app/components/PublicHeader";
import { PublicPageHero } from "@/app/components/PublicPageHero";
import { SiteStatusBar, type SiteStatus24h } from "@/app/components/SiteStatus24h";
import { getJson, type PublicEnvelope } from "@/lib/api";
import { formatDateTime, percent, score, trustScore } from "@/lib/format";

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
  status24h: SiteStatus24h;
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
          description="按统一分数整理中转站，优先看稳定性、24 小时状态和最近测试。价格变化很快，不作为品质判断的核心依据。"
          aside={
            <>
              <div className="metric-card">
                <span>收录站点</span>
                <strong>{items.length}</strong>
              </div>
            </>
          }
        />

        <section className="data-table">
          <div className="overflow-x-auto">
            <table className="recommend-table site-directory-table">
              <thead>
                <tr>
                  <th>中转站</th>
                  <th>综合评分</th>
                  <th>24 小时状态</th>
                  <th>覆盖 / 最近测试</th>
                  <th>特点</th>
                  <th>操作</th>
                </tr>
              </thead>
              <tbody>
                {items.length ? (
                  items.map((item, index) => (
                    <tr key={item.siteSlug}>
                      <td>
                        <strong>
                          #{index + 1} {item.siteName}
                        </strong>
                        <span>{item.description ?? item.siteSlug}</span>
                      </td>
                      <td>
                        <strong>{score(item.siteScore)}</strong>
                        <span>稳定 {formatScorePercent(item.stabilityScore)}</span>
                      </td>
                      <td>
                        <SiteStatusBar status24h={item.status24h} />
                        <span className="status-pill" data-tone="neutral">
                          分数 {score(trustScore(null, item.riskScore))}
                        </span>
                      </td>
                      <td>
                        <strong>{formatModelCount(item.coveredModelCount)}</strong>
                        <span>最近 {formatDateTime(item.latestTestAt)}</span>
                      </td>
                      <td>{formatEnterprise(item)}</td>
                      <td>
                        <Link href={`/sites/${item.siteSlug}`} className="text-button">
                          查看详情
                        </Link>
                      </td>
                    </tr>
                  ))
                ) : (
                  <tr>
                    <td colSpan={6}>暂无中转站数据。</td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        </section>
      </section>
    </main>
  );
}

function formatScorePercent(value?: number | null) {
  return typeof value === "number" && value > 0 ? percent(value) : "暂无";
}

function formatModelCount(value?: number | null) {
  return typeof value === "number" && value > 0 ? `${value} 个` : "待同步";
}

function formatEnterprise(item: Pick<SiteItem, "supportsInvoice" | "supportsRefund" | "hasDocs">) {
  const values = [
    item.supportsInvoice ? "开票" : null,
    item.supportsRefund ? "退款" : null,
    item.hasDocs ? "文档" : null
  ].filter(Boolean);

  return values.length ? values.join(" / ") : "-";
}
