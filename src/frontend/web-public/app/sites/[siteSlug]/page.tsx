import Link from "next/link";
import { BackLink, PublicHeader } from "@/app/components/PublicHeader";
import { SiteStatusTimeline, type SiteStatus24h } from "@/app/components/SiteStatus24h";
import { getJson, type PublicEnvelope } from "@/lib/api";
import { formatDateTime, money, score, trustScore } from "@/lib/format";
import { SiteDetailDataTabs } from "./SiteDetailDataTabs";

export const dynamic = "force-dynamic";

export type SiteDetailResponse = {
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
    id: number;
    publicId: string;
    modelSlug: string;
    modelName: string;
    testType: string;
    status: string;
    firstTokenMs?: number;
    fullResponseMs?: number;
    riskScore?: number;
    riskLevel: string;
    matchScore?: number;
    errorMessage?: string | null;
    testedAt?: string | null;
  }>;
  status24h: SiteStatus24h;
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

  const supportedModels = payload?.supportedModels ?? [];
  const pricing = payload?.pricing ?? [];
  const latestTests = payload?.latestTests ?? [];
  const status24h = payload?.status24h;
  const validInputPrices = pricing
    .map((item) => item.effectiveInputPriceUsd)
    .filter((value): value is number => typeof value === "number" && Number.isFinite(value) && value >= 0);
  const validOutputPrices = pricing
    .map((item) => item.effectiveOutputPriceUsd)
    .filter((value): value is number => typeof value === "number" && Number.isFinite(value) && value >= 0);
  const minInputPrice = validInputPrices.length ? Math.min(...validInputPrices) : null;
  const minOutputPrice = validOutputPrices.length ? Math.min(...validOutputPrices) : null;
  const latestTest = latestTests[0];
  const validPriceTrend = (payload?.trends.price ?? []).filter((point) => Number.isFinite(point.value) && point.value >= 0);
  const validStabilityTrend = (payload?.trends.stability ?? []).filter((point) => Number.isFinite(point.value) && point.value >= 0);

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
            <div className="text-sm text-[var(--text-secondary)]">站点分数</div>
            <div className="mt-3 flex items-center justify-between gap-3">
              <div className="text-3xl font-semibold tracking-tight">{score(trustScore(null, payload?.riskSummary.maxRiskScore), 0)}</div>
              <span className="status-pill" data-tone="neutral">
                分数越高越可信
              </span>
            </div>
            <div className="mt-6 grid grid-cols-3 gap-3 text-center text-sm">
              <div className="rounded-2xl bg-[var(--surface-muted)] p-3">{payload?.site.supportsInvoice ? "支持开票" : "不开票"}</div>
              <div className="rounded-2xl bg-[var(--surface-muted)] p-3">{payload?.site.supportsRefund ? "支持退款" : "无退款"}</div>
              <div className="rounded-2xl bg-[var(--surface-muted)] p-3">{payload?.site.hasDocs ? "有文档" : "无文档"}</div>
            </div>
          </aside>
        </div>

        <section className="mt-6 grid gap-4 md:grid-cols-2 xl:grid-cols-4">
          <SummaryCard title="支持模型" value={`${supportedModels.length} 个`} note={supportedModels.slice(0, 3).map((item) => item.modelName).join(" / ") || "暂无模型数据"} />
          <SummaryCard title="价格摘要" value={minInputPrice === null ? "暂无价格" : `${money(minInputPrice)} 起`} note={`输出最低 ${minOutputPrice === null ? "-" : money(minOutputPrice)}，共 ${pricing.length} 条价格`} />
          <SummaryCard title="最近测试摘要" value={latestTest ? statusLabel(latestTest.status) : "暂无测试"} note={latestTest ? `${latestTest.modelName} · ${formatDateTime(latestTest.testedAt)}` : "还没有平台测试记录"} />
          <SummaryCard title="站点能力" value={payload?.site.status === "active" ? "已收录" : payload?.site.status ?? "未知"} note={`${payload?.site.supportsInvoice ? "可开票" : "不开票"} · ${payload?.site.supportsRefund ? "可退款" : "不支持退款"} · ${payload?.site.hasDocs ? "有文档" : "无文档"}`} />
        </section>

        <section className="mt-6">
          <SiteStatusTimeline status24h={status24h} />
        </section>

        <section className="mt-6 grid gap-6 lg:grid-cols-[1fr_0.95fr]">
          <div className="panel-card p-6">
            <div className="flex flex-wrap items-start justify-between gap-3">
              <div>
                <h2 className="text-xl font-semibold tracking-tight">重点模型覆盖</h2>
                <p className="mt-2 text-sm leading-7 text-[var(--text-secondary)]">先展示少量代表模型，完整模型清单放在页面底部明细区，避免前半屏被长列表占满。</p>
              </div>
              <Link href="#site-detail-data" className="text-button">
                查看全部
              </Link>
            </div>
            <div className="mt-5 flex flex-wrap gap-3">
              {supportedModels.length ? (
                supportedModels.slice(0, 10).map((item) => (
                  <Link key={item.modelSlug} href={`/rankings?model=${encodeURIComponent(item.modelSlug)}`} className="secondary-button">
                    {item.modelName}
                  </Link>
                ))
              ) : (
                <span className="text-sm text-[var(--text-secondary)]">暂无模型数据</span>
              )}
            </div>
          </div>

          <div className="panel-card p-6">
            <h2 className="text-xl font-semibold tracking-tight">趋势摘要</h2>
            <p className="mt-2 text-sm leading-7 text-[var(--text-secondary)]">趋势用于观察价格和稳定性是否持续波动。异常价格趋势会被隐藏，避免用无效负值影响判断。</p>
            <div className="mt-5 space-y-4">
              <TrendBars title="价格趋势" points={validPriceTrend} suffix="" />
              <TrendBars title="稳定性趋势" points={validStabilityTrend} suffix="%" />
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
            <h2 className="text-xl font-semibold tracking-tight">分数说明</h2>
            <p className="mt-3 text-sm leading-7 text-[var(--text-secondary)]">
              分数综合模型替换、响应异常、价格虚标、稳定性波动等证据，分数越高表示当前站点越可信。
            </p>
          </div>
          <div className="panel-card p-6">
            <h2 className="text-xl font-semibold tracking-tight">采购建议</h2>
            <p className="mt-3 text-sm leading-7 text-[var(--text-secondary)]">
              个人开发者优先看价格和可用性；企业用户应同时确认开票、退款、文档和历史稳定性，首次使用建议小额验证。
            </p>
          </div>
        </section>

        <section id="site-detail-data" className="mt-8">
          <SiteDetailDataTabs supportedModels={supportedModels} pricing={pricing} latestTests={latestTests} />
        </section>
      </section>
    </main>
  );
}

function SummaryCard({ title, value, note }: { title: string; value: string; note: string }) {
  return (
    <div className="metric-card">
      <span>{title}</span>
      <strong>{value}</strong>
      <small>{note}</small>
    </div>
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
        <div className="mt-2 rounded-2xl bg-[var(--surface-muted)] p-3 text-sm text-[var(--text-secondary)]">暂无有效趋势数据</div>
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

function statusLabel(status: string) {
  if (status === "success" || status === "succeeded") return "请求完成";
  if (status === "failed" || status === "error") return "请求失败";
  return status || "未知";
}
