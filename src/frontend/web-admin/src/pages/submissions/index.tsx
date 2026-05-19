import { useEffect, useMemo, useState, type Key } from "react";
import { PageContainer, ProCard } from "@ant-design/pro-components";
import { Button, Input, message, Popconfirm, Space, Table, Tag } from "antd";
import AuthGuard from "@/components/AuthGuard";
import AdminHero, { AdminMetrics } from "@/components/AdminHero";
import {
  approveSubmission,
  fetchSubmissions,
  rejectSubmission,
  type SiteSubmissionListItem
} from "@/services/participation";
import { formatBeijingTime } from "@/utils/time";

const reviewColors: Record<string, string> = {
  pending: "processing",
  approved: "success",
  rejected: "error"
};

export default function SubmissionsPage() {
  const [items, setItems] = useState<SiteSubmissionListItem[]>([]);
  const [loading, setLoading] = useState(false);
  const [keyword, setKeyword] = useState("");
  const [selectedSubmissionIds, setSelectedSubmissionIds] = useState<Key[]>([]);

  const filteredItems = useMemo(() => {
    const normalizedKeyword = keyword.trim().toLowerCase();
    if (!normalizedKeyword) {
      return items;
    }

    return items.filter((item) =>
      [item.siteName, item.siteUrl, item.contact, item.description, item.reviewStatus]
        .some((value) => String(value ?? "").toLowerCase().includes(normalizedKeyword))
    );
  }, [items, keyword]);

  async function load() {
    setLoading(true);
    try {
      const result = await fetchSubmissions(1, 80);
      setItems(result.items);
    } finally {
      setLoading(false);
    }
  }

  async function review(id: number, approved: boolean) {
    if (approved) {
      await approveSubmission(id);
      message.success("提交已通过");
    } else {
      await rejectSubmission(id);
      message.success("提交已拒绝");
    }
    await load();
  }

  async function batchReview(approved: boolean) {
    const ids = selectedSubmissionIds.map(Number);
    if (!ids.length) {
      return;
    }

    if (approved) {
      await Promise.all(ids.map((id) => approveSubmission(id)));
      message.success(`已通过 ${ids.length} 条提交`);
    } else {
      await Promise.all(ids.map((id) => rejectSubmission(id)));
      message.success(`已拒绝 ${ids.length} 条提交`);
    }

    setSelectedSubmissionIds([]);
    await load();
  }

  useEffect(() => {
    load();
  }, []);

  return (
    <AuthGuard>
      <PageContainer header={{ title: false }}>
        <AdminHero
          eyebrow="Community Intake"
          title="提交审核"
          subtitle="处理公开入口提交的中转站信息。提交通过后仍进入后台主数据流程，不允许绕过审核直接前台上架。"
          actions={
            <Button type="primary" size="large" onClick={load}>
              刷新提交
            </Button>
          }
        />

        <AdminMetrics
          items={[
            { label: "提交总量", value: items.length, note: "公开入口累计样本" },
            { label: "待审核", value: items.filter((item) => item.reviewStatus === "pending").length, note: "需要处理" },
            { label: "已通过", value: items.filter((item) => item.reviewStatus === "approved").length, note: "可转入收录" },
            { label: "已拒绝", value: items.filter((item) => item.reviewStatus === "rejected").length, note: "不进入主数据" }
          ]}
        />

        <ProCard className="cheapai-admin-card" title="提交列表">
          <Space wrap style={{ marginBottom: 16 }}>
            <Input.Search
              allowClear
              placeholder="搜索站点、地址、联系方式"
              style={{ width: 360 }}
              onSearch={setKeyword}
              onChange={(event) => setKeyword(event.target.value)}
            />
            <Popconfirm title={`确认通过选中的 ${selectedSubmissionIds.length} 条提交？`} onConfirm={() => batchReview(true)}>
              <Button disabled={!selectedSubmissionIds.length}>
                批量通过
              </Button>
            </Popconfirm>
            <Popconfirm title={`确认拒绝选中的 ${selectedSubmissionIds.length} 条提交？`} onConfirm={() => batchReview(false)}>
              <Button danger disabled={!selectedSubmissionIds.length}>
                批量拒绝
              </Button>
            </Popconfirm>
          </Space>
          <Table
            className="cheapai-admin-table"
            loading={loading}
            rowKey="id"
            dataSource={filteredItems}
            pagination={{ pageSize: 10 }}
            rowSelection={{
              selectedRowKeys: selectedSubmissionIds,
              onChange: setSelectedSubmissionIds,
              getCheckboxProps: (record) => ({ disabled: record.reviewStatus !== "pending" })
            }}
            columns={[
              { title: "站点", dataIndex: "siteName" },
              { title: "地址", dataIndex: "siteUrl", ellipsis: true },
              { title: "联系方式", dataIndex: "contact", render: (value?: string) => value || "-" },
              { title: "描述", dataIndex: "description", ellipsis: true, render: (value?: string) => value || "-" },
              {
                title: "状态",
                dataIndex: "reviewStatus",
                render: (status: string) => <Tag color={reviewColors[status] ?? "default"}>{status}</Tag>
              },
              { title: "提交时间", dataIndex: "createdAt", render: (value?: string) => formatBeijingTime(value) },
              {
                title: "操作",
                render: (_: unknown, record: SiteSubmissionListItem) => (
                  <Space>
                    <Button type="link" disabled={record.reviewStatus !== "pending"} onClick={() => review(record.id, true)}>
                      通过
                    </Button>
                    <Button type="link" danger disabled={record.reviewStatus !== "pending"} onClick={() => review(record.id, false)}>
                      拒绝
                    </Button>
                  </Space>
                )
              }
            ]}
          />
        </ProCard>
      </PageContainer>
    </AuthGuard>
  );
}
