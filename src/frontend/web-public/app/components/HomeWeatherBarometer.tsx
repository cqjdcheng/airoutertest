import type { CSSProperties } from "react";
import { formatDateTime } from "@/lib/format";

type HomeWeather = {
  windowHours: number;
  weatherCode: string;
  weatherLabel: string;
  summary: string;
  successRate: number;
  totalTests: number;
  successCount: number;
  failedCount: number;
  activeSiteCount: number;
  degradedSiteCount: number;
  highRiskSiteCount: number;
  activeModelCount: number;
  averageFirstTokenMs?: number | null;
  lastTestedAt?: string | null;
  highlights: string[];
};

export function HomeWeatherBarometer({ weather }: { weather?: HomeWeather | null }) {
  const resolved = weather ?? {
    windowHours: 24,
    weatherCode: "unknown",
    weatherLabel: "待观察",
    summary: "最近 24 小时暂无公开平台测试。",
    successRate: 0,
    totalTests: 0,
    successCount: 0,
    failedCount: 0,
    activeSiteCount: 0,
    degradedSiteCount: 0,
    highRiskSiteCount: 0,
    activeModelCount: 0,
    averageFirstTokenMs: null,
    lastTestedAt: null,
    highlights: ["当前还没有足够的平台测试样本。"]
  };

  const tone = weatherTone(resolved.weatherCode);
  const score = Math.max(0, Math.min(100, resolved.successRate));
  const highlights = resolved.highlights.length ? resolved.highlights.slice(0, 4) : ["当前还没有足够的平台测试样本。"];
  const gaugeStyle = { "--weather-score": `${score}%` } as CSSProperties;

  return (
    <section className="weather-board" data-weather={resolved.weatherCode}>
      <div className="weather-board__hero">
        <div className="weather-board__copy">
          <p className="eyebrow">24H Barometer</p>
          <h2 className="section-title">AI 最近 24 小时晴雨表</h2>
          <p className="section-copy">{resolved.summary}</p>
          <div className="weather-board__meta">
            <span className="status-pill" data-tone={tone}>
              {resolved.weatherLabel}
            </span>
            <span className="pill">最近更新 {formatDateTime(resolved.lastTestedAt)}</span>
            <span className="pill">{resolved.windowHours} 小时窗口</span>
          </div>
        </div>

        <div className="weather-gauge" style={gaugeStyle}>
          <strong>{score.toFixed(1)}%</strong>
          <span>平台测试成功率</span>
          <small>{resolved.totalTests.toLocaleString("zh-CN")} 条样本</small>
        </div>
      </div>

      <div className="weather-board__metrics">
        <WeatherMetric label="活跃站点" value={resolved.activeSiteCount} note={`覆盖 ${resolved.activeModelCount} 个模型`} />
        <WeatherMetric label="异常站点" value={resolved.degradedSiteCount} note={`高风险 ${resolved.highRiskSiteCount} 个`} />
        <WeatherMetric label="成功 / 失败" value={`${resolved.successCount} / ${resolved.failedCount}`} note="仅统计平台公开测试" />
        <WeatherMetric
          label="首 Token 均值"
          value={resolved.averageFirstTokenMs ? `${resolved.averageFirstTokenMs}ms` : "-"}
          note="成功样本平均速度"
        />
      </div>

      <div className="weather-board__highlights">
        {highlights.map((item, index) => (
          <article className="weather-board__highlight" key={`${resolved.weatherCode}-${index}`}>
            <span>{`0${index + 1}`}</span>
            <p>{item}</p>
          </article>
        ))}
      </div>
    </section>
  );
}

function WeatherMetric({ label, value, note }: { label: string; value: number | string; note: string }) {
  return (
    <div className="weather-metric">
      <span>{label}</span>
      <strong>{typeof value === "number" ? value.toLocaleString("zh-CN") : value}</strong>
      <small>{note}</small>
    </div>
  );
}

function weatherTone(weatherCode: string) {
  if (weatherCode === "sunny") return "success";
  if (weatherCode === "cloudy" || weatherCode === "rainy") return "warning";
  if (weatherCode === "stormy") return "danger";
  return "neutral";
}
