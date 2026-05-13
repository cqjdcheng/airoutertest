import Link from "next/link";
import { HomeCheapestTabs, type CheapestRankingGroup } from "@/app/components/HomeCheapestTabs";
import { HomeTestWorkbench } from "@/app/components/HomeTestWorkbench";
import { PublicHeader } from "@/app/components/PublicHeader";
import { getJson, type PublicEnvelope } from "@/lib/api";

export const dynamic = "force-dynamic";

type HomeOverview = {
  featuredModel?: string | null;
  stats: {
    siteCount: number;
    modelCount: number;
    latestTestAt?: string | null;
  };
};

const popularModelSlugs = ["gpt-5.5", "gpt-5.4", "claude-code-4.7", "claude-code-4.6", "gemini-3.1-pro"];

export default async function HomePage() {
  const [overviewResponse, cheapestResponse] = await Promise.all([
    getJson<PublicEnvelope<HomeOverview>>("/api/v1/public/home/overview"),
    getJson<PublicEnvelope<CheapestRankingGroup[]>>(`/api/v1/public/rankings/cheapest?modelSlugs=${popularModelSlugs.join(",")}&limit=5`)
  ]);

  const overview = overviewResponse?.data;
  const rawCheapestGroups = cheapestResponse?.data ?? [];
  const cheapestGroups = popularModelSlugs.map(
    (modelSlug) => rawCheapestGroups.find((group) => group.modelSlug === modelSlug) ?? { modelSlug, items: [] }
  );
  const featuredModel =
    overview?.featuredModel && popularModelSlugs.includes(overview.featuredModel)
      ? overview.featuredModel
      : "gpt-5.5";

  return (
    <main className="min-h-screen">
      <PublicHeader featuredModel={featuredModel} />

      <section className="public-container py-10 lg:py-14">
        <HomeTestWorkbench
          stats={{
            siteCount: overview?.stats.siteCount ?? 0,
            modelCount: overview?.stats.modelCount ?? 0,
            latestTestAt: overview?.stats.latestTestAt
          }}
        />

        <section className="mt-8">
          <HomeCheapestTabs groups={cheapestGroups} />
        </section>

        <section className="mt-8 grid gap-5 lg:grid-cols-3">
          <KnowledgeCard
            title="中转站科普"
            description="AI 中转站本质是兼容 OpenAI 协议的 API 转发服务，常见差异包括上游来源、充值比例、模型覆盖、稳定性、退款和发票能力。"
            href="/sites"
            action="查看中转站大全"
          />
          <KnowledgeCard
            title="模型科普"
            description="同一个模型在不同中转站可能存在价格、上游、限速和降级风险差异。模型大全按模型维度聚合最便宜站点和稳定性摘要。"
            href="/models"
            action="查看模型大全"
          />
          <KnowledgeCard
            title="测试模型原理"
            description="平台测试会记录连通性、首 token、完整响应、错误类型和风险证据。自助测试不进入公共排行，公共排行只使用平台定时测试。"
            href="/tests"
            action="查看测试方案"
          />
        </section>
      </section>
    </main>
  );
}

function KnowledgeCard({
  title,
  description,
  href,
  action
}: {
  title: string;
  description: string;
  href: string;
  action: string;
}) {
  return (
    <article className="panel-card p-6">
      <h2 className="text-2xl font-semibold tracking-tight">{title}</h2>
      <p className="mt-4 text-sm leading-7 text-[var(--text-secondary)]">{description}</p>
      <Link href={href} className="text-button mt-5">
        {action}
      </Link>
    </article>
  );
}
