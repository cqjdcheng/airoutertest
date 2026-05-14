import { PlusOutlined, SearchOutlined } from "@ant-design/icons";
import { useEffect, useMemo, useState } from "react";
import {
  ProCard,
  ProForm,
  ProFormSwitch,
  ProFormText,
  ProFormTextArea
} from "@ant-design/pro-components";
import { Alert, Button, Divider, Empty, Form, Input, Modal, Popconfirm, Select, Space, Table, Tag, message } from "antd";
import AuthGuard from "@/components/AuthGuard";
import AdminPage from "@/components/AdminPage";
import { fetchModels, previewModelImport, type ModelListItem } from "@/services/models";
import {
  createSite,
  fetchSite,
  fetchSites,
  previewSitePricing,
  updateSite,
  updateSiteStatus,
  type RelaySiteDetail,
  type RelaySiteListItem,
  type RelaySiteOffer,
  type RelayPricingPreviewItem
} from "@/services/sites";
import SiteDetailContent from "./SiteDetailContent";

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
  offers?: RelaySiteOffer[];
};

const defaultSiteValues: SiteFormValues = {
  supportsRefund: false,
  supportsInvoice: false,
  hasDocs: false,
  offers: []
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

const offerStatusOptions = [
  { label: "有效", value: "active" },
  { label: "隐藏", value: "hidden" },
  { label: "归档", value: "archived" }
];

function toNumber(value: unknown) {
  if (value === undefined || value === null || value === "") {
    return undefined;
  }

  const numericValue = Number(value);
  return Number.isFinite(numericValue) ? numericValue : undefined;
}

function normalizeOffers(offers?: RelaySiteOffer[]) {
  return (offers ?? [])
    .filter((offer) => offer.modelId || offer.officialModelId || offer.displayName)
    .map((offer) => ({
      modelId: offer.modelId,
      modelSlug: offer.modelSlug,
      vendor: offer.vendor?.trim() || "Custom",
      officialModelId: offer.officialModelId?.trim(),
      displayName: offer.displayName?.trim() || offer.officialModelId?.trim(),
      officialInputPriceUsd: toNumber(offer.officialInputPriceUsd),
      officialOutputPriceUsd: toNumber(offer.officialOutputPriceUsd),
      siteInputPriceUsd: toNumber(offer.siteInputPriceUsd),
      siteOutputPriceUsd: toNumber(offer.siteOutputPriceUsd),
      rechargeRatio: toNumber(offer.rechargeRatio) ?? 1,
      bonusRatio: toNumber(offer.bonusRatio) ?? 0,
      sourceType: offer.sourceType || "manual",
      status: offer.status || "active"
    }));
}

export default function SitesPage() {
  const [items, setItems] = useState<RelaySiteListItem[]>([]);
  const [models, setModels] = useState<ModelListItem[]>([]);
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);
  const [formOpen, setFormOpen] = useState(false);
  const [formLoading, setFormLoading] = useState(false);
  const [formSubmitting, setFormSubmitting] = useState(false);
  const [modelImporting, setModelImporting] = useState(false);
  const [pricingImporting, setPricingImporting] = useState(false);
  const [editing, setEditing] = useState<RelaySiteDetail | null>(null);
  const [detailOpen, setDetailOpen] = useState(false);
  const [detailLoading, setDetailLoading] = useState(false);
  const [detailError, setDetailError] = useState("");
  const [detailId, setDetailId] = useState<number>();
  const [detail, setDetail] = useState<RelaySiteDetail | null>(null);
  const [keyword, setKeyword] = useState("");
  const [statusFilter, setStatusFilter] = useState<string>("all");
  const [importVendor, setImportVendor] = useState("Custom");
  const [importApiKey, setImportApiKey] = useState("");
  const [pricingProviderType, setPricingProviderType] = useState("auto");
  const [pricingRechargeRatio, setPricingRechargeRatio] = useState(1);
  const [pricingBonusRatio, setPricingBonusRatio] = useState(0);
  const [pricingRateBaseline, setPricingRateBaseline] = useState(0.002);
  const [pricingGroupId, setPricingGroupId] = useState("");
  const [form] = Form.useForm<SiteFormValues>();

  const modelOptions = useMemo(
    () =>
      models.map((model) => ({
        label: `${model.displayName} / ${model.officialModelId}`,
        value: model.id
      })),
    [models]
  );

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
      const [siteResult, modelResult] = await Promise.all([fetchSites(1, 80), fetchModels(1, 200)]);
      setItems(siteResult.items);
      setModels(modelResult.items);
      setError("");
    } catch (requestError) {
      setError((requestError as Error).message);
    } finally {
      setLoading(false);
    }
  }

  function openCreateModal() {
    setEditing(null);
    setImportApiKey("");
    setPricingProviderType("auto");
    setPricingRechargeRatio(1);
    setPricingBonusRatio(0);
    setPricingRateBaseline(0.002);
    setPricingGroupId("");
    form.resetFields();
    form.setFieldsValue(defaultSiteValues);
    setFormOpen(true);
  }

  async function openEditModal(record: RelaySiteListItem) {
    setFormLoading(true);

    try {
      const detail = await fetchSite(record.id);
      setEditing(detail);
      setImportApiKey("");
      setPricingProviderType("auto");
      setPricingRechargeRatio(1);
      setPricingBonusRatio(0);
      setPricingRateBaseline(0.002);
      setPricingGroupId("");
      form.resetFields();
      form.setFieldsValue({
        ...defaultSiteValues,
        ...detail,
        offers: detail.offers?.map((offer) => ({ ...offer, displayName: offer.displayName }))
      });
      setFormOpen(true);
    } catch (requestError) {
      message.error((requestError as Error).message);
    } finally {
      setFormLoading(false);
    }
  }

  async function loadDetail(id: number) {
    setDetailLoading(true);

    try {
      setDetail(await fetchSite(id));
      setDetailError("");
    } catch (requestError) {
      setDetail(null);
      setDetailError((requestError as Error).message);
    } finally {
      setDetailLoading(false);
    }
  }

  async function openDetailModal(record: RelaySiteListItem) {
    setDetail(null);
    setDetailError("");
    setDetailId(record.id);
    setDetailOpen(true);
    await loadDetail(record.id);
  }

  function closeFormModal() {
    setFormOpen(false);
    setEditing(null);
  }

  async function submit(values: SiteFormValues) {
    setFormSubmitting(true);

    try {
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
        hasDocs: Boolean(values.hasDocs),
        offers: normalizeOffers(values.offers)
      };

      if (editing) {
        await updateSite(editing.id, payload);
        message.success("站点已更新");
      } else {
        await createSite(payload);
        message.success("站点已创建");
      }

      setFormOpen(false);
      setEditing(null);
      await load();
      return true;
    } finally {
      setFormSubmitting(false);
    }
  }

  async function changeStatus(record: RelaySiteListItem, nextStatus?: string) {
    const targetStatus = nextStatus ?? (record.status === "active" ? "suspended" : "active");
    await updateSiteStatus(record.id, targetStatus);
    message.success(`站点状态已切换为 ${statusLabels[targetStatus] ?? targetStatus}`);
    await load();
  }

  async function importModelsFromEndpoint() {
    const baseUrl = form.getFieldValue("baseUrl");
    if (!baseUrl) {
      message.warning("请先填写 Base URL");
      return;
    }

    setModelImporting(true);
    try {
      const rows = await previewModelImport({
        baseUrl,
        vendor: importVendor,
        apiKey: importApiKey || undefined
      });
      const current = form.getFieldValue("offers") ?? [];
      form.setFieldValue("offers", [
        ...current,
        ...rows.map((row) => ({
          vendor: row.vendor || importVendor,
          officialModelId: row.officialModelId,
          displayName: row.displayName,
          officialInputPriceUsd: row.officialInputPriceUsd,
          officialOutputPriceUsd: row.officialOutputPriceUsd,
          siteInputPriceUsd: row.officialInputPriceUsd,
          siteOutputPriceUsd: row.officialOutputPriceUsd,
          rechargeRatio: 1,
          bonusRatio: 0,
          sourceType: "manual",
          status: "active"
        }))
      ]);
      message.success(`已抓取 ${rows.length} 个模型`);
    } catch (requestError) {
      message.error((requestError as Error).message);
    } finally {
      setModelImporting(false);
    }
  }

  function findMatchingModel(row: RelayPricingPreviewItem) {
    const officialModelId = row.officialModelId.trim().toLowerCase();
    if (!officialModelId) {
      return undefined;
    }

    return models.find((model) => {
      const modelOfficialId = model.officialModelId.toLowerCase();
      return modelOfficialId === officialModelId ||
        modelOfficialId.endsWith(`/${officialModelId}`) ||
        officialModelId.endsWith(`/${modelOfficialId}`);
    });
  }

  function mapPricingRowToOffer(row: RelayPricingPreviewItem): RelaySiteOffer {
    const model = findMatchingModel(row);
    const isPerCall = row.billingType === "times";

    return {
      modelId: model?.id,
      modelSlug: model?.slug,
      vendor: model?.vendor || importVendor || "Custom",
      officialModelId: model?.officialModelId || row.officialModelId,
      displayName: model?.displayName || row.displayName,
      officialInputPriceUsd: model?.officialInputPriceUsd,
      officialOutputPriceUsd: model?.officialOutputPriceUsd,
      siteInputPriceUsd: isPerCall ? row.sitePerCallPriceUsd : row.siteInputPriceUsd,
      siteOutputPriceUsd: isPerCall ? undefined : row.siteOutputPriceUsd,
      rechargeRatio: row.rechargeRatio,
      bonusRatio: row.bonusRatio,
      sourceType: "crawl",
      status: "active"
    };
  }

  async function importPricingFromEndpoint() {
    const baseUrl = form.getFieldValue("baseUrl");
    if (!baseUrl) {
      message.warning("请先填写 Base URL");
      return;
    }

    setPricingImporting(true);
    try {
      const rows = await previewSitePricing({
        baseUrl,
        providerType: pricingProviderType,
        apiKey: importApiKey || undefined,
        rechargeRatio: pricingRechargeRatio,
        bonusRatio: pricingBonusRatio,
        rateBaseline: pricingRateBaseline,
        groupId: pricingGroupId || undefined
      });
      const current = form.getFieldValue("offers") ?? [];
      form.setFieldValue("offers", [
        ...current,
        ...rows.map(mapPricingRowToOffer)
      ]);
      message.success(`已抓取 ${rows.length} 个模型价格`);
    } catch (requestError) {
      message.error((requestError as Error).message);
    } finally {
      setPricingImporting(false);
    }
  }

  function applyModelToOffer(rowIndex: number, modelId: number) {
    const model = models.find((item) => item.id === modelId);
    if (!model) {
      return;
    }

    form.setFieldValue(["offers", rowIndex, "modelId"], model.id);
    form.setFieldValue(["offers", rowIndex, "modelSlug"], model.slug);
    form.setFieldValue(["offers", rowIndex, "vendor"], model.vendor);
    form.setFieldValue(["offers", rowIndex, "officialModelId"], model.officialModelId);
    form.setFieldValue(["offers", rowIndex, "displayName"], model.displayName);
    form.setFieldValue(["offers", rowIndex, "officialInputPriceUsd"], model.officialInputPriceUsd);
    form.setFieldValue(["offers", rowIndex, "officialOutputPriceUsd"], model.officialOutputPriceUsd);
  }

  useEffect(() => {
    void load();
  }, []);

  return (
    <AuthGuard>
      <AdminPage
        title="中转站点"
        subtitle="维护站点基础档案、可售模型与价格，报价保存后可用于后续排行重建。"
        extra={[
          <Button key="create" type="primary" onClick={openCreateModal}>
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
                width: 300,
                render: (_, record) => (
                  <Space>
                    <Button type="link" onClick={() => openDetailModal(record)}>
                      详情
                    </Button>
                    <Button type="link" onClick={() => openEditModal(record)}>
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

        {formOpen ? (
          <Modal
            open
            title={editing ? "编辑站点" : "新增站点"}
            width={960}
            maskClosable={false}
            onCancel={closeFormModal}
            cancelText="取消"
            okText={editing ? "保存更新" : "创建站点"}
            confirmLoading={formSubmitting}
            onOk={() => form.submit()}
          >
            <ProCard loading={formLoading} bordered={false}>
              <ProForm<SiteFormValues> form={form} submitter={false} onFinish={submit}>
              <ProFormText name="name" label="站点名称" rules={[{ required: true, message: "请输入站点名称" }]} />
              <ProFormText name="slug" label="Slug" />
              <ProFormText name="baseUrl" label="Base URL" rules={[{ required: true, message: "请输入 Base URL" }]} />
              <ProFormText name="websiteUrl" label="官网地址" />
              <ProFormTextArea name="description" label="站点说明" fieldProps={{ rows: 3 }} />
              <ProFormText name="docsUrl" label="文档地址" />
              <ProFormText name="inviteUrl" label="邀请链接" />
              <ProFormTextArea name="recentReview" label="近期体验" fieldProps={{ rows: 3 }} />
              <Space wrap>
                <ProFormSwitch name="supportsRefund" label="支持退款" />
                <ProFormSwitch name="supportsInvoice" label="支持发票" />
                <ProFormSwitch name="hasDocs" label="提供文档" />
              </Space>

              <Divider orientation="left">模型与价格</Divider>
              <Space wrap style={{ marginBottom: 16 }}>
                <Input value={importVendor} onChange={(event) => setImportVendor(event.target.value)} placeholder="抓取厂商名" style={{ width: 160 }} />
                <Input.Password value={importApiKey} onChange={(event) => setImportApiKey(event.target.value)} placeholder="API Key，不保存" style={{ width: 260 }} />
                <Button icon={<SearchOutlined />} loading={modelImporting} onClick={importModelsFromEndpoint}>
                  从 Base URL 抓取模型
                </Button>
              </Space>

              <Space wrap style={{ marginBottom: 16 }}>
                <Select
                  value={pricingProviderType}
                  style={{ width: 140 }}
                  onChange={setPricingProviderType}
                  options={[
                    { label: "自动识别", value: "auto" },
                    { label: "NewAPI", value: "newapi" },
                    { label: "OneAPI", value: "oneapi" },
                    { label: "OneHub", value: "onehub" }
                  ]}
                />
                <Input
                  type="number"
                  step="0.000001"
                  value={pricingRateBaseline}
                  onChange={(event) => setPricingRateBaseline(toNumber(event.target.value) ?? 0.002)}
                  placeholder="基准价/1K"
                  style={{ width: 190 }}
                />
                <Input
                  type="number"
                  step="0.01"
                  value={pricingRechargeRatio}
                  onChange={(event) => setPricingRechargeRatio(toNumber(event.target.value) ?? 1)}
                  placeholder="充值倍率"
                  style={{ width: 170 }}
                />
                <Input
                  type="number"
                  step="0.01"
                  value={pricingBonusRatio}
                  onChange={(event) => setPricingBonusRatio(toNumber(event.target.value) ?? 0)}
                  placeholder="赠送倍率"
                  style={{ width: 170 }}
                />
                <Input
                  value={pricingGroupId}
                  onChange={(event) => setPricingGroupId(event.target.value)}
                  placeholder="用户组，可留空取最低价"
                  style={{ width: 190 }}
                />
                <Button icon={<SearchOutlined />} loading={pricingImporting} onClick={importPricingFromEndpoint}>
                  按 one-tracker 抓取价格
                </Button>
              </Space>

              <Form.List name="offers">
                {(fields, { add, remove }) => (
                  <Space direction="vertical" size={12} style={{ width: "100%" }}>
                    {fields.map((field) => (
                      <ProCard key={field.key} bordered size="small">
                        <Space direction="vertical" size={12} style={{ width: "100%" }}>
                          <Space wrap align="start">
                            <Form.Item label="选择已有模型" name={[field.name, "modelId"]} style={{ width: 260 }}>
                              <Select
                                allowClear
                                showSearch
                                optionFilterProp="label"
                                options={modelOptions}
                                onChange={(value) => value && applyModelToOffer(field.name, value)}
                              />
                            </Form.Item>
                            <Form.Item label="展示名称" name={[field.name, "displayName"]} rules={[{ required: true, message: "请输入模型名称" }]} style={{ width: 200 }}>
                              <Input />
                            </Form.Item>
                            <Form.Item label="Vendor" name={[field.name, "vendor"]} style={{ width: 140 }}>
                              <Input />
                            </Form.Item>
                            <Form.Item label="Official ID" name={[field.name, "officialModelId"]} rules={[{ required: true, message: "请输入 Official ID" }]} style={{ width: 200 }}>
                              <Input />
                            </Form.Item>
                          </Space>
                          <Space wrap align="start">
                            <Form.Item label="官方输入价" name={[field.name, "officialInputPriceUsd"]} style={{ width: 140 }}>
                              <Input type="number" step="0.000001" />
                            </Form.Item>
                            <Form.Item label="官方输出价" name={[field.name, "officialOutputPriceUsd"]} style={{ width: 140 }}>
                              <Input type="number" step="0.000001" />
                            </Form.Item>
                            <Form.Item label="站点输入价" name={[field.name, "siteInputPriceUsd"]} style={{ width: 140 }}>
                              <Input type="number" step="0.000001" />
                            </Form.Item>
                            <Form.Item label="站点输出价" name={[field.name, "siteOutputPriceUsd"]} style={{ width: 140 }}>
                              <Input type="number" step="0.000001" />
                            </Form.Item>
                            <Form.Item label="充值倍率" name={[field.name, "rechargeRatio"]} style={{ width: 120 }}>
                              <Input type="number" step="0.01" />
                            </Form.Item>
                            <Form.Item label="赠送倍率" name={[field.name, "bonusRatio"]} style={{ width: 120 }}>
                              <Input type="number" step="0.01" />
                            </Form.Item>
                            <Form.Item label="状态" name={[field.name, "status"]} style={{ width: 120 }}>
                              <Select options={offerStatusOptions} />
                            </Form.Item>
                            <Button danger onClick={() => remove(field.name)}>
                              删除
                            </Button>
                          </Space>
                        </Space>
                      </ProCard>
                    ))}
                    <Button icon={<PlusOutlined />} onClick={() => add({ rechargeRatio: 1, bonusRatio: 0, sourceType: "manual", status: "active", vendor: importVendor })}>
                      手工新增模型价格
                    </Button>
                  </Space>
                )}
              </Form.List>
              </ProForm>
            </ProCard>
          </Modal>
        ) : null}

        {detailOpen ? (
          <Modal
            open
            title={detail ? `站点详情：${detail.name}` : "站点详情"}
            width={1080}
            onCancel={() => {
              setDetailOpen(false);
              setDetail(null);
              setDetailError("");
              setDetailId(undefined);
            }}
            cancelText="关闭"
            okText="刷新"
            okButtonProps={{ loading: detailLoading, disabled: !detailId }}
            onOk={() => {
              if (detailId) {
                void loadDetail(detailId);
              }
            }}
          >
            {detailError ? <Alert type="warning" showIcon message={detailError} style={{ marginBottom: 16 }} /> : null}
            <ProCard className="cheapai-admin-card" loading={detailLoading}>
              {detail ? <SiteDetailContent detail={detail} /> : <Empty description={detailLoading ? "正在加载站点详情" : "未找到站点"} />}
            </ProCard>
          </Modal>
        ) : null}
      </AdminPage>
    </AuthGuard>
  );
}
