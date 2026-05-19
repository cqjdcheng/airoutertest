import { useEffect, useMemo, useState, type Key } from "react";
import { DrawerForm, ProCard, ProFormDigit, ProFormSelect, ProFormText, ProFormTextArea } from "@ant-design/pro-components";
import { Alert, Button, Form, Input, Popconfirm, Select, Space, Table, Tag, message } from "antd";
import AdminPage from "@/components/AdminPage";
import AuthGuard from "@/components/AuthGuard";
import { getAccessToken } from "@/services/api";
import {
  createModelProvider,
  deleteModelProvider,
  fetchModelProviders,
  updateModelProvider,
  updateModelProviderStatus,
  type ModelProviderListItem
} from "@/services/model-providers";

type ProviderFormValues = {
  slug?: string;
  name?: string;
  websiteUrl?: string;
  description?: string;
  status?: string;
  sortOrder?: number;
};

const defaultValues: ProviderFormValues = {
  status: "active",
  sortOrder: 1000
};

const statusLabels: Record<string, string> = {
  active: "启用",
  hidden: "隐藏"
};

const statusColors: Record<string, string> = {
  active: "success",
  hidden: "warning"
};

function toNumber(value: unknown) {
  if (value === undefined || value === null || value === "") {
    return undefined;
  }

  const numericValue = Number(value);
  return Number.isFinite(numericValue) ? numericValue : undefined;
}

export default function ModelProvidersPage() {
  const [items, setItems] = useState<ModelProviderListItem[]>([]);
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);
  const [drawerOpen, setDrawerOpen] = useState(false);
  const [editing, setEditing] = useState<ModelProviderListItem | null>(null);
  const [keyword, setKeyword] = useState("");
  const [statusFilter, setStatusFilter] = useState("all");
  const [selectedProviderIds, setSelectedProviderIds] = useState<Key[]>([]);
  const [form] = Form.useForm<ProviderFormValues>();

  const filteredItems = useMemo(() => {
    const normalizedKeyword = keyword.trim().toLowerCase();

    return items.filter((item) => {
      const keywordMatched = [item.name, item.slug, item.websiteUrl]
        .filter(Boolean)
        .some((value) => String(value).toLowerCase().includes(normalizedKeyword));
      const statusMatched = statusFilter === "all" || item.status === statusFilter;
      return keywordMatched && statusMatched;
    });
  }, [items, keyword, statusFilter]);

  async function load() {
    if (!getAccessToken()) {
      return;
    }

    setLoading(true);

    try {
      const result = await fetchModelProviders(1, 200);
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
    form.setFieldsValue(defaultValues);
    setDrawerOpen(true);
  }

  function openEditDrawer(record: ModelProviderListItem) {
    setEditing(record);
    form.resetFields();
    form.setFieldsValue({
      ...defaultValues,
      ...record
    });
    setDrawerOpen(true);
  }

  async function submit(values: ProviderFormValues) {
    const payload = {
      slug: values.slug,
      name: values.name ?? "",
      websiteUrl: values.websiteUrl,
      description: values.description,
      status: values.status ?? "active",
      sortOrder: toNumber(values.sortOrder) ?? 1000
    };

    if (editing) {
      await updateModelProvider(editing.id, payload);
      message.success("提供商已更新");
    } else {
      await createModelProvider(payload);
      message.success("提供商已创建");
    }

    setDrawerOpen(false);
    setEditing(null);
    await load();
    return true;
  }

  async function changeStatus(record: ModelProviderListItem) {
    const targetStatus = record.status === "active" ? "hidden" : "active";
    await updateModelProviderStatus(record.id, targetStatus);
    message.success(`提供商状态已切换为 ${statusLabels[targetStatus]}`);
    await load();
  }

  async function deleteSingleProvider(record: ModelProviderListItem) {
    await deleteModelProvider(record.id);
    message.success("提供商已删除");
    setSelectedProviderIds((current) => current.filter((id) => id !== record.id));
    await load();
  }

  async function batchUpdateProviders(status: string) {
    const ids = selectedProviderIds.map(Number);
    if (!ids.length) {
      return;
    }

    await Promise.all(ids.map((id) => updateModelProviderStatus(id, status)));
    message.success(`已更新 ${ids.length} 个提供商`);
    setSelectedProviderIds([]);
    await load();
  }

  async function batchDeleteProviders() {
    const ids = selectedProviderIds.map(Number);
    if (!ids.length) {
      return;
    }

    await Promise.all(ids.map((id) => deleteModelProvider(id)));
    message.success(`已删除 ${ids.length} 个提供商`);
    setSelectedProviderIds([]);
    await load();
  }

  useEffect(() => {
    void load();
  }, []);

  return (
    <AuthGuard>
      <AdminPage
        title="模型提供商"
        subtitle="维护 OpenAI、Anthropic、Google、DeepSeek 等上游模型提供商，并与模型目录关联。"
        extra={[
          <Button key="create" type="primary" onClick={openCreateDrawer}>
            新增提供商
          </Button>,
          <Button key="refresh" onClick={load}>
            刷新数据
          </Button>
        ]}
        metrics={[
          { label: "全部提供商", value: items.length, note: "模型主数据来源" },
          { label: "启用", value: items.filter((item) => item.status === "active").length, note: "可关联到模型" },
          { label: "隐藏", value: items.filter((item) => item.status === "hidden").length, note: "暂不使用" }
        ]}
      >
        <ProCard className="cheapai-admin-card" title="提供商列表">
          <Space wrap style={{ marginBottom: 16 }}>
            <Input.Search
              allowClear
              placeholder="搜索名称、Slug、官网"
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
                { label: "隐藏", value: "hidden" }
              ]}
            />
            <Button disabled={!selectedProviderIds.length} onClick={() => batchUpdateProviders("active")}>
              批量启用
            </Button>
            <Button disabled={!selectedProviderIds.length} onClick={() => batchUpdateProviders("hidden")}>
              批量隐藏
            </Button>
            <Popconfirm title={`确认删除选中的 ${selectedProviderIds.length} 个提供商？`} onConfirm={batchDeleteProviders}>
              <Button danger disabled={!selectedProviderIds.length}>
                批量删除
              </Button>
            </Popconfirm>
          </Space>

          {error ? <Alert type="warning" showIcon message={`提供商接口暂不可用：${error}`} style={{ marginBottom: 16 }} /> : null}

          <Table
            className="cheapai-admin-table"
            loading={loading}
            rowKey="id"
            dataSource={filteredItems}
            pagination={{ pageSize: 12 }}
            rowSelection={{
              selectedRowKeys: selectedProviderIds,
              onChange: setSelectedProviderIds
            }}
            columns={[
              {
                title: "提供商",
                dataIndex: "name",
                render: (_, record) => (
                  <div>
                    <strong>{record.name}</strong>
                    <div style={{ color: "var(--cheapai-admin-muted)", fontSize: 12 }}>{record.slug}</div>
                  </div>
                )
              },
              { title: "官网", dataIndex: "websiteUrl", ellipsis: true, render: (value?: string) => value || "-" },
              { title: "排序", dataIndex: "sortOrder", width: 90 },
              {
                title: "状态",
                dataIndex: "status",
                width: 100,
                render: (status: string) => <Tag color={statusColors[status] ?? "default"}>{statusLabels[status] ?? status}</Tag>
              },
              {
                title: "操作",
                width: 160,
                render: (_, record) => (
                  <Space>
                    <Button type="link" onClick={() => openEditDrawer(record)}>
                      编辑
                    </Button>
                    <Popconfirm
                      title={record.status === "active" ? "确认隐藏该提供商？" : "确认启用该提供商？"}
                      onConfirm={() => changeStatus(record)}
                    >
                      <Button type="link">{record.status === "active" ? "隐藏" : "启用"}</Button>
                    </Popconfirm>
                    <Popconfirm title="确认删除该提供商？删除后列表不再显示。" onConfirm={() => deleteSingleProvider(record)}>
                      <Button type="link" danger>
                        删除
                      </Button>
                    </Popconfirm>
                  </Space>
                )
              }
            ]}
          />
        </ProCard>

        <DrawerForm<ProviderFormValues>
          form={form}
          open={drawerOpen}
          title={editing ? "编辑提供商" : "新增提供商"}
          width={560}
          drawerProps={{
            destroyOnClose: false,
            onClose: () => {
              setDrawerOpen(false);
              setEditing(null);
            }
          }}
          submitter={{
            searchConfig: {
              submitText: editing ? "保存更新" : "创建提供商"
            }
          }}
          onFinish={submit}
        >
          <ProFormText name="name" label="提供商名称" rules={[{ required: true, message: "请输入提供商名称" }]} />
          <ProFormText name="slug" label="Slug" />
          <ProFormText name="websiteUrl" label="官网地址" />
          <ProFormTextArea name="description" label="说明" fieldProps={{ rows: 4 }} />
          <ProFormSelect
            name="status"
            label="状态"
            options={[
              { label: "启用", value: "active" },
              { label: "隐藏", value: "hidden" }
            ]}
          />
          <ProFormDigit name="sortOrder" label="排序值" min={0} max={999999} fieldProps={{ precision: 0 }} />
        </DrawerForm>
      </AdminPage>
    </AuthGuard>
  );
}
