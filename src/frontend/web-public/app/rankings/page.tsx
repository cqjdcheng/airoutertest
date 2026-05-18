import { PublicHeader } from "@/app/components/PublicHeader";
import { RankingPageView } from "@/app/rankings/RankingPageView";

export const dynamic = "force-dynamic";

type SearchParams = Promise<{ model?: string }>;

export default async function RankingsPage({ searchParams }: { searchParams: SearchParams }) {
  const params = await searchParams;

  return (
    <main className="public-shell">
      <PublicHeader />

      <section className="public-container public-main">
        <RankingPageView selectedModelSlug={params.model} />
      </section>
    </main>
  );
}
