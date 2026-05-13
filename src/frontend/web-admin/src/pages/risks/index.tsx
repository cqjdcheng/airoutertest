import { useEffect, useMemo, useState } from "react";
import { PageContainer, ProCard } from "@ant-design/pro-components";
import { Alert, Button, Descriptions, Input, message, Modal, Space, Table, Tag } from "antd";
import AuthGuard from "@/components/AuthGuard";
import AdminHero, { AdminMetrics } from "@/components/AdminHero";
import {
  fetchRisk,
  fetchRisks,
  recalculateRisks,
  reviewRisk,
  type RiskEvidenceDetail,
  type RiskEvidenceListItem
} from "@/services/operations";

const levelColors: Record<string, string> = {
  low: "success",
  medium: "warning",
  high: "error",
  critical: "magenta"
};

const reviewColors: Record<string, string> = {
  pending: "processing",
  confirmed: "error",
  false_positive: "success",
  ignored: "default"
};

export default function RisksPage() {
  const [items, setItems] = useState<RiskEvidenceListItem[]>([]);
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);
  const [keyword, setKeyword] = useState("");
  const [detail, setDetail] = useState<RiskEvidenceDetail | null>(null);

  const filteredItems = useMemo(() => {
    const normalizedKeyword = keyword.trim().toLowerCase();
    if (!normalizedKeyword) {
      return items;
    }

    return items.filter((item) =>
      [item.siteName, item.modelName, item.ruleCode, item.riskLevel, item.evidenceSummary, item.reviewStatus]
        .some((value) => String(value ?? "").toLowerCase().includes(normalizedKeyword))
    );
  }, [items, keyword]);

  async function load() {
    setLoading(true);
    try {
      const result = await fetchRisks(1, 80);
      setItems(result.items);
      setError("");
    } catch (requestError) {
      setError((requestError as Error).message);
    } finally {
      setLoading(false);
    }
  }

  async function runRisk() {
    const result = await recalculateRisks();
    message.success(`${result.message}，影响 ${result.affectedCount} 条`);
    await load();
  }

  async function openDetail(id: number) {
    const result = await fetchRisk(id);
    setDetail(result);
  }

  async function updateReviewStatus(status: "confirmed" | "false_positive" | "ignored") {
    if (!detail) {
      return;
    }

    await reviewRisk(detail.id, status);
    message.success("风险复核状态已更新");
    setDetail(null);
    await load();
  }

  useEffect(() => {
    load();
  }, []);

  return (
    <AuthGuard>
      <PageContainer header={{ title: false }}>
        <AdminHero
          eyebrow="Risk Intelligence"
          title="风险中心"
          subtitle="集中查看缓存假响应、模型偷换、降级冒充、价格虚标和响应伪造证据。风险结果先用于运营复核，再进入用户端摘要展示。"
          actions={
            <>
              <Button type="primary" size="large" onClick={runRisk}>
                重算风险
              </Button>
              <Button size="large" onClick={load}>
                刷新
              </Button>
            </>
          }
        />

        <AdminMetrics
          items={[
            { label: "风险证据", value: items.length, note: "当前入库证据总量" },
            { label: "高危以上", value: items.filter((item) => ["high", "critical"].includes(item.riskLevel)).length, note: "需要优先复核" },
            { label: "待复核", value: items.filter((item) => item.reviewStatus === "pending").length, note: "未人工确认" },
            { label: "平均风险分", value: averageScore(items), note: "规则累加后封顶 100" }
          ]}
        />

        <ProCard className="cheapai-admin-card" title="风险证据列表">
          <Input.Search
            allowClear
            placeholder="搜索站点、模型、规则、证据"
            style={{ width: 360, marginBottom: 16 }}
            onSearch={setKeyword}
            onChange={(event) => setKeyword(event.target.value)}
          />
          {error ? <Alert type="warning" showIcon message={`风险接口暂不可用：${error}`} style={{ marginBottom: 16 }} /> : null}
          <Table
            className="cheapai-admin-table"
            loading={loading}
            rowKey="id"
            dataSource={filteredItems}
            pagination={{ pageSize: 10 }}
            columns={[
              { title: "站点", dataIndex: "siteName" },
              { title: "模型", dataIndex: "modelName" },
              { title: "命中规则", dataIndex: "ruleCode" },
              {
                title: "等级",
                dataIndex: "riskLevel",
                render: (level: string) => <Tag color={levelColors[level] ?? "default"}>{level}</Tag>
              },
              {
                title: "风险分",
                dataIndex: "riskScore",
                sorter: (a, b) => a.riskScore - b.riskScore
              },
              { title: "证据摘要", dataIndex: "evidenceSummary", ellipsis: true },
              {
                title: "复核状态",
                dataIndex: "reviewStatus",
                render: (status: string) => <Tag color={reviewColors[status] ?? "default"}>{status}</Tag>
              },
              { title: "创建时间", dataIndex: "createdAt" },
              {
                title: "操作",
                render: (_: unknown, record: RiskEvidenceListItem) => (
                  <Button type="link" onClick={() => openDetail(record.id)}>
                    查看证据
                  </Button>
                )
              }
            ]}
          />
        </ProCard>

        <Modal
          title="风险证据详情"
          open={Boolean(detail)}
          width={760}
          onCancel={() => setDetail(null)}
          footer={
            <Space>
              <Button onClick={() => setDetail(null)}>关闭</Button>
              <Button onClick={() => updateReviewStatus("ignored")}>忽略</Button>
              <Button onClick={() => updateReviewStatus("false_positive")}>标记误报</Button>
              <Button type="primary" danger onClick={() => updateReviewStatus("confirmed")}>
                确认风险
              </Button>
            </Space>
          }
          destroyOnClose
        >
          {detail ? (
            <>
              <Descriptions column={2} bordered size="small">
                <Descriptions.Item label="站点">{detail.siteName}</Descriptions.Item>
                <Descriptions.Item label="模型">{detail.modelName}</Descriptions.Item>
                <Descriptions.Item label="规则">{detail.ruleCode}</Descriptions.Item>
                <Descriptions.Item label="等级">{detail.riskLevel}</Descriptions.Item>
                <Descriptions.Item label="风险分">{detail.riskScore}</Descriptions.Item>
                <Descriptions.Item label="复核状态">{detail.reviewStatus}</Descriptions.Item>
                <Descriptions.Item label="测试记录">{detail.testRecordId ?? "-"}</Descriptions.Item>
                <Descriptions.Item label="复核时间">{detail.reviewedAt ?? "-"}</Descriptions.Item>
              </Descriptions>
              <div style={{ marginTop: 16 }}>
                <strong>证据摘要</strong>
                <p style={{ color: "#667085", lineHeight: 1.8 }}>{detail.evidenceSummary}</p>
              </div>
              <pre style={{ maxHeight: 260, overflow: "auto", borderRadius: 12, background: "#f6f8f7", padding: 16 }}>
                {formatEvidenceJson(detail.evidenceJson)}
              </pre>
            </>
          ) : null}
        </Modal>
      </PageContainer>
    </AuthGuard>
  );
}

function averageScore(items: RiskEvidenceListItem[]) {
  if (items.length === 0) {
    return "-";
  }

  return Math.round(items.reduce((sum, item) => sum + item.riskScore, 0) / items.length);
}

function formatEvidenceJson(value?: string) {
  if (!value) {
    return "暂无结构化证据。";
  }

  try {
    return JSON.stringify(JSON.parse(value), null, 2);
  } catch {
    return value;
  }
}
