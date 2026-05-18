import { redirect } from "next/navigation";

export const dynamic = "force-dynamic";

export default async function LegacyModelRankingPage({ params }: { params: Promise<{ modelSlug: string }> }) {
  const { modelSlug } = await params;
  redirect(`/rankings?model=${encodeURIComponent(modelSlug)}`);
}
