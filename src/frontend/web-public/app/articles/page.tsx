import Link from "next/link";
import { BackLink, PublicHeader } from "@/app/components/PublicHeader";
import { getJson, type PublicEnvelope } from "@/lib/api";
import { formatDateTime } from "@/lib/format";

type ArticleTag = {
  id: number;
  slug: string;
  name: string;
};

type ArticleListItem = {
  slug: string;
  title: string;
  summary?: string;
  publishedAt?: string;
  tags: ArticleTag[];
};

type PagedResult<T> = {
  items: T[];
};

export default async function ArticlesPage({ searchParams }: { searchParams: Promise<{ tag?: string }> }) {
  const { tag } = await searchParams;
  const query = tag ? `&tag=${encodeURIComponent(tag)}` : "";
  const [response, tagResponse] = await Promise.all([
    getJson<PublicEnvelope<PagedResult<ArticleListItem>>>(`/api/v1/public/articles?page=1&pageSize=20${query}`),
    getJson<PublicEnvelope<ArticleTag[]>>("/api/v1/public/article-tags")
  ]);
  const items = response?.data.items ?? [];
  const tags = tagResponse?.data ?? [];

  return (
    <main className="min-h-screen">
      <PublicHeader />
      <section className="public-container py-10 lg:py-14">
        <BackLink />
        <div className="mt-6 glass-card p-6 sm:p-8">
          <p className="eyebrow">Articles</p>
          <h1 className="page-title mt-4">文章与使用指南</h1>
          <p className="body-lead mt-5 max-w-3xl">
            查看 CheapAI 的平台测试、稳定性判断、风险证据和使用说明。
          </p>
        </div>

        {tags.length > 0 ? (
          <div className="mt-6 flex flex-wrap gap-2">
            <Link className="secondary-button" data-active={!tag} href="/articles">全部</Link>
            {tags.map((item) => (
              <Link className="secondary-button" data-active={tag === item.slug} href={`/articles?tag=${item.slug}`} key={item.id}>
                {item.name}
              </Link>
            ))}
          </div>
        ) : null}

        <section className="mt-8 grid gap-4">
          {items.length > 0 ? (
            items.map((item) => (
              <Link key={item.slug} href={`/articles/${item.slug}`} className="panel-card block p-6 transition hover:-translate-y-0.5 hover:border-[rgba(0,113,227,0.32)]">
                <div className="flex flex-wrap items-start justify-between gap-3">
                  <h2 className="text-xl font-semibold tracking-tight">{item.title}</h2>
                  <span className="text-sm text-[var(--text-secondary)]">{formatDateTime(item.publishedAt)}</span>
                </div>
                <div className="mt-3 flex flex-wrap gap-2">
                  {item.tags?.map((articleTag) => <span className="pill" key={articleTag.id}>{articleTag.name}</span>)}
                </div>
                <p className="mt-3 text-sm leading-7 text-[var(--text-secondary)]">{item.summary}</p>
              </Link>
            ))
          ) : (
            <div className="panel-card p-6 text-sm text-[var(--text-secondary)]">暂无文章。</div>
          )}
        </section>
      </section>
    </main>
  );
}
