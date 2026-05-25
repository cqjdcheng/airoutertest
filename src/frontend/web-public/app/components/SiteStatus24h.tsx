import { formatDateTime, percent } from "@/lib/format";

export type SiteStatus24h = {
  windowHours: number;
  successRate: number;
  totalTests: number;
  healthyCount: number;
  warningCount: number;
  criticalCount: number;
  lastTestedAt?: string | null;
  buckets: Array<{
    slotLabel: string;
    slotStartAt: string;
    statusTone: string;
    statusLabel: string;
    hasTest: boolean;
    testedAt?: string | null;
    modelName?: string | null;
    testType?: string | null;
    status?: string | null;
    riskScore?: number | null;
  }>;
};

export function SiteStatusBar({ status24h }: { status24h?: SiteStatus24h | null }) {
  const status = ensureStatus24h(status24h);

  return (
    <div className="site-status-card">
      <div className="site-status-card__header">
        <strong>24 小时状态</strong>
        <span>{percent(status.successRate)}</span>
      </div>
      <div className="site-status-bar" aria-label="中转最近 24 小时状态进度条">
        {status.buckets.map((bucket) => (
          <span
            key={`${bucket.slotLabel}-${bucket.slotStartAt}`}
            className="site-status-bar__segment"
            data-tone={bucket.statusTone}
            title={buildBucketTitle(bucket)}
          />
        ))}
      </div>
      <div className="site-status-card__meta">
        <span>红 {status.criticalCount}</span>
        <span>黄 {status.warningCount}</span>
        <span>绿 {status.healthyCount}</span>
      </div>
    </div>
  );
}

export function SiteStatusTimeline({ status24h }: { status24h?: SiteStatus24h | null }) {
  const status = ensureStatus24h(status24h);

  return (
    <section className="site-status-timeline panel-card p-6">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <p className="eyebrow">24H Relay Status</p>
          <h2 className="mt-2 text-xl font-semibold tracking-tight">最近 24 小时状态轨迹</h2>
          <p className="mt-2 text-sm leading-7 text-[var(--text-secondary)]">
            每个格子代表一个小时，红色表示失败或高风险，黄色表示波动，绿色表示正常，灰色表示该小时没有平台公开测试。
          </p>
        </div>
        <div className="site-status-timeline__summary">
          <strong>{percent(status.successRate)}</strong>
          <span>成功率</span>
          <small>最近更新 {formatDateTime(status.lastTestedAt)}</small>
        </div>
      </div>

      <div className="site-status-timeline__legend">
        <span data-tone="success">正常</span>
        <span data-tone="warning">波动</span>
        <span data-tone="danger">异常</span>
        <span data-tone="neutral">无测试</span>
      </div>

      <div className="site-status-grid" aria-label="中转最近 24 小时状态时间线">
        {status.buckets.map((bucket) => (
          <article
            key={`${bucket.slotLabel}-${bucket.slotStartAt}`}
            className="site-status-grid__cell"
            data-tone={bucket.statusTone}
            title={buildBucketTitle(bucket)}
          >
            <span>{bucket.slotLabel}</span>
            <strong>{bucket.statusLabel}</strong>
            <small>{bucket.modelName || (bucket.hasTest ? "平台测试" : "无测试")}</small>
          </article>
        ))}
      </div>
    </section>
  );
}

function ensureStatus24h(status24h?: SiteStatus24h | null): SiteStatus24h {
  if (status24h) {
    return status24h;
  }

  const now = new Date();
  return {
    windowHours: 24,
    successRate: 0,
    totalTests: 0,
    healthyCount: 0,
    warningCount: 0,
    criticalCount: 0,
    lastTestedAt: null,
    buckets: Array.from({ length: 24 }, (_, index) => {
      const slot = new Date(now.getTime() - (23 - index) * 60 * 60 * 1000);
      const label = `${String(slot.getHours()).padStart(2, "0")}:00`;
      return {
        slotLabel: label,
        slotStartAt: slot.toISOString(),
        statusTone: "neutral",
        statusLabel: "暂无测试",
        hasTest: false
      };
    })
  };
}

function buildBucketTitle(bucket: SiteStatus24h["buckets"][number]) {
  const parts = [bucket.slotLabel, bucket.statusLabel];
  if (bucket.modelName) {
    parts.push(bucket.modelName);
  }
  if (bucket.testedAt) {
    parts.push(formatDateTime(bucket.testedAt));
  }
  return parts.join(" · ");
}
