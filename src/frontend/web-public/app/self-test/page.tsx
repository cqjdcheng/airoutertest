"use client";

import { Suspense } from "react";
import { useSearchParams } from "next/navigation";
import { BackLink, PublicHeader } from "@/app/components/PublicHeader";
import { RelayTestPanel } from "@/app/components/RelayTestPanel";

export default function SelfTestPage() {
  return (
    <Suspense fallback={<SelfTestShell />}>
      <SelfTestContent />
    </Suspense>
  );
}

function SelfTestContent() {
  const searchParams = useSearchParams();
  const initialModel = searchParams.get("model") || "gpt-5.5";

  return (
    <main className="min-h-screen">
      <PublicHeader />
      <section className="public-container py-10 lg:py-14">
        <BackLink />
        <div className="mt-6 grid gap-6 lg:grid-cols-[minmax(0,1fr)_320px]">
          <div>
            <div className="glass-card mb-6 p-6 sm:p-8">
              <p className="eyebrow">Self Test</p>
              <h1 className="page-title mt-4">自助测试中转站模型</h1>
              <p className="body-lead mt-5">
                输入 API 地址、API Key 和模型名称，CheapAI 会发起真实 OpenAI 兼容请求，立即返回协议、能力、Token 和时延探针结果。
              </p>
            </div>
            <RelayTestPanel initialModel={initialModel} />
          </div>

          <aside className="panel-card h-fit p-6">
            <h2 className="text-xl font-semibold tracking-tight">检测口径</h2>
            <div className="mt-5 space-y-4 text-sm leading-7 text-[var(--text-secondary)]">
              <p>API Key 仅用于本次请求，不写入数据库；自助测试结果不进入公共排行。</p>
              <p>首期硬探针覆盖 D1、D2、D4、D5、D7、D8、S5。</p>
              <p>身份一致性、上游指纹和 Token 注入作为低置信证据记录，不单次强判造假。</p>
              <p>真实高置信风险结论以后由平台定时测试、补测和人工复核共同确认。</p>
            </div>
          </aside>
        </div>
      </section>
    </main>
  );
}

function SelfTestShell() {
  return (
    <main className="min-h-screen">
      <PublicHeader />
      <section className="public-container py-10 lg:py-14">
        <BackLink />
        <div className="glass-card mt-6 p-6 sm:p-8">
          <p className="eyebrow">Self Test</p>
          <h1 className="page-title mt-4">自助测试中转站模型</h1>
          <p className="body-lead mt-5">正在加载测试表单...</p>
        </div>
      </section>
    </main>
  );
}
