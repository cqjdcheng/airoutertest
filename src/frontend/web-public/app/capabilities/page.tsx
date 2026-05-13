import { BackLink, PublicHeader } from "@/app/components/PublicHeader";
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
    <main className="min-h-screen">
      <PublicHeader />
      <section className="public-container py-10 lg:py-14">
        <BackLink />
        <div className="mt-6 glass-card p-6 sm:p-8">
          <p className="eyebrow">Model Capability</p>
          <h1 className="page-title mt-4">模型能力榜</h1>
          <p className="body-lead mt-5 max-w-3xl">
            能力榜引用第三方公开来源，仅作为价格、稳定性和风险判断之外的参考口径。
          </p>
        </div>

        <section className="data-table mt-8">
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
                  <div className="text-[var(--text-secondary)] md:col-start-2 md:col-span-3">
                    快照：{formatDateTime(item.snapshotAt)}
                  </div>
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
