import { PageContainer } from "@ant-design/pro-components";
import type { ReactNode } from "react";

type AdminPageProps = {
  title: string;
  subtitle: string;
  extra?: ReactNode;
  metrics?: Array<{ label: string; value: string | number; note?: string }>;
  children: ReactNode;
};

export default function AdminPage({ title, subtitle, extra, metrics, children }: AdminPageProps) {
  return (
    <PageContainer
      className="cheapai-admin-page"
      title={title}
      subTitle={subtitle}
      extra={extra}
    >
      {metrics?.length ? (
        <section className="cheapai-admin-summary-strip">
          {metrics.map((item) => (
            <article className="cheapai-admin-summary-card" key={item.label}>
              <span className="cheapai-admin-summary-label">{item.label}</span>
              <strong className="cheapai-admin-summary-value">{item.value}</strong>
              {item.note ? <span className="cheapai-admin-summary-note">{item.note}</span> : null}
            </article>
          ))}
        </section>
      ) : null}
      {children}
    </PageContainer>
  );
}
