import { useEffect, useMemo, useState } from "react";
import { ProCard } from "@ant-design/pro-components";
import { history } from "@umijs/max";
import { Button, List, Space, Table, Tag, message } from "antd";
import AuthGuard from "@/components/AuthGuard";
import AdminPage from "@/components/AdminPage";
import { fetchModels, type ModelListItem } from "@/services/models";
import {
  fetchJobLogs,
  fetchOffers,
  fetchRisks,
  fetchTestRecords,
  rebuildRankings,
  recalculateRisks,
  runManualCrawl,
  runManualTest,
  type JobExecutionLogListItem
} from "@/services/operations";
import { fetchArticles, fetchSubmissions } from "@/services/participation";
import { fetchSites, type RelaySiteListItem } from "@/services/sites";

type DashboardState = {
  sites: RelaySiteListItem[];
  models: ModelListItem[];
  offerTotal: number;
  testTotal: number;
  riskTotal: number;
  jobLogs: JobExecutionLogListItem[];
  submissionTotal: number;
  articleTotal: number;
  error?: string;
};

const initialState: DashboardState = {
  sites: [],
  models: [],
  offerTotal: 0,
  testTotal: 0,
  riskTotal: 0,
  jobLogs: [],
  submissionTotal: 0,
  articleTotal: 0
};

export default function DashboardPage() {
  const [state, setState] = useState<DashboardState>(initialState);
  const [loading, setLoading] = useState(false);

  const activeSiteCount = useMemo(() => state.sites.filter((site) => site.status === "active").length, [state.sites]);
  const activeModelCount = useMemo(() => state.models.filter((model) => model.status === "active").length, [state.models]);

  async function load() {
    setLoading(true);

    const [sites, models, offers, tests, risks, jobs, submissions, articles] = await Promise.allSettled([
      fetchSites(1, 50),
      fetchModels(1, 50),
      fetchOffers(1, 8),
      fetchTestRecords(1, 8),
      fetchRisks(1, 8),
      fetchJobLogs(1, 8),
      fetchSubmissions(1, 8),
      fetchArticles(1, 8)
    ]);

    const firstError = [sites, models, offers, tests, risks, jobs, submissions, articles]
      .find((result): result is PromiseRejectedResult => result.status === "rejected");

    setState({
      sites: sites.status === "fulfilled" ? sites.value.items : [],
      models: models.status === "fulfilled" ? models.value.items : [],
      offerTotal: offers.status === "fulfilled" ? offers.value.total : 0,
      testTotal: tests.status === "fulfilled" ? tests.value.total : 0,
      riskTotal: risks.status === "fulfilled" ? risks.value.total : 0,
      jobLogs: jobs.status === "fulfilled" ? jobs.value.items : [],
      submissionTotal: submissions.status === "fulfilled" ? submissions.value.total : 0,
      articleTotal: articles.status === "fulfilled" ? articles.value.total : 0,
      error: firstError ? (firstError.reason as Error).message : undefined
    });

    setLoading(false);
  }

  async function runAction(action: "crawl" | "test" | "risk" | "ranking") {
    const actionMap = {
      crawl: runManualCrawl,
      test: runManualTest,
      risk: recalculateRisks,
      ranking: rebuildRankings
    };

    const result = await actionMap[action]();
    message.success(`${result.message}，影响 ${result.affectedCount} 条记录`);
    await load();
  }

  useEffect(() => {
    void load();
  }, []);

  return (
    <AuthGuard>
      <AdminPage
        title="运营工作台"
        subtitle="后台入口收敛为统一操作台，优先处理影响榜单可信度、主数据质量和审核积压的任务。"
        extra={[
          <Button key="crawl" type="primary" onClick={() => runAction("crawl")}>
            手动抓价
          </Button>,
          <Button key="test" onClick={() => runAction("test")}>
            触发测试
          </Button>,
          <Button key="risk" onClick={() => runAction("risk")}>
            重算风险
          </Button>,
          <Button key="refresh" onClick={load}>
            刷新总览
          </Button>
        ]}
        metrics={[
          { label: "站点档案", value: state.sites.length, note: `${activeSiteCount} 个启用中` },
          { label: "模型目录", value: state.models.length, note: `${activeModelCount} 个参与展示` },
          { label: "报价快照", value: state.offerTotal, note: "用于价格排名" },
          { label: "测试记录", value: state.testTotal, note: "支撑能力与稳定性判断" },
          { label: "风险证据", value: state.riskTotal, note: "等待复核与确认" },
          { label: "待审线索", value: state.submissionTotal, note: "来自公开提交入口" },
          { label: "文章资产", value: state.articleTotal, note: "内容口径与公告说明" }
        ]}
      >
        {state.error ? (
          <ProCard className="cheapai-admin-card" style={{ marginBottom: 16 }}>
            <Tag color="warning">部分接口不可用</Tag>
            <span style={{ marginLeft: 12, color: "var(--cheapai-admin-muted)" }}>{state.error}</span>
          </ProCard>
        ) : null}

        <div className="cheapai-admin-section-grid">
          <ProCard
            className="cheapai-admin-card"
            title="最近任务"
            extra={<Button onClick={() => history.push("/jobs")}>查看全部</Button>}
            loading={loading}
          >
            <Table
              className="cheapai-admin-table"
              rowKey="id"
              dataSource={state.jobLogs}
              pagination={false}
              columns={[
                { title: "任务类型", dataIndex: "jobCategory", width: 140 },
                {
                  title: "执行状态",
                  dataIndex: "status",
                  width: 120,
                  render: (status: string) => <Tag color={status === "succeeded" ? "success" : "warning"}>{status}</Tag>
                },
                { title: "执行结果", dataIndex: "message", ellipsis: true },
                { title: "完成时间", dataIndex: "finishedAt", width: 210, render: (value?: string) => value ?? "-" }
              ]}
            />
          </ProCard>

          <ProCard className="cheapai-admin-card" title="快捷入口">
            <List
              dataSource={[
                { title: "新增中转站点", desc: "录入站点基础资料、企业属性和文档链接。", path: "/sites" },
                { title: "维护模型目录", desc: "同步模型官方 ID、价格和展示状态。", path: "/models" },
                { title: "进入风险中心", desc: "处理高风险证据和误报复核。", path: "/risks" },
                { title: "审核公开线索", desc: "处理来自提交入口的站点线索。", path: "/submissions" }
              ]}
              renderItem={(item) => (
                <List.Item
                  actions={[
                    <Button key={item.path} type="link" onClick={() => history.push(item.path)}>
                      进入
                    </Button>
                  ]}
                >
                  <List.Item.Meta title={item.title} description={item.desc} />
                </List.Item>
              )}
            />
            <Space wrap style={{ marginTop: 16 }}>
              <Button onClick={() => runAction("ranking")}>重建排名</Button>
              <Button onClick={() => history.push("/offers")}>查看报价快照</Button>
            </Space>
          </ProCard>
        </div>
      </AdminPage>
    </AuthGuard>
  );
}
