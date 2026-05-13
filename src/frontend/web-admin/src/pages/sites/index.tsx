import { useEffect, useMemo, useState } from "react";
import {
  DrawerForm,
  ProCard,
  ProFormSwitch,
  ProFormText,
  ProFormTextArea
} from "@ant-design/pro-components";
import { Alert, Button, Form, Input, Popconfirm, Select, Space, Table, Tag, message } from "antd";
import AuthGuard from "@/components/AuthGuard";
import AdminPage from "@/components/AdminPage";
import {
  createSite,
  fetchSite,
  fetchSites,
  updateSite,
  updateSiteStatus,
  type RelaySiteListItem
} from "@/services/sites";

type SiteFormValues = {
  slug?: string;
  name?: string;
  baseUrl?: string;
  websiteUrl?: string;
  description?: string;
  supportsRefund?: boolean;
  supportsInvoice?: boolean;
  hasDocs?: boolean;
  docsUrl?: string;
  inviteUrl?: string;
  recentReview?: string;
};

const defaultSiteValues: SiteFormValues = {
  supportsRefund: false,
  supportsInvoice: false,
  hasDocs: false
};

const statusLabels: Record<string, string> = {
  active: "启用",
  suspended: "暂停",
  draft: "草稿",
  archived: "归档"
};

const statusColors: Record<string, string> = {
  active: "success",
  suspended: "warning",
  draft: "default",
  archived: "default"
};

export default function SitesPage() {
  const [items, setItems] = useState<RelaySiteListItem[]>([]);
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);
  const [drawerOpen, setDrawerOpen] = useState(false);
  const [drawerLoading, setDrawerLoading] = useState(false);
  const [editing, setEditing] = useState<RelaySiteListItem | null>(null);
  const [keyword, setKeyword] = useState("");
  const [statusFilter, setStatusFilter] = useState<string>("all");
  const [form] = Form.useForm<SiteFormValues>();

  const filteredItems = useMemo(() => {
    const normalizedKeyword = keyword.trim().toLowerCase();

    return items.filter((item) => {
      const keywordMatched = [item.name, item.slug, item.baseUrl, item.description]
        .filter(Boolean)
        .some((value) => String(value).toLowerCase().includes(normalizedKeyword));
      const statusMatched = statusFilter === "all" || item.status === statusFilter;
      return keywordMatched && statusMatched;
    });
  }, [items, keyword, statusFilter]);

  async function load() {
    setLoading(true);

    try {
      const result = await fetchSites(1, 80);
      setItems(result.items);
      setError("");
    } catch (requestError) {
      setError((requestError as Error).message);
    } finally {
      setLoading(false);
    }
  }

  function openCreateDrawer() {
    setEditing(null);
    form.resetFields();
    form.setFieldsValue(defaultSiteValues);
    setDrawerOpen(true);
  }

  async function openEditDrawer(record: RelaySiteListItem) {
    setDrawerLoading(true);

    try {
      const detail = await fetchSite(record.id);
      setEditing(detail);
      form.resetFields();
      form.setFieldsValue({
        ...defaultSiteValues,
        ...detail
      });
      setDrawerOpen(true);
    } finally {
      setDrawerLoading(false);
    }
  }

  async function submit(values: SiteFormValues) {
    const payload = {
      slug: values.slug,
      name: values.name ?? "",
      baseUrl: values.baseUrl ?? "",
      websiteUrl: values.websiteUrl,
      description: values.description,
      docsUrl: values.docsUrl,
      inviteUrl: values.inviteUrl,
      recentReview: values.recentReview,
      supportsRefund: Boolean(values.supportsRefund),
      supportsInvoice: Boolean(values.supportsInvoice),
      hasDocs: Boolean(values.hasDocs)
    };

    if (editing) {
      await updateSite(editing.id, payload);
      message.success("站点已更新");
    } else {
      await createSite(payload);
      message.success("站点已创建");
    }

    setDrawerOpen(false);
    setEditing(null);
    await load();
    return true;
  }

  async function changeStatus(record: RelaySiteListItem, nextStatus?: string) {
    const targetStatus = nextStatus ?? (record.status === "active" ? "suspended" : "active");
    await updateSiteStatus(record.id, targetStatus);
    message.success(`站点状态已切换为 ${statusLabels[targetStatus] ?? targetStatus}`);
    await load();
  }

  useEffect(() => {
    void load();
  }, []);

  return (
    <AuthGuard>
      <AdminPage
        title="中转站点"
        subtitle="按站点档案统一维护展示状态、企业属性、文档与邀请链接，新增和编辑操作改为单侧抽屉，避免频繁打断列表上下文。"
        extra={[
          <Button key="create" type="primary" onClick={openCreateDrawer}>
            新增站点
          </Button>,
          <Button key="refresh" onClick={load}>
            刷新数据
          </Button>
        ]}
        metrics={[
          { label: "全部站点", value: items.length, note: "后台已收录档案" },
          { label: "启用站点", value: items.filter((item) => item.status === "active").length, note: "参与公开展示" },
          { label: "支持发票", value: items.filter((item) => item.supportsInvoice).length, note: "企业采购筛选项" },
          { label: "带文档", value: items.filter((item) => item.hasDocs).length, note: "可验证接入资料" }
        ]}
      >
        <ProCard className="cheapai-admin-card" title="站点列表">
          <Space wrap style={{ marginBottom: 16 }}>
            <Input.Search
              allowClear
              placeholder="搜索名称、Slug、Base URL"
              style={{ width: 320 }}
              onSearch={setKeyword}
              onChange={(event) => setKeyword(event.target.value)}
            />
            <Select
              value={statusFilter}
              style={{ width: 160 }}
              onChange={setStatusFilter}
              options={[
                { label: "全部状态", value: "all" },
                { label: "启用", value: "active" },
                { label: "暂停", value: "suspended" },
                { label: "草稿", value: "draft" },
                { label: "归档", value: "archived" }
              ]}
            />
          </Space>

          {error ? <Alert type="warning" showIcon message={`站点接口暂不可用：${error}`} style={{ marginBottom: 16 }} /> : null}

          <Table
            className="cheapai-admin-table"
            loading={loading}
            rowKey="id"
            dataSource={filteredItems}
            pagination={{ pageSize: 12 }}
            columns={[
              {
                title: "站点",
                dataIndex: "name",
                render: (_, record) => (
                  <div>
                    <strong>{record.name}</strong>
                    <div style={{ color: "var(--cheapai-admin-muted)", fontSize: 12 }}>{record.slug}</div>
                  </div>
                )
              },
              { title: "Base URL", dataIndex: "baseUrl", ellipsis: true },
              {
                title: "站点能力",
                render: (_, record) => (
                  <Space size={4} wrap>
                    {record.supportsInvoice ? <Tag color="blue">发票</Tag> : null}
                    {record.supportsRefund ? <Tag color="green">退款</Tag> : null}
                    {record.hasDocs ? <Tag>文档</Tag> : null}
                  </Space>
                )
              },
              {
                title: "状态",
                dataIndex: "status",
                width: 110,
                render: (status: string) => <Tag color={statusColors[status] ?? "default"}>{statusLabels[status] ?? status}</Tag>
              },
              {
                title: "最后更新时间",
                dataIndex: "updatedAtUtc",
                width: 200,
                render: (value: string | undefined, record) => value ?? record.createdAtUtc
              },
              {
                title: "操作",
                width: 240,
                render: (_, record) => (
                  <Space>
                    <Button type="link" onClick={() => openEditDrawer(record)}>
                      编辑
                    </Button>
                    <Popconfirm
                      title={record.status === "active" ? "确认暂停该站点？" : "确认启用该站点？"}
                      onConfirm={() => changeStatus(record)}
                    >
                      <Button type="link">{record.status === "active" ? "暂停" : "启用"}</Button>
                    </Popconfirm>
                    <Popconfirm title="确认归档该站点？" onConfirm={() => changeStatus(record, "archived")}>
                      <Button type="link" danger>
                        归档
                      </Button>
                    </Popconfirm>
                  </Space>
                )
              }
            ]}
          />
        </ProCard>

        <DrawerForm<SiteFormValues>
          form={form}
          open={drawerOpen}
          title={editing ? "编辑站点" : "新增站点"}
          width={560}
          loading={drawerLoading}
          drawerProps={{
            destroyOnClose: false,
            onClose: () => {
              setDrawerOpen(false);
              setEditing(null);
            }
          }}
          submitter={{
            searchConfig: {
              submitText: editing ? "保存更新" : "创建站点"
            }
          }}
          onFinish={submit}
        >
          <ProFormText name="name" label="站点名称" rules={[{ required: true, message: "请输入站点名称" }]} />
          <ProFormText name="slug" label="Slug" />
          <ProFormText name="baseUrl" label="Base URL" rules={[{ required: true, message: "请输入 Base URL" }]} />
          <ProFormText name="websiteUrl" label="官网地址" />
          <ProFormTextArea name="description" label="站点说明" fieldProps={{ rows: 3 }} />
          <ProFormText name="docsUrl" label="文档地址" />
          <ProFormText name="inviteUrl" label="邀请链接" />
          <ProFormTextArea name="recentReview" label="近期体验" fieldProps={{ rows: 4 }} />
          <ProFormSwitch name="supportsRefund" label="支持退款" />
          <ProFormSwitch name="supportsInvoice" label="支持发票" />
          <ProFormSwitch name="hasDocs" label="提供文档" />
        </DrawerForm>
      </AdminPage>
    </AuthGuard>
  );
}
