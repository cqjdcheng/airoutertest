import Link from "next/link";
import { BackLink, PublicHeader } from "@/app/components/PublicHeader";
import { getJson, type PublicEnvelope } from "@/lib/api";
import { money, percent, riskLabel, riskTone, score } from "@/lib/format";

export const dynamic = "force-dynamic";

type SiteDetailResponse = {
  site: {
    slug: string;
    name: string;
    baseUrl: string;
    websiteUrl?: string | null;
    description?: string | null;
    supportsRefund: boolean;
    supportsInvoice: boolean;
    hasDocs: boolean;
    docsUrl?: string | null;
    status: string;
    inviteUrl?: string | null;
    recentReview?: string | null;
  };
  supportedModels: Array<{
    modelSlug: string;
    modelName: string;
  }>;
  pricing: Array<{
    modelSlug: string;
    modelName: string;
    effectiveInputPriceUsd?: number;
    effectiveOutputPriceUsd?: number;
  }>;
  latestTests: Array<{
    modelSlug: string;
    modelName: string;
    availability24h?: number;
    stability7d?: number;
    riskScore?: number;
  }>;
  riskSummary: {
    maxRiskScore?: number;
    riskLevel: string;
  };
  trends: {
    price: Array<{ label: string; value: number }>;
    stability: Array<{ label: string; value: number }>;
  };
};

export default async function SiteDetailPage({ params }: { params: Promise<{ siteSlug: string }> }) {
  const { siteSlug } = await params;
  const response = await getJson<PublicEnvelope<SiteDetailResponse>>(`/api/v1/public/sites/${siteSlug}`);
  const payload = response?.data;
  const firstModel = payload?.supportedModels[0]?.modelSlug ?? "gpt-5.5";

  return (
    <main className="min-h-screen">
      <PublicHeader featuredModel={firstModel} />
      <section className="public-container py-10 lg:py-14">
        <BackLink />

        <div className="mt-6 grid gap-6 lg:grid-cols-[1fr_360px]">
          <div className="glass-card p-6 sm:p-8">
            <p className="eyebrow">Relay Site</p>
            <h1 className="page-title mt-4">{payload?.site.name ?? siteSlug}</h1>
            <p className="body-lead mt-5 max-w-3xl">
              {payload?.site.description ?? "该站点已收录，等待更多测试与风险证据。"}
            </p>
            <div className="mt-7 flex flex-wrap gap-3">
              {payload?.site.websiteUrl ? (
                <a className="primary-button" href={payload.site.websiteUrl}>
                  访问官网
                </a>
              ) : null}
              {payload?.site.inviteUrl ? (
                <a className="secondary-button" href={payload.site.inviteUrl}>
                  邀请链接
                </a>
              ) : null}
              {payload?.site.docsUrl ? (
                <a className="secondary-button" href={payload.site.docsUrl}>
                  查看文档
                </a>
              ) : null}
            </div>
          </div>

          <aside className="panel-card p-6">
            <div className="text-sm text-[var(--text-secondary)]">风险状态</div>
            <div className="mt-3 flex items-center justify-between gap-3">
              <div className="text-3xl font-semibold tracking-tight">{riskLabel(payload?.riskSummary.riskLevel)}</div>
              <span className="status-pill" data-tone={riskTone(payload?.riskSummary.riskLevel)}>
                {payload?.riskSummary.maxRiskScore ?? 0} 分
              </span>
            </div>
            <div className="mt-6 grid grid-cols-3 gap-3 text-center text-sm">
              <div className="rounded-2xl bg-[var(--surface-muted)] p-3">{payload?.site.supportsInvoice ? "支持开票" : "不开票"}</div>
              <div className="rounded-2xl bg-[var(--surface-muted)] p-3">{payload?.site.supportsRefund ? "支持退款" : "无退款"}</div>
              <div className="rounded-2xl bg-[var(--surface-muted)] p-3">{payload?.site.hasDocs ? "有文档" : "无文档"}</div>
            </div>
          </aside>
        </div>

        <section className="mt-6 grid gap-6 lg:grid-cols-2">
          <div className="panel-card p-6">
            <div className="flex items-center justify-between gap-3">
              <h2 className="text-xl font-semibold tracking-tight">支持模型</h2>
              <span className="text-sm text-[var(--text-secondary)]">{payload?.supportedModels.length ?? 0} 个</span>
            </div>
            <div className="mt-5 flex flex-wrap gap-3">
              {payload?.supportedModels.length ? (
                payload.supportedModels.map((item) => (
                  <Link key={item.modelSlug} href={`/rankings/${item.modelSlug}`} className="secondary-button">
                    {item.modelName}
                  </Link>
                ))
              ) : (
                <span className="text-sm text-[var(--text-secondary)]">暂无模型数据</span>
              )}
            </div>
          </div>

          <div className="panel-card overflow-hidden">
            <div className="border-b border-[var(--line)] px-6 py-5">
              <h2 className="text-xl font-semibold tracking-tight">价格摘要</h2>
              <p className="mt-1 text-sm text-[var(--text-secondary)]">展示输入 / 输出实际折算价。</p>
            </div>
            <div className="divide-y divide-[var(--line)]">
              {payload?.pricing.length ? (
                payload.pricing.map((item) => (
                  <div key={`${item.modelSlug}-${item.effectiveInputPriceUsd}-${item.effectiveOutputPriceUsd}`} className="flex items-center justify-between gap-4 px-6 py-4 text-sm">
                    <span className="font-medium">{item.modelName}</span>
                    <span className="font-semibold">
                      {money(item.effectiveInputPriceUsd)} / {money(item.effectiveOutputPriceUsd)}
                    </span>
                  </div>
                ))
              ) : (
                <div className="p-6 text-sm text-[var(--text-secondary)]">暂无价格数据</div>
              )}
            </div>
          </div>
        </section>

        <section className="mt-6 grid gap-6 lg:grid-cols-[1fr_0.9fr]">
          <div className="panel-card overflow-hidden">
            <div className="border-b border-[var(--line)] px-6 py-5">
              <h2 className="text-xl font-semibold tracking-tight">最近测试摘要</h2>
              <p className="mt-1 text-sm text-[var(--text-secondary)]">公共展示只使用平台定时测试数据。</p>
            </div>
            <div className="divide-y divide-[var(--line)]">
              {payload?.latestTests.length ? (
                payload.latestTests.map((item) => (
                  <div key={`${item.modelSlug}-${item.riskScore}`} className="grid gap-4 px-6 py-4 text-sm sm:grid-cols-4">
                    <div className="font-semibold">{item.modelName}</div>
                    <div>
                      <span className="text-[var(--text-tertiary)]">24h </span>
                      {percent(item.availability24h)}
                    </div>
                    <div>
                      <span className="text-[var(--text-tertiary)]">7d </span>
                      {percent(item.stability7d)}
                    </div>
                    <div>
                      <span className="text-[var(--text-tertiary)]">风险 </span>
                      {score(item.riskScore)}
                    </div>
                  </div>
                ))
              ) : (
                <div className="p-6 text-sm text-[var(--text-secondary)]">暂无平台测试数据。</div>
              )}
            </div>
          </div>

          <div className="panel-card p-6">
            <h2 className="text-xl font-semibold tracking-tight">趋势摘要</h2>
            <p className="mt-2 text-sm leading-7 text-[var(--text-secondary)]">趋势用于观察价格和稳定性是否持续波动，企业采购应优先选择波动较小的站点。</p>
            <div className="mt-5 space-y-4">
              <TrendBars title="价格趋势" points={payload?.trends.price ?? []} suffix="" />
              <TrendBars title="稳定性趋势" points={payload?.trends.stability ?? []} suffix="%" />
            </div>
          </div>
        </section>

        <section className="mt-6 grid gap-6 lg:grid-cols-3">
          <div className="panel-card p-6">
            <h2 className="text-xl font-semibold tracking-tight">近期体验</h2>
            <p className="mt-3 text-sm leading-7 text-[var(--text-secondary)]">
              {payload?.site.recentReview ?? "暂无人工复核记录。后续测试记录、风险证据和稳定性趋势会在此汇总。"}
            </p>
          </div>
          <div className="panel-card p-6">
            <h2 className="text-xl font-semibold tracking-tight">风险解释</h2>
            <p className="mt-3 text-sm leading-7 text-[var(--text-secondary)]">
              风险摘要来自模型偷换、响应异常、价格虚标、稳定性波动等证据。前台展示摘要，后台保留证据明细用于复核。
            </p>
          </div>
          <div className="panel-card p-6">
            <h2 className="text-xl font-semibold tracking-tight">采购建议</h2>
            <p className="mt-3 text-sm leading-7 text-[var(--text-secondary)]">
              个人开发者优先看价格和可用性；企业用户应同时确认开票、退款、文档和历史稳定性。
            </p>
          </div>
        </section>
      </section>
    </main>
  );
}

function TrendBars({
  title,
  points,
  suffix
}: {
  title: string;
  points: Array<{ label: string; value: number }>;
  suffix: string;
}) {
  if (points.length === 0) {
    return (
      <div>
        <div className="text-sm font-semibold">{title}</div>
        <div className="mt-2 rounded-2xl bg-[var(--surface-muted)] p-3 text-sm text-[var(--text-secondary)]">暂无趋势数据</div>
      </div>
    );
  }

  const maxValue = Math.max(...points.map((point) => point.value), 1);

  return (
    <div>
      <div className="text-sm font-semibold">{title}</div>
      <div className="mt-3 space-y-2">
        {points.slice(-4).map((point) => (
          <div key={`${title}-${point.label}`} className="grid grid-cols-[80px_1fr_70px] items-center gap-3 text-xs text-[var(--text-secondary)]">
            <span>{point.label}</span>
            <span className="h-2 overflow-hidden rounded-full bg-[var(--surface-muted)]">
              <span className="block h-full rounded-full bg-[var(--brand)]" style={{ width: `${Math.max(8, (point.value / maxValue) * 100)}%` }} />
            </span>
            <span className="text-right font-semibold text-[var(--text-primary)]">
              {point.value}
              {suffix}
            </span>
          </div>
        ))}
      </div>
    </div>
  );
}
