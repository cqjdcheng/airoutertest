import { useEffect, useMemo, useState } from "react";
import {
  DrawerForm,
  ProCard,
  ProFormDigit,
  ProFormSelect,
  ProFormText,
  ProFormTextArea
} from "@ant-design/pro-components";
import { Alert, Button, Form, Input, Popconfirm, Select, Space, Table, Tag, message } from "antd";
import AuthGuard from "@/components/AuthGuard";
import AdminPage from "@/components/AdminPage";
import {
  createModel,
  fetchModel,
  fetchModels,
  updateModel,
  updateModelStatus,
  type ModelListItem
} from "@/services/models";

type ModelFormValues = {
  slug?: string;
  vendor?: string;
  officialModelId?: string;
  displayName?: string;
  description?: string;
  status?: string;
  officialInputPriceUsd?: number;
  officialOutputPriceUsd?: number;
};

const defaultModelValues: ModelFormValues = {
  status: "active"
};

const statusLabels: Record<string, string> = {
  active: "启用",
  hidden: "隐藏",
  deprecated: "弃用"
};

const statusColors: Record<string, string> = {
  active: "success",
  hidden: "warning",
  deprecated: "default"
};

export default function ModelsPage() {
  const [items, setItems] = useState<ModelListItem[]>([]);
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);
  const [drawerOpen, setDrawerOpen] = useState(false);
  const [drawerLoading, setDrawerLoading] = useState(false);
  const [editing, setEditing] = useState<ModelListItem | null>(null);
  const [keyword, setKeyword] = useState("");
  const [statusFilter, setStatusFilter] = useState("all");
  const [form] = Form.useForm<ModelFormValues>();

  const filteredItems = useMemo(() => {
    const normalizedKeyword = keyword.trim().toLowerCase();

    return items.filter((item) => {
      const keywordMatched = [item.displayName, item.slug, item.vendor, item.officialModelId]
        .filter(Boolean)
        .some((value) => String(value).toLowerCase().includes(normalizedKeyword));
      const statusMatched = statusFilter === "all" || item.status === statusFilter;
      return keywordMatched && statusMatched;
    });
  }, [items, keyword, statusFilter]);

  async function load() {
    setLoading(true);

    try {
      const result = await fetchModels(1, 80);
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
    form.setFieldsValue(defaultModelValues);
    setDrawerOpen(true);
  }

  async function openEditDrawer(record: ModelListItem) {
    setDrawerLoading(true);

    try {
      const detail = await fetchModel(record.id);
      setEditing(detail);
      form.resetFields();
      form.setFieldsValue({
        ...defaultModelValues,
        ...detail
      });
      setDrawerOpen(true);
    } finally {
      setDrawerLoading(false);
    }
  }

  async function submit(values: ModelFormValues) {
    const payload = {
      slug: values.slug,
      vendor: values.vendor ?? "",
      officialModelId: values.officialModelId ?? "",
      displayName: values.displayName ?? "",
      description: values.description,
      officialInputPriceUsd: values.officialInputPriceUsd,
      officialOutputPriceUsd: values.officialOutputPriceUsd,
      status: values.status ?? "active"
    };

    if (editing) {
      await updateModel(editing.id, payload);
      message.success("模型已更新");
    } else {
      await createModel(payload);
      message.success("模型已创建");
    }

    setDrawerOpen(false);
    setEditing(null);
    await load();
    return true;
  }

  async function changeStatus(record: ModelListItem, nextStatus?: string) {
    const targetStatus = nextStatus ?? (record.status === "active" ? "hidden" : "active");
    await updateModelStatus(record.id, targetStatus);
    message.success(`模型状态已切换为 ${statusLabels[targetStatus] ?? targetStatus}`);
    await load();
  }

  useEffect(() => {
    void load();
  }, []);

  return (
    <AuthGuard>
      <AdminPage
        title="模型目录"
        subtitle="模型主数据以目录页方式集中维护，新增和编辑改为抽屉处理，避免为单条记录打断整页上下文。"
        extra={[
          <Button key="create" type="primary" onClick={openCreateDrawer}>
            新增模型
          </Button>,
          <Button key="refresh" onClick={load}>
            刷新数据
          </Button>
        ]}
        metrics={[
          { label: "全部模型", value: items.length, note: "后台模型档案" },
          { label: "公开展示", value: items.filter((item) => item.status === "active").length, note: "参与排行与详情页" },
          { label: "厂商数量", value: new Set(items.map((item) => item.vendor)).size, note: "按 Vendor 聚合" },
          { label: "带官方价格", value: items.filter((item) => item.officialInputPriceUsd || item.officialOutputPriceUsd).length, note: "用于价格对比" }
        ]}
      >
        <ProCard className="cheapai-admin-card" title="模型列表">
          <Space wrap style={{ marginBottom: 16 }}>
            <Input.Search
              allowClear
              placeholder="搜索模型、厂商、Official ID"
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
                { label: "隐藏", value: "hidden" },
                { label: "弃用", value: "deprecated" }
              ]}
            />
          </Space>

          {error ? <Alert type="warning" showIcon message={`模型接口暂不可用：${error}`} style={{ marginBottom: 16 }} /> : null}

          <Table
            className="cheapai-admin-table"
            loading={loading}
            rowKey="id"
            dataSource={filteredItems}
            pagination={{ pageSize: 12 }}
            columns={[
              {
                title: "模型",
                dataIndex: "displayName",
                render: (_, record) => (
                  <div>
                    <strong>{record.displayName}</strong>
                    <div style={{ color: "var(--cheapai-admin-muted)", fontSize: 12 }}>{record.slug}</div>
                  </div>
                )
              },
              { title: "Vendor", dataIndex: "vendor", width: 140 },
              { title: "Official ID", dataIndex: "officialModelId", ellipsis: true },
              {
                title: "官方价格",
                width: 180,
                render: (_, record) => `$${record.officialInputPriceUsd ?? "-"} / $${record.officialOutputPriceUsd ?? "-"}`
              },
              {
                title: "状态",
                dataIndex: "status",
                width: 110,
                render: (status: string) => <Tag color={statusColors[status] ?? "default"}>{statusLabels[status] ?? status}</Tag>
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
                      title={record.status === "active" ? "确认隐藏该模型？" : "确认启用该模型？"}
                      onConfirm={() => changeStatus(record)}
                    >
                      <Button type="link">{record.status === "active" ? "隐藏" : "启用"}</Button>
                    </Popconfirm>
                    <Popconfirm title="确认标记为弃用？" onConfirm={() => changeStatus(record, "deprecated")}>
                      <Button type="link" danger>
                        弃用
                      </Button>
                    </Popconfirm>
                  </Space>
                )
              }
            ]}
          />
        </ProCard>

        <DrawerForm<ModelFormValues>
          form={form}
          open={drawerOpen}
          title={editing ? "编辑模型" : "新增模型"}
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
              submitText: editing ? "保存更新" : "创建模型"
            }
          }}
          onFinish={submit}
        >
          <ProFormText name="displayName" label="展示名称" rules={[{ required: true, message: "请输入展示名称" }]} />
          <ProFormText name="slug" label="Slug" />
          <ProFormText name="vendor" label="Vendor" rules={[{ required: true, message: "请输入 Vendor" }]} />
          <ProFormText name="officialModelId" label="Official ID" rules={[{ required: true, message: "请输入 Official ID" }]} />
          <ProFormTextArea name="description" label="模型说明" fieldProps={{ rows: 4 }} />
          <ProFormSelect
            name="status"
            label="状态"
            options={[
              { label: "启用", value: "active" },
              { label: "隐藏", value: "hidden" },
              { label: "弃用", value: "deprecated" }
            ]}
          />
          <ProFormDigit name="officialInputPriceUsd" label="官方输入价 USD" fieldProps={{ precision: 6 }} />
          <ProFormDigit name="officialOutputPriceUsd" label="官方输出价 USD" fieldProps={{ precision: 6 }} />
        </DrawerForm>
      </AdminPage>
    </AuthGuard>
  );
}
