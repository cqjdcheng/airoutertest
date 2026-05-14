"use client";

import { Suspense } from "react";
import { useSearchParams } from "next/navigation";
import { BackLink, PublicHeader } from "@/app/components/PublicHeader";
import { PublicPageHero } from "@/app/components/PublicPageHero";
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
    <main className="public-shell">
      <PublicHeader />

      <section className="public-container public-main">
        <BackLink />
        <PublicPageHero
          eyebrow="Self Test"
          title="中转接口自助测试"
          description="输入接口地址、API Key 和模型名，直接查看一次真实请求的速度、结构、Token 与风险信号。"
          aside={
            <>
              <div className="metric-card">
                <span>结果记录</span>
                <strong>本地缓存</strong>
              </div>
              <div className="metric-card mt-3">
                <span>公共排行</span>
                <strong>不受影响</strong>
              </div>
            </>
          }
        />

        <RelayTestPanel initialModel={initialModel} />
      </section>
    </main>
  );
}

function SelfTestShell() {
  return (
    <main className="public-shell">
      <PublicHeader />

      <section className="public-container public-main">
        <BackLink />
        <PublicPageHero
          eyebrow="Self Test"
          title="中转接口自助测试"
          description="正在加载测试表单..."
          aside={
            <div className="metric-card">
              <span>状态</span>
              <strong>加载中</strong>
            </div>
          }
        />
      </section>
    </main>
  );
}
