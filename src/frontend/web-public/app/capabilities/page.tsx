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
          aside={
            <>
              <div className="metric-card">
                <span>收录条目</span>
                <strong>{items.length}</strong>
              </div>
              <div className="metric-card mt-3">
                <span>用途定位</span>
                <strong>辅助参考</strong>
              </div>
            </>
          }
        />

        <section className="data-table">
          {items.length > 0 ? (
            <div className="divide-y divide-[var(--line)]">
              {items.map((item) => (
                <div key={`${item.source}-${item.modelSlug}`} className="grid gap-4 px-5 py-5 text-sm md:grid-cols-[80px_1fr_1fr_160px] md:items-center">
                  <div className="text-3xl font-semibold tracking-tight">#{item.rankPosition}</div>
                  <div>
                    <div className="font-semibold">{item.modelName}</div>
                    <div className="mt-1 text-[var(--text-secondary)]">{item.modelSlug}</div>
                  </div>
                  <div>
                    <div className="text-[var(--text-secondary)]">来源</div>
                    <div className="mt-1 font-medium">{item.source}</div>
                  </div>
                  <div>
                    <div className="text-[var(--text-secondary)]">能力分</div>
                    <div className="mt-1 text-xl font-semibold">{item.capabilityScore}</div>
                  </div>
                  <div className="text-[var(--text-secondary)] md:col-start-2 md:col-span-3">快照：{formatDateTime(item.snapshotAt)}</div>
                </div>
              ))}
            </div>
          ) : (
            <div className="p-6 text-sm text-[var(--text-secondary)]">暂无能力榜数据。</div>
          )}
        </section>
      </section>
    </main>
  );
}
