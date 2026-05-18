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
          description="按综合评分整理中转站，优先看风险、最近测试和模型覆盖，再进入详情核对价格。"
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
                  <th>稳定 / 风险</th>
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
                        <strong>#{index + 1} {item.siteName}</strong>
                        <span>{item.description ?? item.siteSlug}</span>
                      </td>
                      <td>
                        <strong>{score(item.siteScore)}</strong>
                      </td>
                      <td>
                        {/* <span>可用 {formatScorePercent(item.availabilityScore)}</span>
                        <span>稳定 {formatScorePercent(item.stabilityScore)}</span> */}
                        <span className="status-pill" data-tone={riskTone(item.riskLevel)}>
                          {riskLabel(item.riskLevel)} {score(item.riskScore)}
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
