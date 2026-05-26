import { useEffect, useState } from "react";
import { PageContainer, ProCard } from "@ant-design/pro-components";
import { Alert, Button, Descriptions, Drawer, Input, Space, Table, Tag, message } from "antd";
import AuthGuard from "@/components/AuthGuard";
import AdminHero, { AdminMetrics } from "@/components/AdminHero";
import { fetchTestRecord, fetchTestRecords, runManualTest, type TestRecordDetail, type TestRecordListItem } from "@/services/operations";
import { formatBeijingTime } from "@/utils/time";

const DEFAULT_PAGE_SIZE = 20;

export default function TestsPage() {
  const [items, setItems] = useState<TestRecordListItem[]>([]);
  const [total, setTotal] = useState(0);
  const [detail, setDetail] = useState<TestRecordDetail | null>(null);
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);
  const [detailLoading, setDetailLoading] = useState(false);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(DEFAULT_PAGE_SIZE);
  const [keyword, setKeyword] = useState("");
  const [searchText, setSearchText] = useState("");

  async function load(targetPage = page, targetPageSize = pageSize, targetKeyword = keyword) {
    setLoading(true);
    try {
      const result = await fetchTestRecords(targetPage, targetPageSize, targetKeyword);
      setItems(result.items);
      setTotal(result.total);
      setError("");
    } catch (requestError) {
      setError((requestError as Error).message);
    } finally {
      setLoading(false);
    }
  }

  async function openDetail(id: number) {
    setDetailLoading(true);
    try {
      setDetail(await fetchTestRecord(id));
    } finally {
      setDetailLoading(false);
    }
  }

  async function runTest() {
    const result = await runManualTest();
    message.success(`${result.message}，影响 ${result.affectedCount} 条`);
    setPage(1);
    await load(1, pageSize, keyword);
  }

  useEffect(() => {
    void load(page, pageSize, keyword);
  }, [page, pageSize, keyword]);

  return (
    <AuthGuard>
      <PageContainer header={{ title: false }}>
        <AdminHero
          eyebrow="Model Testing"
          title="测试管理"
          subtitle="集中查看平台定时测试和手动补测结果，支持查看每次检测项详情。"
          actions={
            <>
              <Button type="primary" size="large" onClick={runTest}>手动测试</Button>
              <Button size="large" onClick={() => load(page, pageSize, keyword)}>刷新</Button>
            </>
          }
        />

        <AdminMetrics
          items={[
            { label: "测试记录", value: total, note: keyword ? "当前搜索结果" : "全部记录" },
            { label: "当前页成功", value: items.filter((item) => item.status === "success").length, note: "参与稳定性聚合" },
            { label: "当前页失败", value: items.filter((item) => item.status !== "success").length, note: "触发风险规则" },
            { label: "当前页首 token", value: averageMs(items.map((item) => item.firstTokenMs)), note: "越低体验越好" }
          ]}
        />

        <ProCard className="cheapai-admin-card" title="测试记录">
          <Input.Search
            allowClear
            placeholder="搜索站点、模型、状态"
            value={searchText}
            style={{ width: 320, marginBottom: 16 }}
            onSearch={(value) => {
              setPage(1);
              setKeyword(value.trim());
            }}
            onChange={(event) => {
              const value = event.target.value;
              setSearchText(value);
              if (!value) {
                setPage(1);
                setKeyword("");
              }
            }}
          />
          {error ? <Alert type="warning" showIcon message={`测试接口暂不可用：${error}`} style={{ marginBottom: 16 }} /> : null}
          <Table
            className="cheapai-admin-table"
            loading={loading}
            rowKey="id"
            dataSource={items}
            pagination={{
              current: page,
              pageSize,
              total,
              showSizeChanger: true,
              showTotal: (count) => `共 ${count} 条`
            }}
            onChange={(pagination) => {
              setPage(pagination.current ?? 1);
              setPageSize(pagination.pageSize ?? DEFAULT_PAGE_SIZE);
            }}
            columns={[
              { title: "站点", dataIndex: "siteName" },
              { title: "模型", dataIndex: "modelName" },
              { title: "类型", dataIndex: "testType" },
              { title: "状态", dataIndex: "status", render: (status) => <Tag color={status === "success" ? "success" : "error"}>{status}</Tag> },
              { title: "首 token", dataIndex: "firstTokenMs", render: (value) => (value ? `${value}ms` : "-") },
              { title: "完整响应", dataIndex: "fullResponseMs", render: (value) => (value ? `${value}ms` : "-") },
              { title: "测试时间", dataIndex: "testedAt", render: (value) => formatBeijingTime(value) },
              { title: "操作", render: (_, record) => <Button type="link" onClick={() => openDetail(record.id)}>查看详情</Button> }
            ]}
          />
        </ProCard>

        <Drawer
          open={Boolean(detail)}
          title={detail ? `${detail.siteName} / ${detail.modelName}` : "测试详情"}
          width={760}
          loading={detailLoading}
          onClose={() => setDetail(null)}
        >
          {detail ? (
            <Space direction="vertical" size={16} style={{ width: "100%" }}>
              <Descriptions bordered column={2} size="small">
                <Descriptions.Item label="状态"><Tag color={detail.status === "success" ? "success" : "error"}>{detail.status}</Tag></Descriptions.Item>
                <Descriptions.Item label="类型">{detail.testType}</Descriptions.Item>
                <Descriptions.Item label="风险">{detail.riskLevel} / {detail.riskScore}</Descriptions.Item>
                <Descriptions.Item label="匹配度">{detail.matchScore}</Descriptions.Item>
                <Descriptions.Item label="首 token">{detail.firstTokenMs ? `${detail.firstTokenMs}ms` : "-"}</Descriptions.Item>
                <Descriptions.Item label="完整响应">{detail.fullResponseMs ? `${detail.fullResponseMs}ms` : "-"}</Descriptions.Item>
                <Descriptions.Item label="Token" span={2}>
                  输入 {detail.inputTokens ?? "-"} / 输出 {detail.outputTokens ?? "-"} / 总计 {detail.totalTokens ?? "-"}
                </Descriptions.Item>
                <Descriptions.Item label="摘要" span={2}>{detail.resultSummary || detail.errorMessage || "-"}</Descriptions.Item>
              </Descriptions>
              <Table
                rowKey={(row) => `${row.code}-${row.name}`}
                dataSource={detail.checks}
                pagination={false}
                size="small"
                columns={[
                  { title: "编号", dataIndex: "code", width: 80 },
                  { title: "检测项", dataIndex: "name", width: 130 },
                  { title: "分类", dataIndex: "category", width: 100 },
                  { title: "结果", render: (_, row) => `${row.status} / ${row.confidence}` },
                  { title: "证据", dataIndex: "evidence" }
                ]}
              />
            </Space>
          ) : null}
        </Drawer>
      </PageContainer>
    </AuthGuard>
  );
}

function averageMs(values: Array<number | undefined>) {
  const numeric = values.filter((value): value is number => typeof value === "number");
  if (numeric.length === 0) {
    return "-";
  }

  return `${Math.round(numeric.reduce((sum, value) => sum + value, 0) / numeric.length)}ms`;
}
