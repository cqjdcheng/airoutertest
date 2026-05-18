import { PublicHeader } from "@/app/components/PublicHeader";
import { TestsPageClient, type TestRecord } from "@/app/tests/TestsPageClient";
import { getJson, type PublicEnvelope } from "@/lib/api";

export const dynamic = "force-dynamic";

type SearchParams = Promise<{ page?: string; detail?: string }>;

type PagedResult<T> = {
  items: T[];
  page: number;
  pageSize: number;
  total: number;
};

type ModelItem = {
  modelSlug: string;
  modelName: string;
  requestName?: string;
  apiType?: "openai" | "anthropic";
};

export default async function TestsPage({ searchParams }: { searchParams: SearchParams }) {
  const params = await searchParams;
  const page = Math.max(Number(params.page ?? "1") || 1, 1);
  const pageSize = 20;
  const response = await getJson<PublicEnvelope<PagedResult<TestRecord>>>(`/api/v1/public/tests/latest?page=${page}&pageSize=${pageSize}`);
  const modelsResponse = await getJson<PublicEnvelope<PagedResult<ModelItem>>>("/api/v1/public/models?page=1&pageSize=5");
  const payload = response?.data;
  const modelOptions = (modelsResponse?.data.items ?? [])
    .filter((model) => model.modelSlug && model.modelName)
    .map((model, index) => ({
      label: model.modelName,
      slug: model.modelSlug,
      requestName: model.requestName || model.modelSlug,
      apiType: model.apiType ?? "openai",
      badge: index === 0 ? "HOT" : undefined
    }));
  const maxPages = Math.max(1, Math.ceil(Math.min(payload?.total ?? 0, 200) / pageSize));

  return (
    <main className="public-shell">
      <PublicHeader />

      <section className="public-container public-main">
        <TestsPageClient payload={payload} page={page} maxPages={maxPages} initialDetailId={params.detail} modelOptions={modelOptions} />
      </section>
    </main>
  );
}
