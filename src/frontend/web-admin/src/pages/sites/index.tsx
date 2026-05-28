import { PlusOutlined, SearchOutlined } from "@ant-design/icons";
import { useEffect, useMemo, useState, type Key } from "react";
import {
  ProCard,
  ProForm,
  ProFormSwitch,
  ProFormText,
  ProFormTextArea
} from "@ant-design/pro-components";
import { Alert, Button, Empty, Form, Input, Modal, Popconfirm, Select, Space, Switch, Table, Tabs, Tag, message } from "antd";
import AuthGuard from "@/components/AuthGuard";
import AdminPage from "@/components/AdminPage";
import { fetchModels, previewModelImport, type ModelImportPreviewItem, type ModelListItem } from "@/services/models";
import {
  createSiteOffer,
  createSite,
  deleteSite,
  deleteSiteOffer,
  fetchSite,
  fetchSiteOffers,
  fetchSites,
  previewSitePricing,
  updateSite,
  updateSiteOffer,
  updateSiteStatus,
  type RelaySiteDetail,
  type RelaySiteListItem,
  type RelaySiteOffer,
  type RelayPricingPreviewItem
} from "@/services/sites";
import SiteDetailContent from "./SiteDetailContent";
import { formatBeijingTime } from "@/utils/time";

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
  autoTestEnabled?: boolean;
  testApiKey?: string;
  testIntervalMinutes?: number;
  rechargeRatio?: number;
  bonusRatio?: number;
  offers?: RelaySiteOffer[];
};

type OfferTableRow = RelaySiteOffer & {
  rowIndex: number;
};

type OfferImportPreviewRow = RelaySiteOffer & {
  previewKey: string;
  importSource: "model" | "pricing";
};

const defaultSiteValues: SiteFormValues = {
  supportsRefund: false,
  supportsInvoice: false,
  hasDocs: false,
  autoTestEnabled: false,
  testIntervalMinutes: 60,
  rechargeRatio: 1,
  bonusRatio: 0,
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
  { label: "隐藏", value: "hidden" }
];

const offerStatusFilterOptions = [
  { label: "有效模型", value: "active" },
  { label: "隐藏模型", value: "hidden" },
  { label: "全部模型", value: "all" }
];

function toNumber(value: unknown) {
  if (value === undefined || value === null || value === "") {
    return undefined;
  }

  const numericValue = Number(value);
  return Number.isFinite(numericValue) ? numericValue : undefined;
}

function normalizeOffers(offers: RelaySiteOffer[] | undefined, models: ModelListItem[], rechargeRatio: number, bonusRatio: number) {
  return (offers ?? [])
    .filter((offer) => offer.modelId || offer.officialModelId || offer.requestName || offer.displayName)
    .map((offer) => {
      const model = offer.modelId ? models.find((item) => item.id === offer.modelId) : undefined;
      const requestName = offer.requestName?.trim() || model?.requestName || offer.displayName?.trim() || offer.officialModelId?.trim();

      return {
      modelId: offer.modelId,
      modelSlug: offer.modelSlug,
      vendor: offer.vendor?.trim() || model?.vendor || "Custom",
      officialModelId: offer.officialModelId?.trim() || model?.officialModelId || requestName,
      requestName,
      apiType: offer.apiType || model?.apiType || "openai",
      displayName: offer.displayName?.trim() || model?.displayName || requestName,
      officialInputPriceUsd: model?.officialInputPriceUsd,
      officialOutputPriceUsd: model?.officialOutputPriceUsd,
      siteInputPriceUsd: toNumber(offer.siteInputPriceUsd),
      siteOutputPriceUsd: toNumber(offer.siteOutputPriceUsd),
      rechargeRatio,
      bonusRatio,
      sourceType: offer.sourceType || "manual",
      status: offer.status || "active",
      autoTestEnabled: Boolean(offer.autoTestEnabled),
      testApiKey: offer.testApiKey?.trim() || undefined
    };
    });
}

function normalizeOfferIdentity(value: unknown) {
  return String(value ?? "").trim().toLowerCase();
}

function getOfferIdentityValues(offer: RelaySiteOffer) {
  return [
    offer.modelId ? `model:${offer.modelId}` : "",
    offer.modelSlug ? `slug:${normalizeOfferIdentity(offer.modelSlug)}` : "",
    offer.officialModelId ? `name:${normalizeOfferIdentity(offer.officialModelId)}` : "",
    offer.requestName ? `name:${normalizeOfferIdentity(offer.requestName)}` : "",
    offer.displayName ? `name:${normalizeOfferIdentity(offer.displayName)}` : ""
  ].filter(Boolean);
}

function isSameOfferModel(left: RelaySiteOffer, right: RelaySiteOffer) {
  const leftValues = new Set(getOfferIdentityValues(left));
  return getOfferIdentityValues(right).some((value) => leftValues.has(value));
}

function stripPreviewFields(row: OfferImportPreviewRow): RelaySiteOffer {
  const { previewKey: _previewKey, importSource: _importSource, ...offer } = row;
  return offer;
}

function buildOfferPreviewKey(source: string, offer: RelaySiteOffer, index: number) {
  return `${source}:${offer.modelId ?? offer.modelSlug ?? offer.requestName ?? offer.officialModelId ?? offer.displayName ?? index}:${index}`;
}

function OfficialPriceHint({ form, models }: { form: ReturnType<typeof Form.useForm<RelaySiteOffer>>[0]; models: ModelListItem[] }) {
  const modelId = Form.useWatch("modelId", form);
  const model = models.find((item) => item.id === modelId);

  if (!model) {
    return null;
  }

  return (
    <Tag>
      官方价 {model.officialInputPriceUsd ?? "-"} / {model.officialOutputPriceUsd ?? "-"}
    </Tag>
  );
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
  const [offerImportOpen, setOfferImportOpen] = useState(false);
  const [offerImportTitle, setOfferImportTitle] = useState("抓取模型预览");
  const [offerImportRows, setOfferImportRows] = useState<OfferImportPreviewRow[]>([]);
  const [selectedImportOfferKeys, setSelectedImportOfferKeys] = useState<Key[]>([]);
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
  const [offerModalOpen, setOfferModalOpen] = useState(false);
  const [editingOfferIndex, setEditingOfferIndex] = useState<number | null>(null);
  const [offerStatusFilter, setOfferStatusFilter] = useState("active");
  const [offerKeyword, setOfferKeyword] = useState("");
  const [offerLoading, setOfferLoading] = useState(false);
  const [offerPage, setOfferPage] = useState(1);
  const [offerPageSize, setOfferPageSize] = useState(20);
  const [offerTotal, setOfferTotal] = useState(0);
  const [selectedSiteIds, setSelectedSiteIds] = useState<Key[]>([]);
  const [selectedOfferRows, setSelectedOfferRows] = useState<Key[]>([]);
  const [siteOffers, setSiteOffers] = useState<RelaySiteOffer[]>([]);
  const [siteFormValues, setSiteFormValues] = useState<SiteFormValues>(defaultSiteValues);
  const [form] = Form.useForm<SiteFormValues>();
  const [offerForm] = Form.useForm<RelaySiteOffer>();

  const modelOptions = useMemo(
    () =>
      models.map((model) => ({
        label: `${model.displayName} / ${model.requestName}`,
        value: model.id
      })),
    [models]
  );

  const filteredItems = useMemo(() => {
    const normalizedKeyword = keyword.trim().toLowerCase();

    return items.filter((item) => {
      if (item.status === "archived") {
        return false;
      }

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
      setItems(siteResult.items.filter((item) => item.status !== "archived"));
      setModels(modelResult.items);
      setError("");
    } catch (requestError) {
      setError((requestError as Error).message);
    } finally {
      setLoading(false);
    }
  }

  async function loadSiteOffers(
    siteId: number,
    options: { page?: number; pageSize?: number; keyword?: string; status?: string } = {}
  ) {
    const nextPage = options.page ?? offerPage;
    const nextPageSize = options.pageSize ?? offerPageSize;
    const nextKeyword = options.keyword ?? offerKeyword;
    const nextStatus = options.status ?? offerStatusFilter;
    setOfferLoading(true);

    try {
      const result = await fetchSiteOffers(siteId, {
        page: nextPage,
        pageSize: nextPageSize,
        keyword: nextKeyword,
        status: nextStatus
      });
      setSiteOffers(result.items);
      setOfferPage(result.page);
      setOfferPageSize(result.pageSize);
      setOfferTotal(result.total);
      setSelectedOfferRows([]);
      return result;
    } catch (requestError) {
      message.error((requestError as Error).message);
      return null;
    } finally {
      setOfferLoading(false);
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
    setSiteOffers([]);
    setSelectedOfferRows([]);
    setOfferKeyword("");
    setOfferStatusFilter("active");
    setOfferPage(1);
    setOfferPageSize(20);
    setOfferTotal(0);
    setSiteFormValues({ ...defaultSiteValues, rechargeRatio: 1, bonusRatio: 0 });
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
      setOfferKeyword("");
      setOfferStatusFilter("active");
      setOfferPage(1);
      setOfferPageSize(20);
      setOfferTotal(0);
      setSiteOffers([]);
      setSelectedOfferRows([]);
      const offerResult = await loadSiteOffers(detail.id, { page: 1, pageSize: 20, keyword: "", status: "active" });
      const firstOffer = offerResult?.items?.[0];
      setSiteFormValues({
        ...defaultSiteValues,
        ...detail,
        rechargeRatio: firstOffer?.rechargeRatio ?? 1,
        bonusRatio: firstOffer?.bonusRatio ?? 0
      });
      setPricingRechargeRatio(firstOffer?.rechargeRatio ?? 1);
      setPricingBonusRatio(firstOffer?.bonusRatio ?? 0);
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
    setOfferModalOpen(false);
    setEditingOfferIndex(null);
    setOfferImportOpen(false);
    setOfferImportRows([]);
    setSelectedImportOfferKeys([]);
    setSelectedOfferRows([]);
    setSiteOffers([]);
    setOfferKeyword("");
    setOfferStatusFilter("active");
    setOfferPage(1);
    setOfferTotal(0);
  }

  useEffect(() => {
    if (!formOpen) {
      return;
    }

    form.resetFields();
    form.setFieldsValue(siteFormValues);
  }, [form, formOpen, siteFormValues]);

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
        autoTestEnabled: Boolean(values.autoTestEnabled),
        testIntervalMinutes: toNumber(values.testIntervalMinutes) ?? 60,
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

  async function deleteSingleSite(record: RelaySiteListItem) {
    await deleteSite(record.id);
    message.success("站点已删除");
    setSelectedSiteIds((current) => current.filter((id) => id !== record.id));
    await load();
  }

  async function batchUpdateSites(status: string) {
    const ids = selectedSiteIds.map(Number);
    if (!ids.length) {
      return;
    }

    await Promise.all(ids.map((id) => updateSiteStatus(id, status)));
    message.success(`已更新 ${ids.length} 个站点`);
    setSelectedSiteIds([]);
    await load();
  }

  async function batchDeleteSites() {
    const ids = selectedSiteIds.map(Number);
    if (!ids.length) {
      return;
    }

    await Promise.all(ids.map((id) => deleteSite(id)));
    message.success(`已删除 ${ids.length} 个站点`);
    setSelectedSiteIds([]);
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
      openOfferImportPreview(
        "从 Base URL 抓取模型",
        rows.map(mapModelImportRowToOffer),
        "model"
      );
      message.success(`已抓取 ${rows.length} 个模型`);
    } catch (requestError) {
      message.error((requestError as Error).message);
    } finally {
      setModelImporting(false);
    }
  }

  function mapModelImportRowToOffer(row: ModelImportPreviewItem): RelaySiteOffer {
    return {
      vendor: row.vendor || importVendor,
      officialModelId: row.officialModelId,
      requestName: row.requestName,
      apiType: row.apiType,
      displayName: row.displayName,
      siteInputPriceUsd: row.officialInputPriceUsd,
      siteOutputPriceUsd: row.officialOutputPriceUsd,
      sourceType: "manual",
      status: "active",
      autoTestEnabled: false
    };
  }

  function findMatchingModel(row: RelayPricingPreviewItem) {
    const officialModelId = row.officialModelId.trim().toLowerCase();
    const requestName = row.requestName.trim().toLowerCase();
    if (!officialModelId && !requestName) {
      return undefined;
    }

    return models.find((model) => {
      const modelOfficialId = model.officialModelId.toLowerCase();
      const modelRequestName = model.requestName.toLowerCase();
      if (requestName && modelRequestName === requestName) {
        return true;
      }

      return modelOfficialId === officialModelId ||
        modelOfficialId === requestName ||
        modelOfficialId.endsWith(`/${officialModelId}`) ||
        officialModelId.endsWith(`/${modelOfficialId}`) ||
        modelOfficialId.endsWith(`/${requestName}`);
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
      requestName: model?.requestName || row.requestName,
      apiType: model?.apiType || "openai",
      displayName: model?.displayName || row.displayName,
      siteInputPriceUsd: isPerCall ? row.sitePerCallPriceUsd : row.siteInputPriceUsd,
      siteOutputPriceUsd: isPerCall ? undefined : row.siteOutputPriceUsd,
      sourceType: "crawl",
      status: "active",
      autoTestEnabled: false
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
        rechargeRatio: toNumber(form.getFieldValue("rechargeRatio")) ?? pricingRechargeRatio,
        bonusRatio: toNumber(form.getFieldValue("bonusRatio")) ?? pricingBonusRatio,
        rateBaseline: pricingRateBaseline,
        groupId: pricingGroupId || undefined
      });
      openOfferImportPreview("按 one-tracker 抓取价格", rows.map(mapPricingRowToOffer), "pricing");
      message.success(`已抓取 ${rows.length} 个模型价格`);
    } catch (requestError) {
      message.error((requestError as Error).message);
    } finally {
      setPricingImporting(false);
    }
  }

  function openOfferImportPreview(title: string, offers: RelaySiteOffer[], importSource: OfferImportPreviewRow["importSource"]) {
    const rows = offers.map((offer, index) => ({
      ...offer,
      previewKey: buildOfferPreviewKey(importSource, offer, index),
      importSource
    }));

    setOfferImportTitle(title);
    setOfferImportRows(rows);
    setSelectedImportOfferKeys(rows.map((row) => row.previewKey));
    setOfferImportOpen(true);
  }

  function buildOfferPayload(offer: RelaySiteOffer) {
    return normalizeOffers(
      [offer],
      models,
      toNumber(form.getFieldValue("rechargeRatio")) ?? pricingRechargeRatio,
      toNumber(form.getFieldValue("bonusRatio")) ?? pricingBonusRatio
    )[0];
  }

  async function saveSelectedImportedOffers() {
    const selectedKeys = new Set(selectedImportOfferKeys);
    const selectedRows = offerImportRows.filter((row) => selectedKeys.has(row.previewKey));

    if (!selectedRows.length) {
      message.warning("请选择要保存的模型");
      return;
    }

    if (editing) {
      try {
        await Promise.all(selectedRows.map((row) => {
          const payload = buildOfferPayload(stripPreviewFields(row));
          if (!payload) {
            throw new Error("璇峰畬鍠勬ā鍨嬪悕绉版垨璇锋眰鍚嶇О");
          }

          return createSiteOffer(editing.id, payload);
        }));
        setSelectedOfferRows([]);
        setOfferImportOpen(false);
        setOfferImportRows([]);
        setSelectedImportOfferKeys([]);
        await loadSiteOffers(editing.id, { page: 1 });
        message.success(`已保存 ${selectedRows.length} 个模型`);
      } catch (requestError) {
        message.error((requestError as Error).message);
      }
      return;
    }

    let replacedCount = 0;
    let createdCount = 0;
    setSiteOffers((current) => {
      const next = [...current];

      selectedRows.forEach((row) => {
        const incoming = stripPreviewFields(row);
        const existingIndex = next.findIndex((offer) => isSameOfferModel(offer, incoming));

        if (existingIndex >= 0) {
          const existing = next[existingIndex];
          next[existingIndex] = {
            ...existing,
            ...incoming,
            autoTestEnabled: existing.autoTestEnabled ?? incoming.autoTestEnabled,
            testApiKey: existing.testApiKey,
            hasTestApiKey: existing.hasTestApiKey
          };
          replacedCount += 1;
          return;
        }

        next.push(incoming);
        createdCount += 1;
      });

      return next;
    });

    setSelectedOfferRows([]);
    setOfferImportOpen(false);
    setOfferImportRows([]);
    setSelectedImportOfferKeys([]);
    message.success(`已保存 ${selectedRows.length} 个模型，新增 ${createdCount} 个，替换 ${replacedCount} 个`);
  }

  function applyModelToOfferForm(modelId: number) {
    const model = models.find((item) => item.id === modelId);
    if (!model) {
      return;
    }

    offerForm.setFieldsValue({
      modelId: model.id,
      modelSlug: model.slug,
      vendor: model.vendor,
      officialModelId: model.officialModelId,
      requestName: model.requestName,
      apiType: model.apiType,
      displayName: model.displayName
    });
  }

  function openCreateOfferModal() {
    if (!editing) {
      message.warning("请先保存基本信息后再添加模型");
      return;
    }

    setEditingOfferIndex(null);
    offerForm.resetFields();
    offerForm.setFieldsValue({
      vendor: importVendor,
      sourceType: "manual",
      status: "active",
      autoTestEnabled: false
    });
    setOfferModalOpen(true);
  }

  function openEditOfferModal(index: number) {
    setEditingOfferIndex(index);
    offerForm.resetFields();
    offerForm.setFieldsValue(siteOffers[index]);
    setOfferModalOpen(true);
  }

  async function removeOffer(index: number) {
    const offer = siteOffers[index];
    if (editing && offer?.id) {
      await deleteSiteOffer(editing.id, offer.id);
      message.success("模型报价已删除");
      await loadSiteOffers(editing.id, { page: offerPage });
      return;
    }

    setSiteOffers((current) => current.filter((_, offerIndex) => offerIndex !== index));
    setSelectedOfferRows((current) => current.filter((rowIndex) => rowIndex !== index));
  }

  async function batchUpdateOffers(status: string) {
    const selectedIndexes = new Set(selectedOfferRows.map(Number));
    if (editing) {
      const selectedOffers = siteOffers.filter((_, index) => selectedIndexes.has(index));
      await Promise.all(selectedOffers.map((offer) => {
        if (!offer.id) {
          return Promise.resolve();
        }

        const payload = buildOfferPayload({ ...offer, status });
        if (!payload) {
          throw new Error("模型报价数据不完整");
        }

        return updateSiteOffer(editing.id, offer.id, payload);
      }));
      message.success(`已更新 ${selectedOffers.length} 个模型`);
      await loadSiteOffers(editing.id, { page: offerPage });
      return;
    }

    setSiteOffers((current) => current.map((offer, index) => selectedIndexes.has(index) ? { ...offer, status } : offer));
    setSelectedOfferRows([]);
  }

  async function batchRemoveOffers() {
    const selectedIndexes = new Set(selectedOfferRows.map(Number));
    if (editing) {
      const selectedOffers = siteOffers.filter((offer, index) => selectedIndexes.has(index) && offer.id);
      await Promise.all(selectedOffers.map((offer) => deleteSiteOffer(editing.id, offer.id!)));
      message.success(`已删除 ${selectedOffers.length} 个模型`);
      await loadSiteOffers(editing.id, { page: offerPage });
      return;
    }

    setSiteOffers((current) => current.filter((_, index) => !selectedIndexes.has(index)));
    setSelectedOfferRows([]);
  }

  async function saveOffer() {
    const values = await offerForm.validateFields();
    const nextOffer: RelaySiteOffer = {
      ...values,
      vendor: values.vendor || importVendor,
      requestName: values.requestName || values.displayName,
      officialModelId: values.officialModelId || values.requestName || values.displayName,
      apiType: values.apiType || "openai",
      sourceType: values.sourceType || "manual",
      status: values.status || "active",
      autoTestEnabled: Boolean(values.autoTestEnabled),
      testApiKey: values.testApiKey?.trim() || undefined,
      hasTestApiKey: Boolean(values.testApiKey?.trim() || values.hasTestApiKey)
    };

    if (editing) {
      try {
        const payload = buildOfferPayload(nextOffer);
        if (!payload) {
          message.warning("请完善模型名称或请求名称");
          return;
        }

        const currentOffer = editingOfferIndex === null ? undefined : siteOffers[editingOfferIndex];
        if (currentOffer?.id) {
          await updateSiteOffer(editing.id, currentOffer.id, payload);
          message.success("模型报价已更新");
          await loadSiteOffers(editing.id, { page: offerPage });
        } else {
          await createSiteOffer(editing.id, payload);
          message.success("模型报价已新增");
          await loadSiteOffers(editing.id, { page: 1 });
        }
      } catch (requestError) {
        message.error((requestError as Error).message);
        return;
      }
    } else if (editingOfferIndex === null) {
      setSiteOffers((current) => [...current, nextOffer]);
    } else {
      setSiteOffers((current) => current.map((offer, index) => index === editingOfferIndex ? nextOffer : offer));
    }

    setOfferModalOpen(false);
    setEditingOfferIndex(null);
  }

  function handleOfferSearch(value: string) {
    setOfferKeyword(value);
    if (editing) {
      void loadSiteOffers(editing.id, { page: 1, keyword: value });
    }
  }

  function handleOfferStatusFilter(value: string) {
    setOfferStatusFilter(value);
    if (editing) {
      void loadSiteOffers(editing.id, { page: 1, status: value });
    }
  }

  function handleOfferPageChange(page: number, pageSize: number) {
    if (editing) {
      void loadSiteOffers(editing.id, { page, pageSize });
      return;
    }

    setOfferPage(page);
    setOfferPageSize(pageSize);
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
                { label: "草稿", value: "draft" }
              ]}
            />
            <Button disabled={!selectedSiteIds.length} onClick={() => batchUpdateSites("active")}>
              批量启用
            </Button>
            <Button disabled={!selectedSiteIds.length} onClick={() => batchUpdateSites("suspended")}>
              批量暂停
            </Button>
            <Popconfirm title={`确认删除选中的 ${selectedSiteIds.length} 个站点？`} onConfirm={batchDeleteSites}>
              <Button danger disabled={!selectedSiteIds.length}>
                批量删除
              </Button>
            </Popconfirm>
          </Space>

          {error ? <Alert type="warning" showIcon message={`站点接口暂不可用：${error}`} style={{ marginBottom: 16 }} /> : null}

          <Table
            className="cheapai-admin-table"
            loading={loading}
            rowKey="id"
            dataSource={filteredItems}
            pagination={{ pageSize: 12 }}
            rowSelection={{
              selectedRowKeys: selectedSiteIds,
              onChange: setSelectedSiteIds
            }}
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
                    {record.autoTestEnabled ? <Tag color={record.hasTestApiKey ? "purple" : "orange"}>自动测试</Tag> : null}
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
                render: (value: string | undefined, record) => formatBeijingTime(value ?? record.createdAtUtc)
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
                    <Popconfirm title="确认删除该站点？删除后列表不再显示。" onConfirm={() => deleteSingleSite(record)}>
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

        {formOpen ? (
          <Modal
            open
            title={editing ? "编辑站点" : "新增站点"}
            width={1280}
            maskClosable={false}
            onCancel={closeFormModal}
            cancelText="取消"
            okText={editing ? "保存更新" : "创建站点"}
            confirmLoading={formSubmitting}
            onOk={() => form.submit()}
          >
            <ProCard loading={formLoading} bordered={false}>
              <ProForm<SiteFormValues> form={form} submitter={false} onFinish={submit}>
                <Tabs
                  items={[
                    {
                      key: "basic",
                      label: "编辑基本信息",
                      children: (
                        <>
              <ProFormText name="name" label="站点名称" rules={[{ required: true, message: "请输入站点名称" }]} />
              <ProFormText name="slug" label="Slug" />
              <ProFormText name="baseUrl" label="Base URL" rules={[{ required: true, message: "请输入 Base URL" }]} />
              <ProFormText name="websiteUrl" label="官网地址" />
              <ProFormTextArea name="description" label="站点说明" fieldProps={{ rows: 3 }} />
              <ProFormText name="docsUrl" label="文档地址" />
              <ProFormText name="inviteUrl" label="邀请链接" />
              <ProFormTextArea name="recentReview" label="近期体验" fieldProps={{ rows: 3 }} />
              <Space wrap>
                <ProFormSwitch name="autoTestEnabled" label="启用自动测试" />
                <ProFormText
                  name="testIntervalMinutes"
                  label="测试间隔（分钟）"
                  fieldProps={{ type: "number", min: 15, step: 15 }}
                  rules={[{ required: true, message: "请输入测试间隔" }]}
                />
              </Space>
              <Space wrap>
                <ProFormText
                  name="rechargeRatio"
                  label="充值倍率"
                  fieldProps={{
                    type: "number",
                    step: "0.01",
                    onChange: (event) => setPricingRechargeRatio(toNumber(event.target.value) ?? 1)
                  }}
                  rules={[{ required: true, message: "请输入充值倍率" }]}
                />
                <ProFormText
                  name="bonusRatio"
                  label="赠送倍率"
                  fieldProps={{
                    type: "number",
                    step: "0.01",
                    onChange: (event) => setPricingBonusRatio(toNumber(event.target.value) ?? 0)
                  }}
                />
              </Space>
              <Space wrap>
                <ProFormSwitch name="supportsRefund" label="支持退款" />
                <ProFormSwitch name="supportsInvoice" label="支持发票" />
                <ProFormSwitch name="hasDocs" label="提供文档" />
              </Space>
                        </>
                      )
                    },
                    {
                      key: "models",
                      label: "编辑模型",
                      children: (
                        <>
              {!editing ? (
                <Alert
                  type="info"
                  showIcon
                  style={{ marginBottom: 16 }}
                  message="请先保存站点基本信息，再进入编辑页新增和维护模型报价。"
                />
              ) : null}
              <Space wrap style={{ marginBottom: 16 }}>
                <Input value={importVendor} onChange={(event) => setImportVendor(event.target.value)} placeholder="抓取厂商名" style={{ width: 160 }} />
                <Input.Password value={importApiKey} onChange={(event) => setImportApiKey(event.target.value)} placeholder="API Key，不保存" style={{ width: 260 }} />
                <Button icon={<SearchOutlined />} disabled={!editing} loading={modelImporting} onClick={importModelsFromEndpoint}>
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
                  value={pricingGroupId}
                  onChange={(event) => setPricingGroupId(event.target.value)}
                  placeholder="用户组，可留空取最低价"
                  style={{ width: 190 }}
                />
                <Button icon={<SearchOutlined />} disabled={!editing} loading={pricingImporting} onClick={importPricingFromEndpoint}>
                  按 one-tracker 抓取价格
                </Button>
              </Space>

              <Form.Item shouldUpdate noStyle>
                {() => {
                  const offers = siteOffers;
                  const rows: OfferTableRow[] = offers
                    .map((offer, rowIndex) => ({ ...offer, rowIndex }))
                    .filter((offer) => {
                      const status = offer.status || "active";
                      if (editing) {
                        return status !== "archived";
                      }

                      return status !== "archived" && (offerStatusFilter === "all" || status === offerStatusFilter);
                    });

                  return (
                    <Space direction="vertical" size={12} style={{ width: "100%" }}>
                      <Space style={{ width: "100%", justifyContent: "space-between" }}>
                        <Space wrap>
                          <Tag color="blue">当前显示 {editing ? offerTotal : rows.length} 个模型</Tag>
                          <Input.Search
                            allowClear
                            value={offerKeyword}
                            placeholder="搜索模型、请求名、厂商"
                            style={{ width: 260 }}
                            onSearch={handleOfferSearch}
                            onChange={(event) => {
                              setOfferKeyword(event.target.value);
                              if (!event.target.value) {
                                handleOfferSearch("");
                              }
                            }}
                          />
                          <Select
                            value={offerStatusFilter}
                            options={offerStatusFilterOptions}
                            style={{ width: 130 }}
                            onChange={handleOfferStatusFilter}
                          />
                          <Button disabled={!editing || !selectedOfferRows.length} onClick={() => batchUpdateOffers("active")}>
                            批量启用
                          </Button>
                          <Button disabled={!editing || !selectedOfferRows.length} onClick={() => batchUpdateOffers("hidden")}>
                            批量隐藏
                          </Button>
                          <Popconfirm title={`确认删除选中的 ${selectedOfferRows.length} 个模型？`} onConfirm={batchRemoveOffers}>
                            <Button danger disabled={!editing || !selectedOfferRows.length}>
                              批量删除
                            </Button>
                          </Popconfirm>
                        </Space>
                        <Button icon={<PlusOutlined />} type="primary" disabled={!editing} onClick={openCreateOfferModal}>
                          新增模型价格
                        </Button>
                      </Space>
                      <Table<OfferTableRow>
                        rowKey="rowIndex"
                        loading={offerLoading}
                        dataSource={rows}
                        pagination={editing ? {
                          current: offerPage,
                          pageSize: offerPageSize,
                          total: offerTotal,
                          showSizeChanger: true,
                          onChange: handleOfferPageChange
                        } : { pageSize: 12, showSizeChanger: false }}
                        scroll={{ x: 980, y: 460 }}
                        rowSelection={{
                          selectedRowKeys: selectedOfferRows,
                          onChange: setSelectedOfferRows
                        }}
                        columns={[
                          {
                            title: "模型名称",
                            dataIndex: "displayName",
                            render: (_, record) => (
                              <div>
                                <strong>{record.displayName || record.requestName || record.officialModelId || "-"}</strong>
                                <div style={{ color: "var(--cheapai-admin-muted)", fontSize: 12 }}>{record.requestName || record.officialModelId || record.modelSlug || "-"}</div>
                              </div>
                            )
                          },
                          {
                            title: "输入价格",
                            dataIndex: "siteInputPriceUsd",
                            width: 130,
                            render: (value?: number) => value ?? "-"
                          },
                          {
                            title: "输出价格",
                            dataIndex: "siteOutputPriceUsd",
                            width: 130,
                            render: (value?: number) => value ?? "-"
                          },
                          {
                            title: "渠道",
                            dataIndex: "sourceType",
                            width: 110,
                            render: (value?: string) => value === "crawl" ? "抓取" : "手动"
                          },
                          {
                            title: "状态",
                            dataIndex: "status",
                            width: 110,
                            render: (status?: string) => <Tag color={statusColors[status ?? ""] ?? "default"}>{offerStatusOptions.find((item) => item.value === status)?.label ?? status ?? "-"}</Tag>
                          },
                          {
                            title: "自动测试",
                            dataIndex: "autoTestEnabled",
                            width: 110,
                            render: (enabled?: boolean) => <Tag color={enabled ? "purple" : "default"}>{enabled ? "参与" : "关闭"}</Tag>
                          },
                          {
                            title: "测试 Key",
                            dataIndex: "hasTestApiKey",
                            width: 100,
                            render: (hasKey?: boolean) => <Tag color={hasKey ? "green" : "orange"}>{hasKey ? "已配置" : "未配置"}</Tag>
                          },
                          {
                            title: "操作",
                            width: 150,
                            render: (_, record) => (
                              <Space>
                                <Button type="link" onClick={() => openEditOfferModal(record.rowIndex)}>
                                  编辑
                                </Button>
                                <Button type="link" danger onClick={() => removeOffer(record.rowIndex)}>
                                  删除
                                </Button>
                              </Space>
                            )
                          }
                        ]}
                      />
                    </Space>
                  );
                }}
              </Form.Item>
                        </>
                      )
                    }
                  ]}
                />
              </ProForm>
            </ProCard>
            <Modal
              open={offerModalOpen}
              title={editingOfferIndex === null ? "新增模型价格" : "编辑模型价格"}
              width={720}
              forceRender
              maskClosable={false}
              onCancel={() => {
                setOfferModalOpen(false);
                setEditingOfferIndex(null);
              }}
              onOk={saveOffer}
              cancelText="取消"
              okText="保存"
            >
              <Form<RelaySiteOffer> form={offerForm} layout="vertical">
                <Form.Item name="modelId" label="模型名称">
                  <Select
                    allowClear
                    showSearch
                    optionFilterProp="label"
                    options={modelOptions}
                    placeholder="选择已有模型"
                    onChange={(value) => value && applyModelToOfferForm(value)}
                  />
                </Form.Item>
                <Form.Item name="displayName" label="显示名称" rules={[{ required: true, message: "请输入模型名称" }]}>
                  <Input />
                </Form.Item>
                <OfficialPriceHint form={offerForm} models={models} />
                <Space wrap style={{ width: "100%" }}>
                  <Form.Item name="siteInputPriceUsd" label="输入价格" style={{ width: 200 }}>
                    <Input type="number" step="0.000001" />
                  </Form.Item>
                  <Form.Item name="siteOutputPriceUsd" label="输出价格" style={{ width: 200 }}>
                    <Input type="number" step="0.000001" />
                  </Form.Item>
                  <Form.Item name="sourceType" label="渠道" style={{ width: 120 }}>
                    <Select
                      options={[
                        { label: "手动", value: "manual" },
                        { label: "抓取", value: "crawl" }
                      ]}
                    />
                  </Form.Item>
                  <Form.Item name="status" label="状态" style={{ width: 120 }}>
                    <Select options={offerStatusOptions} />
                  </Form.Item>
                  <Form.Item name="autoTestEnabled" label="自动测试" valuePropName="checked" style={{ width: 120 }}>
                    <Switch />
                  </Form.Item>
                </Space>
                <Form.Item
                  name="testApiKey"
                  label={offerForm.getFieldValue("hasTestApiKey") ? "模型测试 Key（已保存，留空不变）" : "模型测试 Key"}
                >
                  <Input.Password autoComplete="new-password" placeholder="仅用于该模型自动测试，不会展示明文" />
                </Form.Item>
                <Form.Item name="hasTestApiKey" hidden>
                  <Input />
                </Form.Item>
                <Form.Item name="modelSlug" hidden>
                  <Input />
                </Form.Item>
                <Form.Item name="vendor" hidden>
                  <Input />
                </Form.Item>
                <Form.Item name="officialModelId" hidden>
                  <Input />
                </Form.Item>
                <Form.Item name="requestName" hidden>
                  <Input />
                </Form.Item>
                <Form.Item name="apiType" hidden>
                  <Input />
                </Form.Item>
              </Form>
            </Modal>
            <Modal
              open={offerImportOpen}
              title={offerImportTitle}
              width={1080}
              maskClosable={false}
              onCancel={() => {
                setOfferImportOpen(false);
                setOfferImportRows([]);
                setSelectedImportOfferKeys([]);
              }}
              onOk={saveSelectedImportedOffers}
              okText="保存选中模型"
              cancelText="取消"
            >
              <Alert
                type="info"
                showIcon
                style={{ marginBottom: 16 }}
                message={`已抓取 ${offerImportRows.length} 个模型。保存时同名模型会替换当前列表中的旧数据，未选中的模型不会写入。`}
              />
              <Table<OfferImportPreviewRow>
                rowKey="previewKey"
                dataSource={offerImportRows}
                pagination={{ pageSize: 10, showSizeChanger: false }}
                scroll={{ x: 920, y: 420 }}
                rowSelection={{
                  selectedRowKeys: selectedImportOfferKeys,
                  onChange: setSelectedImportOfferKeys
                }}
                columns={[
                  {
                    title: "模型",
                    dataIndex: "displayName",
                    render: (_, record) => (
                      <div>
                        <strong>{record.displayName || record.requestName || record.officialModelId || "-"}</strong>
                        <div style={{ color: "var(--cheapai-admin-muted)", fontSize: 12 }}>{record.requestName || record.officialModelId || "-"}</div>
                      </div>
                    )
                  },
                  {
                    title: "接口",
                    dataIndex: "apiType",
                    width: 100,
                    render: (value?: string) => <Tag>{value || "openai"}</Tag>
                  },
                  {
                    title: "输入价格",
                    dataIndex: "siteInputPriceUsd",
                    width: 130,
                    render: (value?: number) => value ?? "-"
                  },
                  {
                    title: "输出价格",
                    dataIndex: "siteOutputPriceUsd",
                    width: 130,
                    render: (value?: number) => value ?? "-"
                  },
                  {
                    title: "来源",
                    dataIndex: "sourceType",
                    width: 100,
                    render: (value?: string) => value === "crawl" ? "抓取" : "模型列表"
                  },
                  {
                    title: "保存动作",
                    width: 110,
                    render: (_, record) => (
                      siteOffers.some((offer) => isSameOfferModel(offer, record))
                        ? <Tag color="orange">替换</Tag>
                        : <Tag color="green">新增</Tag>
                    )
                  }
                ]}
              />
            </Modal>
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
