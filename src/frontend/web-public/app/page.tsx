import Link from "next/link";
import { HomeCheapestTabs, type CheapestRankingGroup } from "@/app/components/HomeCheapestTabs";
import { HomeWeatherBarometer } from "@/app/components/HomeWeatherBarometer";
import { HomeTestWorkbench } from "@/app/components/HomeTestWorkbench";
import { PublicHeader } from "@/app/components/PublicHeader";
import { getJson, type PublicEnvelope } from "@/lib/api";

export const dynamic = "force-dynamic";

type HomeOverview = {
  featuredModel?: string | null;
  popularModels?: Array<{
    modelSlug: string;
    modelName: string;
    requestName: string;
    apiType: "openai" | "anthropic";
  }>;
  stats: {
    siteCount: number;
    modelCount: number;
    testCount: number;
    latestTestAt?: string | null;
  };
  weather?: {
    windowHours: number;
    weatherCode: string;
    weatherLabel: string;
    summary: string;
    successRate: number;
    totalTests: number;
    successCount: number;
    failedCount: number;
    activeSiteCount: number;
    degradedSiteCount: number;
    highRiskSiteCount: number;
    activeModelCount: number;
    averageFirstTokenMs?: number | null;
    lastTestedAt?: string | null;
    highlights: string[];
  };
};

export default async function HomePage() {
  const overviewResponse = await getJson<PublicEnvelope<HomeOverview>>("/api/v1/public/home/overview");

  const overview = overviewResponse?.data;
  const popularModelSlugs = (overview?.popularModels ?? []).map((model) => model.modelSlug).filter(Boolean);
  const resolvedPopularModelSlugs = popularModelSlugs.length ? popularModelSlugs : ["gpt-4-1-mini"];
  const cheapestResponse = await getJson<PublicEnvelope<CheapestRankingGroup[]>>(
    `/api/v1/public/rankings/cheapest?modelSlugs=${resolvedPopularModelSlugs.join(",")}&limit=5`
  );
  const rawCheapestGroups = cheapestResponse?.data ?? [];
  const popularModelNameMap = new Map((overview?.popularModels ?? []).map((model) => [model.modelSlug, model.modelName || model.requestName || model.modelSlug]));
  const cheapestGroups = resolvedPopularModelSlugs.map(
    (modelSlug) => ({
      ...(rawCheapestGroups.find((group) => group.modelSlug === modelSlug) ?? { modelSlug, items: [] }),
      modelName: popularModelNameMap.get(modelSlug) ?? modelSlug
    })
  );
  const featuredModel =
    overview?.featuredModel && resolvedPopularModelSlugs.includes(overview.featuredModel)
      ? overview.featuredModel
      : resolvedPopularModelSlugs[0];

  return (
    <main className="public-shell">
      <PublicHeader featuredModel={featuredModel} />

      <section className="public-container public-main">
        <section className="home-hero home-hero--focused">
          <div className="home-hero__content">
            <p className="eyebrow">Relay Intelligence</p>
            <h1 className="display-title mt-4">先看低价，再看分数</h1>
            <p className="body-lead mt-5 max-w-3xl">
              按模型查看当前低价中转，同时保留分数、稳定性和最近测试记录。分数越高表示越可信，任何中转站都建议先小额试用。
            </p>
            <div className="hero-actions">
              <Link href="/rankings" className="primary-button">
                查看价格排行
              </Link>
              <Link href="/sites" className="secondary-button">
                查看中转站
              </Link>
            </div>
          </div>

          <div className="home-hero__aside">
            <div className="hero-stats">
              <div className="metric-card metric-card--primary">
                <span>测试次数</span>
                <strong>{(overview?.stats.testCount ?? 0).toLocaleString("zh-CN")}</strong>
                <small>平台样本与自测记录持续更新</small>
              </div>
              <div className="metric-card">
                <span>收录站点</span>
                <strong>{overview?.stats.siteCount ?? 0}</strong>
              </div>
              <div className="metric-card">
                <span>覆盖模型</span>
                <strong>{overview?.stats.modelCount ?? 0}</strong>
              </div>         
            </div>
          </div>
        </section>
        <HomeWeatherBarometer weather={overview?.weather} />
        <div id="self-test">
          <HomeTestWorkbench popularModels={overview?.popularModels ?? []} />
        </div>
        <section className="section-heading">
          <div>
            <p className="eyebrow">Market Snapshot</p>
            <h2 className="section-title">主流模型低价排行</h2>
            <p className="section-copy">每个模型只展示当前可比价的低价候选，进入站点页查看支持模型、价格和最近测试。</p>
          </div>
          <Link href="/models" className="text-button">
            查看全部模型
          </Link>
        </section>
        <HomeCheapestTabs groups={cheapestGroups} />


      </section>
    </main>
  );
}
