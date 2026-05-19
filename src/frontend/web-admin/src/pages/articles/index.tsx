import { useEffect, useMemo, useState, type Key } from "react";
import MDEditor from "@uiw/react-md-editor";
import "@uiw/react-md-editor/markdown-editor.css";
import { ProCard } from "@ant-design/pro-components";
import { Button, Form, Input, Modal, Popconfirm, Select, Space, Table, Tag, Upload, message } from "antd";
import type { UploadProps } from "antd";
import AuthGuard from "@/components/AuthGuard";
import AdminPage from "@/components/AdminPage";
import {
  archiveArticle,
  createArticle,
  createArticleTag,
  deleteArticle,
  deleteArticleTag,
  fetchArticle,
  fetchArticleTags,
  fetchArticles,
  publishArticle,
  updateArticle,
  updateArticleTag,
  type ArticleDetail,
  type ArticleListItem,
  type ArticleTag
} from "@/services/participation";
import { uploadFile } from "@/services/uploads";
import { formatBeijingTime } from "@/utils/time";

type ArticleFormValues = {
  title: string;
  slug: string;
  summary?: string;
  contentMd: string;
  status?: string;
  tagIds?: number[];
};

type TagFormValues = {
  id?: number;
  slug?: string;
  name: string;
  sortOrder?: number;
};

const statusColors: Record<string, string> = {
  draft: "default",
  published: "success",
  archived: "warning"
};

export default function ArticlesPage() {
  const [items, setItems] = useState<ArticleListItem[]>([]);
  const [tags, setTags] = useState<ArticleTag[]>([]);
  const [editing, setEditing] = useState<ArticleDetail | null>(null);
  const [modalOpen, setModalOpen] = useState(false);
  const [tagModalOpen, setTagModalOpen] = useState(false);
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [keyword, setKeyword] = useState("");
  const [selectedArticleIds, setSelectedArticleIds] = useState<Key[]>([]);
  const [form] = Form.useForm<ArticleFormValues>();
  const [tagForm] = Form.useForm<TagFormValues>();
  const contentValue = Form.useWatch("contentMd", form) ?? "";

  const filteredItems = useMemo(() => {
    const normalizedKeyword = keyword.trim().toLowerCase();
    return items.filter((item) => {
      if (item.status === "archived") {
        return false;
      }

      if (!normalizedKeyword) {
        return true;
      }

      return [item.title, item.slug, item.summary, item.status, ...(item.tags ?? []).map((tag) => tag.name)]
        .some((value) => String(value ?? "").toLowerCase().includes(normalizedKeyword));
    });
  }, [items, keyword]);

  async function load() {
    setLoading(true);
    try {
      const [articleResult, tagResult] = await Promise.all([fetchArticles(1, 100), fetchArticleTags()]);
      setItems(articleResult.items.filter((item) => item.status !== "archived"));
      setTags(tagResult);
    } finally {
      setLoading(false);
    }
  }

  function openCreateModal() {
    setEditing(null);
    form.resetFields();
    form.setFieldsValue({ status: "draft", contentMd: "", tagIds: [] });
    setModalOpen(true);
  }

  async function openEditModal(id: number) {
    const detail = await fetchArticle(id);
    setEditing(detail);
    form.setFieldsValue({
      title: detail.title,
      slug: detail.slug,
      summary: detail.summary,
      contentMd: detail.contentMd,
      status: detail.status,
      tagIds: detail.tags?.map((tag) => tag.id) ?? []
    });
    setModalOpen(true);
  }

  async function submit(values: ArticleFormValues) {
    setSaving(true);
    try {
      const payload = {
        slug: values.slug,
        title: values.title,
        summary: values.summary,
        contentMd: values.contentMd,
        status: values.status ?? "draft",
        tagIds: values.tagIds ?? []
      };

      if (editing) {
        await updateArticle(editing.id, payload);
        message.success("文章已更新");
      } else {
        await createArticle(payload);
        message.success("文章已创建");
      }

      setModalOpen(false);
      await load();
    } finally {
      setSaving(false);
    }
  }

  async function changeStatus(id: number, status: "published" | "archived") {
    if (status === "published") {
      await publishArticle(id);
      message.success("文章已发布");
    } else {
      await archiveArticle(id);
      message.success("文章已归档");
    }

    await load();
  }

  async function deleteSingleArticle(id: number) {
    await deleteArticle(id);
    message.success("文章已删除");
    setSelectedArticleIds((current) => current.filter((selectedId) => selectedId !== id));
    await load();
  }

  async function batchPublishArticles() {
    const ids = selectedArticleIds.map(Number);
    await Promise.all(ids.map((id) => publishArticle(id)));
    message.success(`已发布 ${ids.length} 篇文章`);
    setSelectedArticleIds([]);
    await load();
  }

  async function batchDeleteArticles() {
    const ids = selectedArticleIds.map(Number);
    await Promise.all(ids.map((id) => deleteArticle(id)));
    message.success(`已删除 ${ids.length} 篇文章`);
    setSelectedArticleIds([]);
    await load();
  }

  const uploadProps: UploadProps = {
    showUploadList: false,
    beforeUpload: async (file) => {
      const result = await uploadFile(file);
      const current = form.getFieldValue("contentMd") ?? "";
      form.setFieldValue("contentMd", `${current}${current ? "\n\n" : ""}![${file.name}](${result.url})`);
      message.success("图片已上传并插入正文");
      return false;
    }
  };

  function openCreateTagModal() {
    tagForm.resetFields();
    setTagModalOpen(true);
  }

  function openEditTagModal(tag: ArticleTag) {
    tagForm.setFieldsValue(tag);
    setTagModalOpen(true);
  }

  async function saveTag(values: TagFormValues) {
    if (values.id) {
      await updateArticleTag(values.id, values);
      message.success("标签已更新");
    } else {
      await createArticleTag(values);
      message.success("标签已创建");
    }

    setTagModalOpen(false);
    await load();
  }

  useEffect(() => {
    void load();
  }, []);

  return (
    <AuthGuard>
      <AdminPage
        title="文章管理"
        subtitle="维护文章、标签和 Markdown 正文，正文支持预览与图片上传。"
        extra={[
          <Button key="create" type="primary" onClick={openCreateModal}>新增文章</Button>,
          <Button key="tag" onClick={openCreateTagModal}>新增标签</Button>,
          <Button key="refresh" onClick={load}>刷新列表</Button>
        ]}
        metrics={[
          { label: "文章总数", value: items.length, note: "后台内容资产" },
          { label: "草稿", value: items.filter((item) => item.status === "draft").length, note: "待编辑" },
          { label: "已发布", value: items.filter((item) => item.status === "published").length, note: "公开可见" },
          { label: "标签", value: tags.length, note: "可维护筛选项" }
        ]}
      >
        <ProCard className="cheapai-admin-card" title="文章列表">
          <Space wrap style={{ marginBottom: 16 }}>
            <Input.Search
              allowClear
              placeholder="搜索标题、Slug、摘要、标签"
              style={{ width: 360 }}
              onSearch={setKeyword}
              onChange={(event) => setKeyword(event.target.value)}
            />
            <Button disabled={!selectedArticleIds.length} onClick={batchPublishArticles}>批量发布</Button>
            <Popconfirm title={`确认删除选中的 ${selectedArticleIds.length} 篇文章？`} onConfirm={batchDeleteArticles}>
              <Button danger disabled={!selectedArticleIds.length}>批量删除</Button>
            </Popconfirm>
          </Space>

          <Table
            className="cheapai-admin-table"
            loading={loading}
            rowKey="id"
            dataSource={filteredItems}
            pagination={{ pageSize: 10 }}
            rowSelection={{ selectedRowKeys: selectedArticleIds, onChange: setSelectedArticleIds }}
            columns={[
              { title: "标题", dataIndex: "title" },
              { title: "Slug", dataIndex: "slug" },
              {
                title: "标签",
                render: (_, record: ArticleListItem) => (
                  <Space size={4} wrap>{record.tags?.map((tag) => <Tag key={tag.id}>{tag.name}</Tag>)}</Space>
                )
              },
              { title: "摘要", dataIndex: "summary", ellipsis: true, render: (value?: string) => value || "-" },
              { title: "状态", dataIndex: "status", render: (status: string) => <Tag color={statusColors[status] ?? "default"}>{status}</Tag> },
              { title: "发布时间", dataIndex: "publishedAt", render: (value?: string) => formatBeijingTime(value) },
              {
                title: "操作",
                render: (_, record: ArticleListItem) => (
                  <Space>
                    <Button type="link" onClick={() => openEditModal(record.id)}>编辑</Button>
                    <Button type="link" disabled={record.status === "published"} onClick={() => changeStatus(record.id, "published")}>发布</Button>
                    <Popconfirm title="确认删除该文章？删除后列表不再显示。" onConfirm={() => deleteSingleArticle(record.id)}>
                      <Button type="link" danger>删除</Button>
                    </Popconfirm>
                  </Space>
                )
              }
            ]}
          />
        </ProCard>

        <ProCard className="cheapai-admin-card" title="标签维护" style={{ marginTop: 16 }}>
          <Table
            rowKey="id"
            dataSource={tags}
            pagination={false}
            size="small"
            columns={[
              { title: "名称", dataIndex: "name" },
              { title: "Slug", dataIndex: "slug" },
              { title: "排序", dataIndex: "sortOrder" },
              {
                title: "操作",
                render: (_, record: ArticleTag) => (
                  <Space>
                    <Button type="link" onClick={() => openEditTagModal(record)}>编辑</Button>
                    <Popconfirm title="确认删除该标签？文章列表将不再显示它。" onConfirm={async () => { await deleteArticleTag(record.id); await load(); }}>
                      <Button type="link" danger>删除</Button>
                    </Popconfirm>
                  </Space>
                )
              }
            ]}
          />
        </ProCard>

        <Modal
          open={modalOpen}
          title={editing ? "编辑文章" : "新增文章"}
          width={1100}
          forceRender
          maskClosable={false}
          confirmLoading={saving}
          onCancel={() => setModalOpen(false)}
          onOk={() => form.submit()}
          okText="保存文章"
          cancelText="取消"
        >
          <Form<ArticleFormValues> form={form} layout="vertical" onFinish={submit}>
            <Space style={{ width: "100%" }} align="start">
              <Form.Item name="title" label="标题" rules={[{ required: true, message: "请输入标题" }]} style={{ width: 320 }}>
                <Input />
              </Form.Item>
              <Form.Item name="slug" label="Slug" rules={[{ required: true, message: "请输入 Slug" }]} style={{ width: 260 }}>
                <Input />
              </Form.Item>
              <Form.Item name="status" label="状态" initialValue="draft" style={{ width: 160 }}>
                <Select options={[{ label: "草稿", value: "draft" }, { label: "发布", value: "published" }]} />
              </Form.Item>
            </Space>
            <Form.Item name="tagIds" label="标签">
              <Select
                mode="multiple"
                options={tags.map((tag) => ({ label: tag.name, value: tag.id }))}
                placeholder="选择文章标签"
              />
            </Form.Item>
            <Form.Item name="summary" label="摘要">
              <Input.TextArea rows={3} />
            </Form.Item>
            <Form.Item label="正文 Markdown" required>
              <Space style={{ marginBottom: 8 }}>
                <Upload {...uploadProps}>
                  <Button>上传图片</Button>
                </Upload>
              </Space>
              <Form.Item name="contentMd" noStyle rules={[{ required: true, message: "请输入正文" }]}>
                <MDEditor value={contentValue} onChange={(value) => form.setFieldValue("contentMd", value ?? "")} height={460} preview="live" />
              </Form.Item>
            </Form.Item>
          </Form>
        </Modal>

        <Modal
          open={tagModalOpen}
          title={tagForm.getFieldValue("id") ? "编辑标签" : "新增标签"}
          forceRender
          onCancel={() => setTagModalOpen(false)}
          onOk={() => tagForm.submit()}
          okText="保存"
          cancelText="取消"
        >
          <Form<TagFormValues> form={tagForm} layout="vertical" onFinish={saveTag}>
            <Form.Item name="id" hidden><Input /></Form.Item>
            <Form.Item name="name" label="名称" rules={[{ required: true, message: "请输入标签名称" }]}><Input /></Form.Item>
            <Form.Item name="slug" label="Slug"><Input placeholder="留空自动根据名称生成" /></Form.Item>
            <Form.Item name="sortOrder" label="排序" initialValue={1000}><Input type="number" /></Form.Item>
          </Form>
        </Modal>
      </AdminPage>
    </AuthGuard>
  );
}
