"use client";

import { FormEvent, useState } from "react";
import { BackLink, PublicHeader } from "@/app/components/PublicHeader";
import { postJson, type PublicEnvelope } from "@/lib/api";

export default function SubmissionPage() {
  const [submittedId, setSubmittedId] = useState<number | null>(null);
  const [error, setError] = useState("");

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    setError("");

    const response = await postJson<PublicEnvelope<{ id: number }>>("/api/v1/public/submissions", {
      siteName: form.get("siteName"),
      siteUrl: form.get("siteUrl"),
      contact: form.get("contact"),
      description: form.get("description")
    });

    setSubmittedId(response?.data.id ?? null);
    setError(response?.data.id ? "" : "提交失败，请检查必填信息。");
  }

  return (
    <main className="min-h-screen">
      <PublicHeader />
      <section className="public-container py-10 lg:py-14">
        <BackLink />
        <div className="mt-6 grid gap-6 lg:grid-cols-[0.94fr_0.56fr]">
          <div className="glass-card p-6 sm:p-8">
            <p className="eyebrow">Site Submission</p>
            <h1 className="page-title mt-4">提交中转站</h1>
            <p className="body-lead mt-5">
              提交后进入后台审核，不会自动公开上架。我们会优先收录可稳定测试、资料清晰的站点。
            </p>

            <form onSubmit={submit} className="mt-8 grid gap-4">
              <input name="siteName" required placeholder="站点名称" className="form-input" />
              <input name="siteUrl" required placeholder="站点地址" className="form-input" />
              <input name="contact" placeholder="联系方式，可选" className="form-input" />
              <textarea name="description" placeholder="补充说明：支持模型、充值比例、是否开票等" className="form-input min-h-32 resize-y" />
              <button className="primary-button w-full">提交审核</button>
            </form>

            {error ? <div className="mt-5 rounded-2xl bg-red-50 p-4 text-sm text-[var(--danger)]">{error}</div> : null}
            {submittedId ? (
              <div className="mt-5 rounded-2xl bg-green-50 p-4 text-sm text-[var(--success)]">
                已提交，编号：{submittedId}
              </div>
            ) : null}
          </div>

          <aside className="panel-card p-6">
            <h2 className="text-xl font-semibold tracking-tight">审核规则</h2>
            <div className="mt-5 space-y-4 text-sm leading-7 text-[var(--text-secondary)]">
              <p>公开提交只进入待审核队列，不直接影响榜单。</p>
              <p>站点上线前需要补齐基础资料、文档入口和平台测试账号。</p>
              <p>存在模型偷换、假响应或稳定性异常风险的站点会被标记。</p>
            </div>
          </aside>
        </div>
      </section>
    </main>
  );
}
