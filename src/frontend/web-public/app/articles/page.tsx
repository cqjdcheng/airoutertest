import Link from "next/link";
import { BackLink, PublicHeader } from "@/app/components/PublicHeader";
import { getJson, type PublicEnvelope } from "@/lib/api";
import { formatDateTime } from "@/lib/format";

type ArticleListItem = {
  slug: string;
  title: string;
  summary?: string;
  publishedAt?: string;
};

type PagedResult<T> = {
  items: T[];
};

export default async function ArticlesPage() {
  const response = await getJson<PublicEnvelope<PagedResult<ArticleListItem>>>("/api/v1/public/articles?page=1&pageSize=20");
  const items = response?.data.items ?? [];

  return (
    <main className="min-h-screen">
      <PublicHeader />
      <section className="public-container py-10 lg:py-14">
        <BackLink />
        <div className="mt-6 glass-card p-6 sm:p-8">
          <p className="eyebrow">Articles</p>
          <h1 className="page-title mt-4">文章与口径说明</h1>
          <p className="body-lead mt-5 max-w-3xl">
            解释 CheapAI 的价格折算、平台测试、风险证据和榜单使用方式，避免误读数据。
          </p>
        </div>

        <section className="mt-8 grid gap-4">
          {items.length > 0 ? (
            items.map((item) => (
              <Link key={item.slug} href={`/articles/${item.slug}`} className="panel-card block p-6 transition hover:-translate-y-0.5 hover:border-[rgba(0,113,227,0.32)]">
                <div className="flex flex-wrap items-start justify-between gap-3">
                  <h2 className="text-xl font-semibold tracking-tight">{item.title}</h2>
                  <span className="text-sm text-[var(--text-secondary)]">{formatDateTime(item.publishedAt)}</span>
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
