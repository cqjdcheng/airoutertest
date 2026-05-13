import Link from "next/link";
import { PublicHeader } from "@/app/components/PublicHeader";
import { getJson, type PublicEnvelope } from "@/lib/api";
import { formatDateTime, percent, riskLabel, riskTone, score } from "@/lib/format";

export const dynamic = "force-dynamic";

type SearchParams = Promise<{ page?: string }>;

type TestRecord = {
  id: number;
  siteSlug: string;
  siteName: string;
  modelSlug: string;
  modelName: string;
  testType: string;
  status: string;
  firstTokenMs?: number;
  fullResponseMs?: number;
  errorMessage?: string;
  riskScore: number;
  riskLevel: string;
  testedAt: string;
};

type PagedResult<T> = {
  items: T[];
  page: number;
  pageSize: number;
  total: number;
};

export default async function TestsPage({ searchParams }: { searchParams: SearchParams }) {
  const params = await searchParams;
  const page = Math.max(Number(params.page ?? "1") || 1, 1);
  const pageSize = 20;
  const response = await getJson<PublicEnvelope<PagedResult<TestRecord>>>(`/api/v1/public/tests/latest?page=${page}&pageSize=${pageSize}`);
  const payload = response?.data;
  const items = payload?.items ?? [];
  const maxPages = Math.max(1, Math.ceil(Math.min(payload?.total ?? 0, 200) / pageSize));
  const successRate = items.length ? (items.filter((item) => item.status === "success").length * 100) / items.length : undefined;

  return (
    <main className="min-h-screen">
      <PublicHeader />
      <section className="public-container py-10 lg:py-14">
        <div className="grid gap-6 lg:grid-cols-[1fr_360px]">
          <div>
            <p className="eyebrow">Testing Protocol</p>
            <h1 className="page-title mt-4">中转测试</h1>
            <p className="body-lead mt-5 max-w-3xl">
              展示平台测试方案和最新中转测试记录。列表最多保留最新 200 条，分页查看；自助测试结果不进入公共排行。
            </p>
          </div>
          <aside className="panel-card p-5">
            <div className="text-sm text-[var(--text-secondary)]">当前页成功率</div>
            <div className="mt-2 text-3xl font-semibold tracking-tight">{percent(successRate)}</div>
            <p className="mt-3 text-sm text-[var(--text-secondary)]">首 token、完整响应和风险分会进入聚合评分。</p>
            <Link href="/self-test" className="primary-button mt-5">
              发起自助测试
            </Link>
          </aside>
        </div>

        <section className="mt-8 grid gap-5 lg:grid-cols-3">
          <PrincipleCard title="响应速度" text="记录首 token 耗时和完整响应耗时。首 token 反映体感速度，完整响应反映吞吐稳定性。" />
          <PrincipleCard title="稳定性" text="按时间窗口聚合成功率和耗时波动。稳定性不是单次成功，而是连续样本的波动足够小。" />
          <PrincipleCard title="造假测试" text="识别缓存假响应、模型偷换、降级冒充、价格虚标和响应伪造，命中后进入风险证据和补测流程。" />
        </section>

        <section className="data-table mt-8">
          <div className="flex flex-wrap items-center justify-between gap-3 border-b border-[var(--line)] px-5 py-4">
            <div>
              <h2 className="text-xl font-semibold tracking-tight">最新测试记录</h2>
              <p className="mt-1 text-sm text-[var(--text-secondary)]">分页展示最新 200 条平台测试样本。</p>
            </div>
            <span className="status-pill" data-tone="neutral">
              第 {page} / {maxPages} 页
            </span>
          </div>
          <div className="overflow-x-auto">
            <table className="recommend-table">
              <thead>
                <tr>
                  <th>站点 / 模型</th>
                  <th>状态</th>
                  <th>速度</th>
                  <th>风险</th>
                  <th>时间</th>
                </tr>
              </thead>
              <tbody>
                {items.length ? (
                  items.map((item) => (
                    <tr key={item.id}>
                      <td>
                        <Link href={`/sites/${item.siteSlug}`}>
                          <strong>{item.siteName}</strong>
                        </Link>
                        <span>{item.modelName}</span>
                      </td>
                      <td>
                        <span className="status-pill" data-tone={item.status === "success" ? "success" : "danger"}>
                          {item.status}
                        </span>
                        <span>{item.testType}</span>
                      </td>
                      <td>
                        首 token {item.firstTokenMs ?? "-"}ms
                        <span>完整 {item.fullResponseMs ?? "-"}ms</span>
                      </td>
                      <td>
                        <span className="status-pill" data-tone={riskTone(item.riskLevel)}>
                          {riskLabel(item.riskLevel)}
                        </span>
                        <span>风险分 {score(item.riskScore)}</span>
                      </td>
                      <td>{formatDateTime(item.testedAt)}</td>
                    </tr>
                  ))
                ) : (
                  <tr>
                    <td colSpan={5}>暂无测试记录。</td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
          <div className="flex justify-between gap-3 px-5 py-4">
            <Link className="secondary-button" aria-disabled={page <= 1} href={`/tests?page=${Math.max(1, page - 1)}`}>
              上一页
            </Link>
            <Link className="secondary-button" aria-disabled={page >= maxPages} href={`/tests?page=${Math.min(maxPages, page + 1)}`}>
              下一页
            </Link>
          </div>
        </section>
      </section>
    </main>
  );
}

function PrincipleCard({ title, text }: { title: string; text: string }) {
  return (
    <article className="panel-card p-6">
      <h2 className="text-xl font-semibold tracking-tight">{title}</h2>
      <p className="mt-3 text-sm leading-7 text-[var(--text-secondary)]">{text}</p>
    </article>
  );
}
