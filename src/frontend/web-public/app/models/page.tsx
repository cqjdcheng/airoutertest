import Link from "next/link";
import { PublicHeader } from "@/app/components/PublicHeader";
import { getJson, type PublicEnvelope } from "@/lib/api";
import { money, riskLabel, riskTone, score } from "@/lib/format";

export const dynamic = "force-dynamic";

type ModelItem = {
  modelSlug: string;
  modelName: string;
  vendor: string;
  officialModelId: string;
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

  return (
    <main className="min-h-screen">
      <PublicHeader />
      <section className="public-container py-10 lg:py-14">
        <p className="eyebrow">Model Directory</p>
        <h1 className="page-title mt-4">模型大全</h1>
        <p className="body-lead mt-5 max-w-3xl">
          展示所有收录模型，并按“模型 + 中转站”维度给出当前最便宜中转和稳定性摘要。
        </p>

        <section className="data-table mt-8">
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
                        <span>{item.vendor} / {item.officialModelId}</span>
                      </td>
                      <td>{money(item.officialInputPriceUsd)} / {money(item.officialOutputPriceUsd)}</td>
                      <td>
                        {item.cheapestSiteSlug ? (
                          <Link href={`/sites/${item.cheapestSiteSlug}`} className="text-button">
                            {item.cheapestSiteName}
                          </Link>
                        ) : "-"}
                        <span>覆盖 {item.relaySiteCount} 个站点</span>
                      </td>
                      <td>{money(item.effectiveInputPriceUsd)} / {money(item.effectiveOutputPriceUsd)}</td>
                      <td>
                        稳定 {score(item.stabilityScore)}
                        <span className="status-pill" data-tone={riskTone(item.riskLevel)}>
                          {riskLabel(item.riskLevel)} {score(item.riskScore)}
                        </span>
                      </td>
                      <td>
                        <Link href={`/rankings/${item.modelSlug}`} className="text-button">
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
