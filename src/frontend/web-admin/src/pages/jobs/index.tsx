import { useEffect, useMemo, useState } from "react";
import { PageContainer, ProCard } from "@ant-design/pro-components";
import { Alert, Button, Input, Space, Table, Tag, message } from "antd";
import AuthGuard from "@/components/AuthGuard";
import AdminHero, { AdminMetrics } from "@/components/AdminHero";
import {
  fetchJobLogs,
  rebuildRankings,
  recalculateRisks,
  runManualCrawl,
  runManualTest,
  type JobActionResponse,
  type JobExecutionLogListItem
} from "@/services/operations";

const statusColors: Record<string, string> = {
  succeeded: "success",
  failed: "error",
  running: "processing",
  queued: "default"
};

export default function JobsPage() {
  const [items, setItems] = useState<JobExecutionLogListItem[]>([]);
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);
  const [actionLoading, setActionLoading] = useState("");
  const [keyword, setKeyword] = useState("");

  const filteredItems = useMemo(() => {
    const normalizedKeyword = keyword.trim().toLowerCase();
    if (!normalizedKeyword) {
      return items;
    }

    return items.filter((item) =>
      [item.jobCategory, item.status, item.message, item.jobId]
        .some((value) => String(value ?? "").toLowerCase().includes(normalizedKeyword))
    );
  }, [items, keyword]);

  async function load() {
    setLoading(true);
    try {
      const result = await fetchJobLogs(1, 80);
      setItems(result.items);
      setError("");
    } catch (requestError) {
      setError((requestError as Error).message);
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    load();
  }, []);

  async function runAction(name: string, action: () => Promise<JobActionResponse>) {
    setActionLoading(name);
    try {
      const result = await action();
      message.success(`${result.message}，影响 ${result.affectedCount} 条`);
      await load();
    } catch (requestError) {
      message.error((requestError as Error).message);
    } finally {
      setActionLoading("");
    }
  }

  return (
    <AuthGuard>
      <PageContainer header={{ title: false }}>
        <AdminHero
          eyebrow="Operations Timeline"
          title="任务日志"
          subtitle="跟踪抓价、模型测试、风险计算和排行榜聚合任务的执行结果。这里用于判断自动化链路是否健康，以及失败任务是否需要人工补偿。"
          actions={
            <Space wrap>
              <Button size="large" loading={actionLoading === "crawl"} onClick={() => runAction("crawl", runManualCrawl)}>
                手动抓价
              </Button>
              <Button size="large" loading={actionLoading === "test"} onClick={() => runAction("test", runManualTest)}>
                立即自动测试
              </Button>
              <Button size="large" loading={actionLoading === "risk"} onClick={() => runAction("risk", recalculateRisks)}>
                重算风险
              </Button>
              <Button size="large" loading={actionLoading === "ranking"} onClick={() => runAction("ranking", rebuildRankings)}>
                重建排行
              </Button>
              <Button type="primary" size="large" onClick={load}>
                刷新日志
              </Button>
            </Space>
          }
        />

        <AdminMetrics
          items={[
            { label: "日志数量", value: items.length, note: "最近任务执行记录" },
            { label: "成功", value: items.filter((item) => item.status === "succeeded").length, note: "可作为健康样本" },
            { label: "失败", value: items.filter((item) => item.status === "failed").length, note: "需要排查或重试" },
            { label: "任务类型", value: new Set(items.map((item) => item.jobCategory)).size, note: "抓取 / 测试 / 聚合" }
          ]}
        />

        <ProCard className="cheapai-admin-card" title="执行记录">
          <Input.Search
            allowClear
            placeholder="搜索任务类型、状态或消息"
            style={{ width: 360, marginBottom: 16 }}
            onSearch={setKeyword}
            onChange={(event) => setKeyword(event.target.value)}
          />
          {error ? <Alert type="warning" showIcon message={`任务日志暂不可用：${error}`} style={{ marginBottom: 16 }} /> : null}
          <Table
            className="cheapai-admin-table"
            loading={loading}
            rowKey="id"
            dataSource={filteredItems}
            pagination={{ pageSize: 10 }}
            columns={[
              { title: "任务类型", dataIndex: "jobCategory" },
              { title: "Job ID", dataIndex: "jobId", render: (value?: number) => value ?? "-" },
              {
                title: "状态",
                dataIndex: "status",
                render: (status: string) => <Tag color={statusColors[status] ?? "warning"}>{status}</Tag>
              },
              { title: "消息", dataIndex: "message", ellipsis: true },
              { title: "开始时间", dataIndex: "startedAt", render: (value?: string) => value ?? "-" },
              { title: "结束时间", dataIndex: "finishedAt", render: (value?: string) => value ?? "-" },
              { title: "创建时间", dataIndex: "createdAt" }
            ]}
          />
        </ProCard>
      </PageContainer>
    </AuthGuard>
  );
}
