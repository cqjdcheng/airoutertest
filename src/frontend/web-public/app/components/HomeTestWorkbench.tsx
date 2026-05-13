import { formatDateTime } from "@/lib/format";
import { RelayTestPanel } from "@/app/components/RelayTestPanel";

export function HomeTestWorkbench({
  stats
}: {
  stats: {
    siteCount: number;
    modelCount: number;
    latestTestAt?: string | null;
  };
}) {
  return (
    <section className="home-workbench">
      <div className="home-workbench__header">
        <div>
          <p className="eyebrow">CheapAI Relay Intelligence</p>
          <h1 className="display-title mt-4">挑选靠谱、便宜、少掺假的 AI 中转站</h1>
          <p className="body-lead mt-5 max-w-3xl">
            输入中转站 API 地址、Key 和模型名，先做一次真实协议与模型探针检测，再查看不同模型的低价排行。
          </p>
        </div>
        <div className="metric-strip">
          <MetricItem label="收录站点" value={stats.siteCount} />
          <MetricItem label="覆盖模型" value={stats.modelCount} />
          <MetricItem label="最近测试" value={formatDateTime(stats.latestTestAt)} />
        </div>
      </div>

      <RelayTestPanel compact />
    </section>
  );
}

function MetricItem({ label, value }: { label: string; value: string | number }) {
  return (
    <div>
      <span>{label}</span>
      <strong>{value}</strong>
    </div>
  );
}
