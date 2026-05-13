import { useEffect, useMemo, useState } from "react";
import { PageContainer, ProCard } from "@ant-design/pro-components";
import { Alert, Button, Input, message, Space, Table, Tag } from "antd";
import AuthGuard from "@/components/AuthGuard";
import AdminHero, { AdminMetrics } from "@/components/AdminHero";
import { fetchOffers, rebuildRankings, runManualCrawl, verifyOffer, type RelayOfferListItem } from "@/services/operations";

export default function OffersPage() {
  const [items, setItems] = useState<RelayOfferListItem[]>([]);
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);
  const [keyword, setKeyword] = useState("");

  const filteredItems = useMemo(() => {
    const normalizedKeyword = keyword.trim().toLowerCase();
    if (!normalizedKeyword) {
      return items;
    }

    return items.filter((item) =>
      [item.siteName, item.siteSlug, item.modelName, item.modelSlug]
        .some((value) => String(value ?? "").toLowerCase().includes(normalizedKeyword))
    );
  }, [items, keyword]);

  async function load() {
    setLoading(true);
    try {
      const result = await fetchOffers(1, 80);
      setItems(result.items);
      setError("");
    } catch (requestError) {
      setError((requestError as Error).message);
    } finally {
      setLoading(false);
    }
  }

  async function runCrawl() {
    const result = await runManualCrawl();
    message.success(`${result.message}，影响 ${result.affectedCount} 条`);
    await load();
  }

  async function runAggregate() {
    const result = await rebuildRankings();
    message.success(`${result.message}，影响 ${result.affectedCount} 条`);
  }

  async function reviewOffer(id: number) {
    await verifyOffer(id);
    message.success("报价已标记为已复核");
    await load();
  }

  useEffect(() => {
    load();
  }, []);

  return (
    <AuthGuard>
      <PageContainer header={{ title: false }}>
        <AdminHero
          eyebrow="Price Intelligence"
          title="报价管理"
          subtitle="查看站点标价、官方价和考虑充值比例后的实际折算价。公开价格排行只读取聚合快照，不实时拼装明细。"
          actions={
            <>
              <Button type="primary" size="large" onClick={runCrawl}>
                手动抓价
              </Button>
              <Button size="large" onClick={runAggregate}>
                重建排行
              </Button>
              <Button size="large" onClick={load}>
                刷新
              </Button>
            </>
          }
        />

        <AdminMetrics
          items={[
            { label: "报价记录", value: items.length, note: "当前筛选前记录" },
            { label: "有效报价", value: items.filter((item) => item.status === "active").length, note: "参与快照计算" },
            { label: "覆盖站点", value: new Set(items.map((item) => item.siteSlug)).size, note: "报价来源数量" },
            { label: "已复核", value: items.filter((item) => Boolean(item.reviewedAt)).length, note: "人工确认过价格" }
          ]}
        />

        <ProCard className="cheapai-admin-card" title="报价快照">
          <Space wrap style={{ marginBottom: 16 }}>
            <Input.Search
              allowClear
              placeholder="搜索站点或模型"
              style={{ width: 320 }}
              onSearch={setKeyword}
              onChange={(event) => setKeyword(event.target.value)}
            />
          </Space>
          {error ? <Alert type="warning" showIcon message={`报价接口暂不可用：${error}`} style={{ marginBottom: 16 }} /> : null}
          <Table
            className="cheapai-admin-table"
            loading={loading}
            rowKey="id"
            dataSource={filteredItems}
            pagination={{ pageSize: 10 }}
            columns={[
              { title: "站点", dataIndex: "siteName" },
              { title: "模型", dataIndex: "modelName" },
              {
                title: "官方价",
                render: (_: unknown, record: RelayOfferListItem) => `$${record.officialInputPriceUsd ?? "-"} / $${record.officialOutputPriceUsd ?? "-"}`
              },
              {
                title: "站点标价",
                render: (_: unknown, record: RelayOfferListItem) => `$${record.siteInputPriceUsd ?? "-"} / $${record.siteOutputPriceUsd ?? "-"}`
              },
              {
                title: "实际折算价",
                render: (_: unknown, record: RelayOfferListItem) => `$${record.effectiveInputPriceUsd ?? "-"} / $${record.effectiveOutputPriceUsd ?? "-"}`
              },
              { title: "状态", dataIndex: "status", render: (status: string) => <Tag color={status === "active" ? "success" : "default"}>{status}</Tag> },
              { title: "抓取时间", dataIndex: "crawledAt", render: (value?: string) => value ?? "-" },
              { title: "复核时间", dataIndex: "reviewedAt", render: (value?: string) => value ?? "未复核" },
              {
                title: "操作",
                render: (_: unknown, record: RelayOfferListItem) => (
                  <Button type="link" disabled={Boolean(record.reviewedAt)} onClick={() => reviewOffer(record.id)}>
                    标记复核
                  </Button>
                )
              }
            ]}
          />
        </ProCard>
      </PageContainer>
    </AuthGuard>
  );
}
