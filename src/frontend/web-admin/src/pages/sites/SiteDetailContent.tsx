import { useEffect, useState } from "react";
import { Descriptions, Empty, Input, Space, Table, Tabs, Tag } from "antd";
import { fetchSiteOffers, type RelaySiteDetail, type RelaySiteOffer, type RelaySiteTestRecord } from "@/services/sites";
import { formatBeijingTime } from "@/utils/time";

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
  const [offers, setOffers] = useState<RelaySiteOffer[]>([]);
  const [offerLoading, setOfferLoading] = useState(false);
  const [offerKeyword, setOfferKeyword] = useState("");
  const [offerPage, setOfferPage] = useState(1);
  const [offerPageSize, setOfferPageSize] = useState(20);
  const [offerTotal, setOfferTotal] = useState(0);

  async function loadOffers(options: { page?: number; pageSize?: number; keyword?: string } = {}) {
    const nextPage = options.page ?? offerPage;
    const nextPageSize = options.pageSize ?? offerPageSize;
    const nextKeyword = options.keyword ?? offerKeyword;
    setOfferLoading(true);

    try {
      const result = await fetchSiteOffers(detail.id, {
        page: nextPage,
        pageSize: nextPageSize,
        keyword: nextKeyword,
        status: "all"
      });
      setOffers(result.items);
      setOfferPage(result.page);
      setOfferPageSize(result.pageSize);
      setOfferTotal(result.total);
    } finally {
      setOfferLoading(false);
    }
  }

  useEffect(() => {
    setOffers([]);
    setOfferKeyword("");
    setOfferPage(1);
    setOfferTotal(0);
    void loadOffers({ page: 1, keyword: "" });
  }, [detail.id]);

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
              <Descriptions.Item label="自动测试">
                <Space>
                  <Tag color={detail.autoTestEnabled ? "purple" : "default"}>{detail.autoTestEnabled ? "启用" : "停用"}</Tag>
                  <Tag color={detail.hasTestApiKey ? "green" : "orange"}>{detail.hasTestApiKey ? "已配置 Key" : "未配置 Key"}</Tag>
                </Space>
              </Descriptions.Item>
              <Descriptions.Item label="测试间隔">{detail.testIntervalMinutes} 分钟</Descriptions.Item>
              <Descriptions.Item label="上次自动测试" span={2}>{formatBeijingTime(detail.lastAutoTestAt)}</Descriptions.Item>
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
          children: (
            <Space direction="vertical" size={12} style={{ width: "100%" }}>
              <Space wrap style={{ justifyContent: "space-between", width: "100%" }}>
                <Input.Search
                  allowClear
                  value={offerKeyword}
                  placeholder="搜索模型、请求名、厂商"
                  style={{ width: 320 }}
                  onSearch={(value) => {
                    setOfferKeyword(value);
                    void loadOffers({ page: 1, keyword: value });
                  }}
                  onChange={(event) => {
                    setOfferKeyword(event.target.value);
                    if (!event.target.value) {
                      void loadOffers({ page: 1, keyword: "" });
                    }
                  }}
                />
                <Tag color="blue">共 {offerTotal} 个模型</Tag>
              </Space>
              <Table<RelaySiteOffer>
                rowKey={(record) => String(record.id ?? `${record.modelId}-${record.officialModelId}`)}
                loading={offerLoading}
                dataSource={offers}
                locale={{ emptyText: <Empty description="暂无模型报价" /> }}
                pagination={{
                  current: offerPage,
                  pageSize: offerPageSize,
                  total: offerTotal,
                  showSizeChanger: true,
                  onChange: (page, pageSize) => void loadOffers({ page, pageSize })
                }}
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
                  { title: "测试 Key", dataIndex: "hasTestApiKey", width: 100, render: (hasKey?: boolean) => <Tag color={hasKey ? "green" : "orange"}>{hasKey ? "已配置" : "未配置"}</Tag> },
                  { title: "状态", dataIndex: "status", width: 90, render: (status: string) => <Tag color={statusTone[status] ?? "default"}>{status}</Tag> }
                ]}
              />
            </Space>
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
                { title: "测试时间", dataIndex: "testedAt", width: 190, render: (value?: string) => formatBeijingTime(value) }
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
