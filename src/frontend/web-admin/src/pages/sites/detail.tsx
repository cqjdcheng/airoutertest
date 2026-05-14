import { useEffect, useMemo, useState } from "react";
import { PageContainer, ProCard } from "@ant-design/pro-components";
import { Alert, Button, Empty } from "antd";
import { history } from "@umijs/max";
import AuthGuard from "@/components/AuthGuard";
import { fetchSite, type RelaySiteDetail } from "@/services/sites";
import SiteDetailContent from "./SiteDetailContent";

function currentSiteId() {
  if (typeof window === "undefined") {
    return 0;
  }

  return Number(window.location.pathname.split("/").filter(Boolean).pop());
}

export default function SiteDetailPage() {
  const [detail, setDetail] = useState<RelaySiteDetail | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");
  const siteId = useMemo(currentSiteId, []);

  async function load() {
    setLoading(true);
    try {
      setDetail(await fetchSite(siteId));
      setError("");
    } catch (requestError) {
      setError((requestError as Error).message);
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void load();
  }, []);

  return (
    <AuthGuard>
      <PageContainer
        title={detail?.name ?? "站点详情"}
        subTitle={detail?.slug}
        extra={[
          <Button key="back" onClick={() => history.push("/sites")}>
            返回列表
          </Button>,
          <Button key="refresh" type="primary" onClick={load}>
            刷新
          </Button>
        ]}
      >
        {error ? <Alert type="warning" showIcon message={error} style={{ marginBottom: 16 }} /> : null}
        <ProCard className="cheapai-admin-card" loading={loading}>
          {detail ? (
            <SiteDetailContent detail={detail} />
          ) : (
            <Empty description="未找到站点" />
          )}
        </ProCard>
      </PageContainer>
    </AuthGuard>
  );
}
