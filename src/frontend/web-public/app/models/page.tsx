import Link from "next/link";
import { PublicHeader } from "@/app/components/PublicHeader";
import { PublicPageHero } from "@/app/components/PublicPageHero";
import { getJson, type PublicEnvelope } from "@/lib/api";
import { money, riskLabel, riskTone, score } from "@/lib/format";

export const dynamic = "force-dynamic";

type ModelItem = {
  modelSlug: string;
  modelName: string;
  vendor: string;
  officialModelId: string;
  requestName: string;
  apiType: "openai" | "anthropic";
  officialInputPriceUsd?: number;
  officialOutputPriceUsd?: number;
  cheapestSiteSlug?: string;
  cheapestSiteName?: string;
  effectiveInputPriceUsd?: number;
  effectiveOutputPriceUsd?: number;
  stabilityScore?: number;
  riskScore?: number;
  riskLevel: string;
  relaySiteCount: number;
};

type PagedResult<T> = {
  items: T[];
};

export default async function ModelsPage() {
  const response = await getJson<PublicEnvelope<PagedResult<ModelItem>>>("/api/v1/public/models?page=1&pageSize=100");
  const items = response?.data.items ?? [];
  const lowRiskCount = items.filter((item) => item.riskLevel === "low").length;
  const coveredRelayCount = items.reduce((sum, item) => sum + item.relaySiteCount, 0);

  return (
    <main className="public-shell">
      <PublicHeader />

      <section className="public-container public-main">
        <PublicPageHero
          eyebrow="Model Directory"
          title="模型大全"
          description="按模型维度聚合官方定价、当前最便宜中转、稳定性和风险摘要，适合“我先确定模型，再找站”的决策方式。"
          aside={
            <>
              <div className="metric-card">
                <span>收录模型</span>
                <strong>{items.length}</strong>
              </div>
              <div className="metric-card mt-3">
                <span>关联站点覆盖</span>
                <strong>{coveredRelayCount}</strong>
              </div>
            </>
          }
        />

        <section className="data-table">
          <div className="overflow-x-auto">
            <table className="recommend-table">
              <thead>
                <tr>
                  <th>模型</th>
                  <th>官方价</th>
                  <th>最便宜中转</th>
                  <th>中转价</th>
                  <th>稳定 / 风险</th>
                  <th>操作</th>
                </tr>
              </thead>
              <tbody>
                {items.length ? (
                  items.map((item) => (
                    <tr key={item.modelSlug}>
                      <td>
                        <strong>{item.modelName}</strong>
                        <span>
                          {item.vendor} / {item.requestName}
                        </span>
                      </td>
                      <td>
                        {money(item.officialInputPriceUsd)} / {money(item.officialOutputPriceUsd)}
                      </td>
                      <td>
                        {item.cheapestSiteSlug ? (
                          <Link href={`/sites/${item.cheapestSiteSlug}`} className="text-button">
                            {item.cheapestSiteName}
                          </Link>
                        ) : (
                          "-"
                        )}
                        <span>覆盖 {item.relaySiteCount} 个站点</span>
                      </td>
                      <td>
                        {money(item.effectiveInputPriceUsd)} / {money(item.effectiveOutputPriceUsd)}
                      </td>
                      <td>
                        稳定 {score(item.stabilityScore)}
                        <span className="status-pill" data-tone={riskTone(item.riskLevel)}>
                          {riskLabel(item.riskLevel)} {score(item.riskScore)}
                        </span>
                      </td>
                      <td>
                        <Link href={`/rankings?model=${encodeURIComponent(item.modelSlug)}`} className="text-button">
                          查看排名
                        </Link>
                      </td>
                    </tr>
                  ))
                ) : (
                  <tr>
                    <td colSpan={6}>暂无模型数据。</td>
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
