import Link from "next/link";
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
};

export default async function HomePage() {
  const overviewResponse = await getJson<PublicEnvelope<HomeOverview>>("/api/v1/public/home/overview");

  const overview = overviewResponse?.data;
  const popularModelSlugs = (overview?.popularModels ?? []).map((model) => model.modelSlug).filter(Boolean);
  const featuredModel =
    overview?.featuredModel && popularModelSlugs.includes(overview.featuredModel)
      ? overview.featuredModel
      : popularModelSlugs[0] ?? "gpt-4-1-mini";

  return (
    <main className="public-shell">
      <PublicHeader featuredModel={featuredModel} />

      <section className="public-container public-main">
        <section className="home-hero home-hero--focused">
          <div className="home-hero__content">
            <p className="eyebrow">Relay Intelligence</p>
            <h1 className="display-title mt-4">先看稳定性，再看风险证据</h1>
            <p className="body-lead mt-5 max-w-3xl">
              中转站价格变化很快，低价不一定代表品质。RealLLM 优先做真实连通性、稳定性和风险测试，帮助你在使用前先判断链路是否可靠。
            </p>
            <div className="hero-actions">
              <Link href="/sites" className="primary-button">
                查看中转站
              </Link>
              <Link href="/tests" className="secondary-button">
                查看测试记录
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
        <div id="self-test">
          <HomeTestWorkbench popularModels={overview?.popularModels ?? []} />
        </div>
      </section>
    </main>
  );
}
