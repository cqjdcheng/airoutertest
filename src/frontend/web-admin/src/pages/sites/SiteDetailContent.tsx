import { Descriptions, Empty, Space, Table, Tabs, Tag } from "antd";
import type { RelaySiteDetail, RelaySiteOffer, RelaySiteTestRecord } from "@/services/sites";

type SiteDetailContentProps = {
  detail: RelaySiteDetail;
};

const statusTone: Record<string, string> = {
  active: "success",
  draft: "default",
  suspended: "warning",
  archived: "default",
  hidden: "warning",
  failed: "error",
  succeeded: "success",
  success: "success"
};

function price(input?: number, output?: number) {
  return `$${input ?? "-"} / $${output ?? "-"}`;
}

export default function SiteDetailContent({ detail }: SiteDetailContentProps) {
  return (
    <Tabs
      items={[
        {
          key: "base",
          label: "基本信息",
          children: (
            <Descriptions bordered column={2}>
              <Descriptions.Item label="站点名称">{detail.name}</Descriptions.Item>
              <Descriptions.Item label="状态">
                <Tag color={statusTone[detail.status] ?? "default"}>{detail.status}</Tag>
              </Descriptions.Item>
              <Descriptions.Item label="Base URL">{detail.baseUrl}</Descriptions.Item>
              <Descriptions.Item label="官网">{detail.websiteUrl ?? "-"}</Descriptions.Item>
              <Descriptions.Item label="文档">{detail.docsUrl ?? "-"}</Descriptions.Item>
              <Descriptions.Item label="邀请链接">{detail.inviteUrl ?? "-"}</Descriptions.Item>
              <Descriptions.Item label="能力" span={2}>
                <Space>
                  {detail.supportsInvoice ? <Tag color="blue">支持发票</Tag> : <Tag>无发票标记</Tag>}
                  {detail.supportsRefund ? <Tag color="green">支持退款</Tag> : <Tag>无退款标记</Tag>}
                  {detail.hasDocs ? <Tag>提供文档</Tag> : <Tag>无文档标记</Tag>}
                </Space>
              </Descriptions.Item>
              <Descriptions.Item label="说明" span={2}>{detail.description ?? "-"}</Descriptions.Item>
              <Descriptions.Item label="近期体验" span={2}>{detail.recentReview ?? "-"}</Descriptions.Item>
            </Descriptions>
          )
        },
        {
          key: "models",
          label: "模型信息",
          children: detail.offers.length ? (
            <Table<RelaySiteOffer>
              rowKey={(record) => String(record.id ?? `${record.modelId}-${record.officialModelId}`)}
              dataSource={detail.offers}
              pagination={false}
              scroll={{ x: "max-content" }}
              columns={[
                { title: "模型", dataIndex: "displayName" },
                { title: "Vendor", dataIndex: "vendor", width: 120 },
                { title: "请求名称", dataIndex: "requestName", ellipsis: true },
                { title: "接口类型", dataIndex: "apiType", width: 100 },
                { title: "Official ID", dataIndex: "officialModelId", ellipsis: true },
                { title: "官方价", render: (_, record) => price(record.officialInputPriceUsd, record.officialOutputPriceUsd) },
                { title: "站点价", render: (_, record) => price(record.siteInputPriceUsd, record.siteOutputPriceUsd) },
                { title: "折算价", render: (_, record) => price(record.effectiveInputPriceUsd, record.effectiveOutputPriceUsd) },
                { title: "来源", dataIndex: "sourceType", width: 90 },
                { title: "状态", dataIndex: "status", width: 90, render: (status: string) => <Tag color={statusTone[status] ?? "default"}>{status}</Tag> }
              ]}
            />
          ) : (
            <Empty description="暂无模型报价" />
          )
        },
        {
          key: "tests",
          label: "最近测试记录",
          children: detail.recentTests.length ? (
            <Table<RelaySiteTestRecord>
              rowKey="id"
              dataSource={detail.recentTests}
              pagination={false}
              scroll={{ x: "max-content" }}
              columns={[
                { title: "模型", dataIndex: "modelName" },
                { title: "类型", dataIndex: "testType", width: 110 },
                { title: "状态", dataIndex: "status", width: 100, render: (status: string) => <Tag color={statusTone[status] ?? "default"}>{status}</Tag> },
                { title: "首 Token", dataIndex: "firstTokenMs", width: 110, render: (value?: number) => value ? `${value}ms` : "-" },
                { title: "完整响应", dataIndex: "fullResponseMs", width: 110, render: (value?: number) => value ? `${value}ms` : "-" },
                { title: "风险", dataIndex: "riskScore", width: 90 },
                { title: "错误", dataIndex: "errorMessage", ellipsis: true, render: (value?: string) => value ?? "-" },
                { title: "测试时间", dataIndex: "testedAt", width: 190 }
              ]}
            />
          ) : (
            <Empty description="暂无测试记录" />
          )
        }
      ]}
    />
  );
}
