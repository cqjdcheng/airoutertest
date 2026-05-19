import { BackLink, PublicHeader } from "@/app/components/PublicHeader";
import { getJson, type PublicEnvelope } from "@/lib/api";
import { formatDateTime } from "@/lib/format";

type ArticleDetail = {
  slug: string;
  title: string;
  summary?: string;
  contentMd: string;
  publishedAt?: string;
  tags?: Array<{ id: number; name: string; slug: string }>;
};

export default async function ArticleDetailPage({ params }: { params: Promise<{ slug: string }> }) {
  const { slug } = await params;
  const response = await getJson<PublicEnvelope<ArticleDetail>>(`/api/v1/public/articles/${slug}`);
  const article = response?.data;

  return (
    <main className="min-h-screen">
      <PublicHeader />
      <article className="public-container py-10 lg:py-14">
        <BackLink href="/articles" label="返回文章列表" />
        <div className="mt-6 glass-card p-6 sm:p-8">
          <p className="eyebrow">Article</p>
          <h1 className="page-title mt-4">{article?.title ?? slug}</h1>
          {article?.summary ? <p className="body-lead mt-5 max-w-3xl">{article.summary}</p> : null}
          <div className="mt-5 flex flex-wrap gap-2">
            {article?.tags?.map((tag) => <span className="pill" key={tag.id}>{tag.name}</span>)}
          </div>
          {article?.publishedAt ? <p className="mt-5 text-sm text-[var(--text-secondary)]">发布：{formatDateTime(article.publishedAt)}</p> : null}
        </div>
        <div className="panel-card mt-8 whitespace-pre-wrap p-6 text-sm leading-8 text-[var(--text-secondary)] sm:p-8">
          {article?.contentMd ?? "文章不存在或尚未发布。"}
        </div>
      </article>
    </main>
  );
}
