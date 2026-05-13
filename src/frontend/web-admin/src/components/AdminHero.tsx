import { ReactNode } from "react";

type AdminHeroProps = {
  eyebrow: string;
  title: string;
  subtitle: string;
  actions?: ReactNode;
};

export default function AdminHero({ eyebrow, title, subtitle, actions }: AdminHeroProps) {
  return (
    <section className="cheapai-admin-hero">
      <div className="cheapai-admin-eyebrow">{eyebrow}</div>
      <h1 className="cheapai-admin-title">{title}</h1>
      <p className="cheapai-admin-subtitle">{subtitle}</p>
      {actions ? <div className="cheapai-admin-actions">{actions}</div> : null}
    </section>
  );
}

export function AdminMetrics({
  items
}: {
  items: Array<{ label: string; value: string | number; note?: string }>;
}) {
  return (
    <section className="cheapai-admin-metrics">
      {items.map((item) => (
        <div className="cheapai-admin-metric" key={item.label}>
          <div className="cheapai-admin-metric-label">{item.label}</div>
          <div className="cheapai-admin-metric-value">{item.value}</div>
          {item.note ? <div className="cheapai-admin-metric-note">{item.note}</div> : null}
        </div>
      ))}
    </section>
  );
}
