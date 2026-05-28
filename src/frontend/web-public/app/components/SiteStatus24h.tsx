import type { CSSProperties } from "react";
import { formatDateTime, percent, score } from "@/lib/format";

type SiteStatusBucket = {
  slotLabel: string;
  slotStartAt: string;
  statusTone: string;
  statusLabel: string;
  hasTest: boolean;
  totalTests?: number;
  successCount?: number;
  successRate?: number;
  testedModelCount?: number;
  averageScore?: number | null;
  testedAt?: string | null;
};

export type SiteStatus24h = {
  windowHours: number;
  successRate: number;
  totalTests: number;
  testedModelCount?: number;
  averageScore?: number | null;
  healthyCount: number;
  warningCount: number;
  criticalCount: number;
  lastTestedAt?: string | null;
  buckets: SiteStatusBucket[];
  dailyBuckets?: SiteStatusBucket[];
};

export function SiteStatusBar({ status24h }: { status24h?: SiteStatus24h | null }) {
  const status = ensureStatus24h(status24h);

  return (
    <div className="site-status-card">
      <div className="site-status-card__header">
        <strong>24 小时</strong>
        <span>{status.averageScore == null ? "-" : `${score(status.averageScore, 0)} 分`}</span>
      </div>
      <div className="site-status-bar" aria-label="中转最近 24 小时状态">
        {status.buckets.map((bucket) => (
          <span
            key={`${bucket.slotLabel}-${bucket.slotStartAt}`}
            className="site-status-bar__segment"
            data-tone={bucket.statusTone}
            style={bucketColorStyle(bucket, "bar")}
            title={buildBucketTitle(bucket)}
          />
        ))}
      </div>
      <div className="site-status-card__meta">
        <span>模型 {status.testedModelCount ?? 0}</span>
        <span>测试 {status.totalTests}</span>
        <span>成功 {percent(status.successRate)}</span>
      </div>
    </div>
  );
}

export function SiteStatusTimeline({ status24h }: { status24h?: SiteStatus24h | null }) {
  const status = ensureStatus24h(status24h);

  return (
    <section className="site-status-panel panel-card">
      <div className="site-status-panel__head">
        <div>
          <p className="eyebrow">Relay Status</p>
          <h2>运行状态</h2>
        </div>
        <div className="site-status-panel__score">
          <strong>{percent(status.successRate)}</strong>
          <span>24H 成功率</span>
          <small>{formatDateTime(status.lastTestedAt)}</small>
        </div>
      </div>

      <div className="site-status-kpis">
        <Metric label="24H 测试" value={status.totalTests.toLocaleString("zh-CN")} />
        <Metric label="测试模型" value={String(status.testedModelCount ?? 0)} />
        <Metric label="平均分" value={status.averageScore == null ? "-" : score(status.averageScore, 1)} />
        <Metric label="成功率" value={percent(status.successRate)} />
      </div>

      <div className="site-status-section">
        <div className="site-status-section__head">
          <strong>最近 24 小时</strong>
          <span>按小时</span>
        </div>
        <div className="site-status-heatmap" aria-label="中转最近 24 小时状态时间线">
          {status.buckets.map((bucket) => (
            <span
              key={`${bucket.slotLabel}-${bucket.slotStartAt}`}
              data-tone={bucket.statusTone}
              style={bucketColorStyle(bucket, "cell")}
              title={buildBucketTitle(bucket)}
            >
              {bucket.slotLabel}
            </span>
          ))}
        </div>
      </div>

      <div className="site-status-section">
        <div className="site-status-section__head">
          <strong>最近 30 天</strong>
          <span>按天</span>
        </div>
        <div className="site-status-daily-grid" aria-label="中转最近 30 天状态">
          {status.dailyBuckets?.map((bucket) => (
            <span
              key={`${bucket.slotLabel}-${bucket.slotStartAt}`}
              data-tone={bucket.statusTone}
              style={bucketColorStyle(bucket, "cell")}
              title={buildBucketTitle(bucket)}
            >
              <small>{bucket.slotLabel}</small>
              <strong>{bucket.hasTest ? score(bucket.averageScore, 0) : "-"}</strong>
            </span>
          ))}
        </div>
      </div>
    </section>
  );
}

function Metric({ label, value }: { label: string; value: string }) {
  return (
    <div className="site-status-kpi">
      <span>{label}</span>
      <strong>{value}</strong>
    </div>
  );
}

function ensureStatus24h(status24h?: SiteStatus24h | null): SiteStatus24h {
  if (status24h) {
    return {
      ...status24h,
      dailyBuckets: status24h.dailyBuckets?.length ? status24h.dailyBuckets : buildEmptyDailyBuckets()
    };
  }

  const now = new Date();
  return {
    windowHours: 24,
    successRate: 0,
    totalTests: 0,
    testedModelCount: 0,
    averageScore: null,
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
        hasTest: false,
        totalTests: 0,
        testedModelCount: 0,
        averageScore: null
      };
    }),
    dailyBuckets: buildEmptyDailyBuckets()
  };
}

function buildEmptyDailyBuckets() {
  const now = new Date();
  return Array.from({ length: 30 }, (_, index) => {
    const date = new Date(now.getTime() - (29 - index) * 24 * 60 * 60 * 1000);
    return {
      slotLabel: `${String(date.getMonth() + 1).padStart(2, "0")}/${String(date.getDate()).padStart(2, "0")}`,
      slotStartAt: date.toISOString(),
      statusTone: "neutral",
      statusLabel: "暂无测试",
      hasTest: false,
      totalTests: 0,
      testedModelCount: 0,
      averageScore: null
    };
  });
}

function buildBucketTitle(bucket: SiteStatusBucket) {
  const parts = [bucket.slotLabel, bucket.statusLabel];
  if (bucket.hasTest && typeof bucket.testedModelCount === "number") {
    parts.push(`${bucket.testedModelCount} 个测试模型`);
  }
  if (typeof bucket.totalTests === "number") {
    parts.push(`${bucket.totalTests} 次测试`);
  }
  if (typeof bucket.averageScore === "number" && bucket.hasTest) {
    parts.push(`平均分 ${score(bucket.averageScore)}`);
  }
  if (bucket.testedAt) {
    parts.push(formatDateTime(bucket.testedAt));
  }
  return parts.join(" · ");
}

function bucketColorStyle(bucket: SiteStatusBucket, variant: "bar" | "cell"): CSSProperties | undefined {
  if (!bucket.hasTest || typeof bucket.averageScore !== "number") {
    return undefined;
  }

  const bounded = Math.max(0, Math.min(100, bucket.averageScore));
  const hue = Math.round((bounded / 100) * 132);
  const saturation = 68;
  const lightness = variant === "bar" ? 42 : 94 - Math.round((bounded / 100) * 12);
  const textLightness = bounded >= 60 ? 24 : 30;

  if (variant === "bar") {
    return {
      backgroundColor: `hsl(${hue} ${saturation}% ${lightness}%)`
    };
  }

  return {
    backgroundColor: `hsl(${hue} ${saturation}% ${lightness}%)`,
    color: `hsl(${hue} ${saturation}% ${textLightness}%)`
  };
}
