import { BackLink, PublicHeader } from "@/app/components/PublicHeader";
import { PublicPageHero } from "@/app/components/PublicPageHero";
import { getJson, type PublicEnvelope } from "@/lib/api";
import { formatDateTime } from "@/lib/format";

type CapabilityItem = {
  modelSlug: string;
  modelName: string;
  source: string;
  rankPosition: number;
  capabilityScore: number;
  snapshotAt: string;
};

export default async function CapabilitiesPage() {
  const response = await getJson<PublicEnvelope<CapabilityItem[]>>("/api/v1/public/model-capabilities");
  const items = response?.data ?? [];

  return (
    <main className="public-shell">
      <PublicHeader />

      <section className="public-container public-main">
        <BackLink />
        <PublicPageHero
          eyebrow="Model Capability"
          title="模型能力榜"
          description="能力榜引用第三方公开来源，仅作为价格、稳定性和风险判断之外的参考口径，不参与 CheapAI 的综合评分。"
         
        />

        <section className="data-table">
          <div className="overflow-x-auto">
            <table className="recommend-table">
              <thead>
                <tr>
                  <th>排名</th>
                  <th>模型</th>
                  <th>来源</th>
                  <th>能力分</th>
                  <th>快照时间</th>
                </tr>
              </thead>
              <tbody>
                {items.length > 0 ? (
                  items.map((item) => (
                    <tr key={`${item.source}-${item.modelSlug}`}>
                      <td>
                        <strong>#{item.rankPosition}</strong>
                      </td>
                      <td>
                        <strong>{item.modelName}</strong>
                        <span>{item.modelSlug}</span>
                      </td>
                      <td>{item.source}</td>
                      <td>
                        <strong>{item.capabilityScore}</strong>
                      </td>
                      <td>{formatDateTime(item.snapshotAt)}</td>
                    </tr>
                  ))
                ) : (
                  <tr>
                    <td colSpan={5}>暂无能力榜数据。</td>
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
