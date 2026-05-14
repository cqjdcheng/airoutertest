import Link from "next/link";
import { HomeCheapestTabs, type CheapestRankingGroup } from "@/app/components/HomeCheapestTabs";
import { HomeTestWorkbench } from "@/app/components/HomeTestWorkbench";
import { PublicHeader } from "@/app/components/PublicHeader";
import { getJson, type PublicEnvelope } from "@/lib/api";

export const dynamic = "force-dynamic";

type HomeOverview = {
  featuredModel?: string | null;
  popularModels?: Array<{
    modelSlug: string;
    modelName: string;
  }>;
  stats: {
    siteCount: number;
    modelCount: number;
    testCount: number;
    latestTestAt?: string | null;
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
  const cheapestGroups = resolvedPopularModelSlugs.map(
    (modelSlug) => rawCheapestGroups.find((group) => group.modelSlug === modelSlug) ?? { modelSlug, items: [] }
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
            <h1 className="display-title mt-4">挑选靠谱的中转站</h1>
            <p className="body-lead mt-5 max-w-3xl">
              任何中转站存在跑路风险，为了您的财产安全, 建议先小额试用, 请勿囤积、贪图大额优惠
            </p>
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

        <div id="self-test">
          <HomeTestWorkbench popularModels={overview?.popularModels ?? []} />
        </div>

        <section className="section-heading">
          <div>
            <p className="eyebrow">Market Snapshot</p>
            <h2 className="section-title">主流模型低价排行</h2>
            <p className="section-copy">只保留核心排行入口，详细决策留到站点页和测试页。</p>
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
