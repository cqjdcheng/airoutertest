import { CloudDownloadOutlined } from "@ant-design/icons";
import { useEffect, useMemo, useState, type Key } from "react";
import {
  ModalForm,
  ProCard,
  ProFormDigit,
  ProFormSelect,
  ProFormSwitch,
  ProFormText,
  ProFormTextArea
} from "@ant-design/pro-components";
import { Alert, Button, Form, Input, InputNumber, Modal, Popconfirm, Select, Space, Switch, Table, Tag, message } from "antd";
import AuthGuard from "@/components/AuthGuard";
import AdminPage from "@/components/AdminPage";
import {
  createModel,
  fetchModel,
  fetchModels,
  importModels,
  previewOpenRouterModels,
  updateModel,
  updateModelMetadata,
  updateModelStatus,
  type ModelImportPreviewItem,
  type ModelListItem
} from "@/services/models";
import { getAccessToken } from "@/services/api";
import { fetchActiveModelProviders, type ModelProviderListItem } from "@/services/model-providers";

type ApiType = "openai" | "anthropic";

type ModelFormValues = {
  providerId?: number;
  slug?: string;
  vendor?: string;
  officialModelId?: string;
  requestName?: string;
  apiType?: ApiType;
  displayName?: string;
  description?: string;
  status?: string;
  isHot?: boolean;
  sortOrder?: number;
  officialInputPriceUsd?: number;
  officialOutputPriceUsd?: number;
  capabilityScore?: number;
  capabilitySource?: string;
};

const defaultModelValues: ModelFormValues = {
  status: "active",
  apiType: "openai",
  isHot: false,
  sortOrder: 1000
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

function toNumber(value: unknown) {
  if (value === undefined || value === null || value === "") {
    return undefined;
  }

  const numericValue = Number(value);
  return Number.isFinite(numericValue) ? numericValue : undefined;
}

function normalizeRequestName(requestName?: string, officialModelId?: string) {
  const source = (requestName || officialModelId || "").trim();
  const parts = source.split("/").filter(Boolean);
  return (parts.at(-1) ?? source).replace(/^~+/, "");
}

export default function ModelsPage() {
  const [items, setItems] = useState<ModelListItem[]>([]);
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);
  const [modalOpen, setModalOpen] = useState(false);
  const [modalLoading, setModalLoading] = useState(false);
  const [importOpen, setImportOpen] = useState(false);
  const [importLoading, setImportLoading] = useState(false);
  const [importRows, setImportRows] = useState<ModelImportPreviewItem[]>([]);
  const [selectedImportIds, setSelectedImportIds] = useState<Key[]>([]);
  const [editing, setEditing] = useState<ModelListItem | null>(null);
  const [keyword, setKeyword] = useState("");
  const [statusFilter, setStatusFilter] = useState("all");
  const [providers, setProviders] = useState<ModelProviderListItem[]>([]);
  const [form] = Form.useForm<ModelFormValues>();

  const filteredItems = useMemo(() => {
    const normalizedKeyword = keyword.trim().toLowerCase();

    return items.filter((item) => {
      const keywordMatched = [
        item.displayName,
        item.slug,
        item.vendor,
        item.providerName,
        item.officialModelId,
        item.requestName,
        item.apiType
      ]
        .filter(Boolean)
        .some((value) => String(value).toLowerCase().includes(normalizedKeyword));
      const statusMatched = statusFilter === "all" || item.status === statusFilter;
      return keywordMatched && statusMatched;
    });
  }, [items, keyword, statusFilter]);

  async function load() {
    setLoading(true);

    try {
      const result = await fetchModels(1, 200);
      setItems(result.items);
      setError("");
    } catch (requestError) {
      setError((requestError as Error).message);
    } finally {
      setLoading(false);
    }
  }

  async function loadProviders() {
    if (!getAccessToken()) {
      return;
    }

    try {
      const result = await fetchActiveModelProviders();
      setProviders(result);
    } catch (requestError) {
      message.warning(`模型供应商加载失败：${(requestError as Error).message}`);
    }
  }

  function openCreateModal() {
    setEditing(null);
    form.resetFields();
    form.setFieldsValue(defaultModelValues);
    setModalOpen(true);
  }

  async function openEditModal(record: ModelListItem) {
    setModalLoading(true);

    try {
      const detail = await fetchModel(record.id);
      setEditing(detail);
      form.resetFields();
      form.setFieldsValue({
        ...defaultModelValues,
        ...detail,
        providerId: detail.providerId
      });
      setModalOpen(true);
    } finally {
      setModalLoading(false);
    }
  }

  async function submit(values: ModelFormValues) {
    const officialModelId = values.officialModelId?.trim() ?? "";
    const requestName = normalizeRequestName(values.requestName, officialModelId);
    const payload = {
      providerId: values.providerId,
      slug: values.slug,
      vendor: values.vendor ?? "",
      officialModelId,
      requestName,
      apiType: values.apiType ?? "openai",
      displayName: values.displayName?.trim() ?? "",
      description: values.description,
      officialInputPriceUsd: toNumber(values.officialInputPriceUsd),
      officialOutputPriceUsd: toNumber(values.officialOutputPriceUsd),
      status: values.status ?? "active",
      isHot: Boolean(values.isHot),
      sortOrder: toNumber(values.sortOrder) ?? 1000,
      capabilityScore: toNumber(values.capabilityScore),
      capabilitySource: values.capabilitySource
    };

    if (editing) {
      await updateModel(editing.id, payload);
      message.success("模型已更新");
    } else {
      await createModel(payload);
      message.success("模型已创建");
    }

    setModalOpen(false);
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

  async function saveMetadata(record: ModelListItem, patch: Partial<Pick<ModelListItem, "isHot" | "sortOrder">>) {
    const next = {
      isHot: patch.isHot ?? record.isHot,
      sortOrder: patch.sortOrder ?? record.sortOrder
    };

    setItems((current) => current.map((item) => (item.id === record.id ? { ...item, ...next } : item)));

    try {
      await updateModelMetadata(record.id, next);
      message.success("模型配置已保存");
    } catch (requestError) {
      message.error((requestError as Error).message);
      await load();
    }
  }

  async function previewImport() {
    setImportLoading(true);
    try {
      const rows = await previewOpenRouterModels();
      setImportRows(rows);
      setSelectedImportIds(rows.map((row) => row.officialModelId));
      message.success(`从 OpenRouter 抓取到 ${rows.length} 个模型`);
    } catch (requestError) {
      message.error((requestError as Error).message);
    } finally {
      setImportLoading(false);
    }
  }

  async function submitImport() {
    const selectedRows = importRows.filter((row) => selectedImportIds.includes(row.officialModelId));
    if (!selectedRows.length) {
      message.warning("请选择要导入的模型");
      return;
    }

    setImportLoading(true);
    try {
      const result = await importModels({
        models: selectedRows.map((row, index) => ({
          providerSlug: row.providerSlug,
          providerName: row.providerName,
          vendor: row.vendor || row.providerName,
          officialModelId: row.officialModelId,
          requestName: row.requestName,
          apiType: row.apiType,
          displayName: row.displayName,
          description: row.description,
          status: "active",
          isHot: false,
          sortOrder: 1000 + index,
          officialInputPriceUsd: row.officialInputPriceUsd,
          officialOutputPriceUsd: row.officialOutputPriceUsd,
          capabilityScore: row.capabilityScore,
          capabilitySource: row.capabilitySource
        }))
      });
      message.success(`导入完成：新增 ${result.createdCount}，更新 ${result.updatedCount}`);
      setImportOpen(false);
      await load();
    } catch (requestError) {
      message.error((requestError as Error).message);
    } finally {
      setImportLoading(false);
    }
  }

  useEffect(() => {
    void load();
    void loadProviders();
  }, []);

  return (
    <AuthGuard>
      <AdminPage
        title="模型目录"
        subtitle="维护模型主数据、请求名称、供应商、接口类型、官方价格和首页热门展示。"
        extra={[
          <Button key="import" icon={<CloudDownloadOutlined />} onClick={() => setImportOpen(true)}>
            抓取 OpenRouter
          </Button>,
          <Button key="create" type="primary" onClick={openCreateModal}>
            新增模型
          </Button>,
          <Button key="refresh" onClick={load}>
            刷新数据
          </Button>
        ]}
        metrics={[
          { label: "全部模型", value: items.length, note: "后台模型档案" },
          { label: "公开展示", value: items.filter((item) => item.status === "active").length, note: "参与排行与详情页" },
          { label: "热门模型", value: items.filter((item) => item.isHot).length, note: "用户端首页模型 Tab" },
          { label: "带官方价格", value: items.filter((item) => item.officialInputPriceUsd || item.officialOutputPriceUsd).length, note: "用于价格对比" }
        ]}
      >
        <ProCard className="cheapai-admin-card" title="模型列表">
          <Space wrap style={{ marginBottom: 16 }}>
            <Input.Search
              allowClear
              placeholder="搜索模型、厂商、请求名称、Official ID"
              style={{ width: 360 }}
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

          <Table<ModelListItem>
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
              { title: "供应商", width: 130, render: (_, record) => record.providerName || record.vendor },
              { title: "请求名称", dataIndex: "requestName", ellipsis: true },
              { title: "接口类型", dataIndex: "apiType", width: 100, render: (apiType) => <Tag>{apiType}</Tag> },
              { title: "Official ID", dataIndex: "officialModelId", ellipsis: true },
              {
                title: "官方价格",
                width: 170,
                render: (_, record) => `$${record.officialInputPriceUsd ?? "-"} / $${record.officialOutputPriceUsd ?? "-"}`
              },
              {
                title: "评分",
                width: 90,
                render: (_, record) => record.capabilityScore ?? "-"
              },
              {
                title: "排序",
                dataIndex: "sortOrder",
                width: 110,
                render: (sortOrder, record) => (
                  <InputNumber
                    min={0}
                    max={999999}
                    precision={0}
                    size="small"
                    defaultValue={sortOrder}
                    onBlur={(event) => {
                      const nextValue = toNumber(event.target.value) ?? 1000;
                      if (nextValue !== record.sortOrder) {
                        void saveMetadata(record, { sortOrder: nextValue });
                      }
                    }}
                    onPressEnter={(event) => {
                      event.currentTarget.blur();
                    }}
                  />
                )
              },
              {
                title: "热门",
                dataIndex: "isHot",
                width: 100,
                render: (isHot, record) => (
                  <Switch
                    checked={isHot}
                    checkedChildren="热门"
                    unCheckedChildren="普通"
                    onChange={(checked) => void saveMetadata(record, { isHot: checked })}
                  />
                )
              },
              {
                title: "状态",
                dataIndex: "status",
                width: 100,
                render: (status) => <Tag color={statusColors[status] ?? "default"}>{statusLabels[status] ?? status}</Tag>
              },
              {
                title: "操作",
                width: 220,
                render: (_, record) => (
                  <Space>
                    <Button type="link" onClick={() => openEditModal(record)}>
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

        <ModalForm<ModelFormValues>
          form={form}
          open={modalOpen}
          title={editing ? "编辑模型" : "新增模型"}
          width={680}
          loading={modalLoading}
          modalProps={{
            destroyOnClose: false,
            onCancel: () => {
              setModalOpen(false);
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
          <ProFormText name="displayName" label="模型名称" rules={[{ required: true, message: "请输入模型名称" }]} />
          <ProFormText name="requestName" label="模型请求名称" rules={[{ required: true, message: "请输入模型请求名称" }]} />
          <ProFormSelect
            name="providerId"
            label="模型供应商"
            showSearch
            options={providers.map((provider) => ({
              label: provider.name,
              value: provider.id
            }))}
            placeholder="请选择供应商；旧数据可继续使用 Vendor"
          />
          <ProFormSelect
            name="apiType"
            label="接口类型"
            rules={[{ required: true, message: "请选择接口类型" }]}
            options={[
              { label: "OpenAI", value: "openai" },
              { label: "Anthropic", value: "anthropic" }
            ]}
          />
          <ProFormText name="officialModelId" label="Official ID" rules={[{ required: true, message: "请输入 Official ID" }]} />
          <ProFormText name="vendor" label="兼容 Vendor" />
          <ProFormText name="slug" label="Slug" />
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
          <ProFormSwitch name="isHot" label="首页热门模型" />
          <ProFormDigit name="sortOrder" label="排序值" min={0} max={999999} fieldProps={{ precision: 0 }} />
          <ProFormDigit name="officialInputPriceUsd" label="官方输入价 USD" fieldProps={{ precision: 6 }} />
          <ProFormDigit name="officialOutputPriceUsd" label="官方输出价 USD" fieldProps={{ precision: 6 }} />
          <ProFormDigit name="capabilityScore" label="能力评分" min={0} max={100} fieldProps={{ precision: 2 }} />
          <ProFormText name="capabilitySource" label="评分来源" />
        </ModalForm>

        <Modal
          title="从 OpenRouter 抓取主流模型"
          open={importOpen}
          width={980}
          confirmLoading={importLoading}
          onOk={submitImport}
          onCancel={() => setImportOpen(false)}
          okText="导入选中"
        >
          <Alert
            type="info"
            showIcon
            message="数据来源：OpenRouter 官方 /api/v1/models"
            description="模型名称用于展示，模型请求名称用于真实请求；价格按 USD / 1M tokens 换算，评分基于 OpenRouter 返回的能力字段生成。"
            style={{ marginBottom: 16 }}
          />
          <Space wrap style={{ marginBottom: 16 }}>
            <Button loading={importLoading} onClick={previewImport}>
              抓取 OpenRouter 预览
            </Button>
          </Space>
          <Table<ModelImportPreviewItem>
            rowKey="officialModelId"
            size="small"
            dataSource={importRows}
            pagination={{ pageSize: 8 }}
            rowSelection={{
              selectedRowKeys: selectedImportIds,
              onChange: (keys) => setSelectedImportIds(keys)
            }}
            columns={[
              { title: "模型名称", dataIndex: "displayName" },
              { title: "供应商", dataIndex: "providerName", width: 130 },
              { title: "请求名称", dataIndex: "requestName", ellipsis: true },
              { title: "接口类型", dataIndex: "apiType", width: 100 },
              { title: "Official ID", dataIndex: "officialModelId", ellipsis: true },
              { title: "官方输入价", dataIndex: "officialInputPriceUsd", width: 120, render: (value?: number) => value ?? "-" },
              { title: "官方输出价", dataIndex: "officialOutputPriceUsd", width: 120, render: (value?: number) => value ?? "-" },
              { title: "评分", dataIndex: "capabilityScore", width: 90, render: (value?: number) => value ?? "-" }
            ]}
          />
        </Modal>
      </AdminPage>
    </AuthGuard>
  );
}
