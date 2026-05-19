import { useEffect, useMemo, useState, type Key } from "react";
import {
  DrawerForm,
  ProCard,
  ProFormSelect,
  ProFormText,
  ProFormTextArea
} from "@ant-design/pro-components";
import { Button, Form, Input, Popconfirm, Space, Table, Tag, message } from "antd";
import AuthGuard from "@/components/AuthGuard";
import AdminPage from "@/components/AdminPage";
import {
  archiveArticle,
  createArticle,
  deleteArticle,
  fetchArticles,
  publishArticle,
  type ArticleListItem
} from "@/services/participation";

type ArticleFormValues = {
  title: string;
  slug: string;
  summary?: string;
  contentMd: string;
  status?: string;
};

const statusColors: Record<string, string> = {
  draft: "default",
  published: "success",
  archived: "warning"
};

export default function ArticlesPage() {
  const [items, setItems] = useState<ArticleListItem[]>([]);
  const [drawerOpen, setDrawerOpen] = useState(false);
  const [loading, setLoading] = useState(false);
  const [keyword, setKeyword] = useState("");
  const [selectedArticleIds, setSelectedArticleIds] = useState<Key[]>([]);
  const [form] = Form.useForm<ArticleFormValues>();

  const filteredItems = useMemo(() => {
    const normalizedKeyword = keyword.trim().toLowerCase();
    if (!normalizedKeyword) {
      return items.filter((item) => item.status !== "archived");
    }

    return items.filter((item) =>
      item.status !== "archived" &&
      [item.title, item.slug, item.summary, item.status]
        .some((value) => String(value ?? "").toLowerCase().includes(normalizedKeyword))
    );
  }, [items, keyword]);

  async function load() {
    setLoading(true);
    try {
      const result = await fetchArticles(1, 80);
      setItems(result.items.filter((item) => item.status !== "archived"));
    } finally {
      setLoading(false);
    }
  }

  function openCreateDrawer() {
    form.resetFields();
    form.setFieldsValue({
      status: "draft"
    });
    setDrawerOpen(true);
  }

  async function submit(values: ArticleFormValues) {
    await createArticle({
      slug: values.slug,
      title: values.title,
      summary: values.summary,
      contentMd: values.contentMd,
      status: values.status ?? "draft"
    });

    message.success("文章已创建");
    setDrawerOpen(false);
    await load();
    return true;
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
    if (!ids.length) {
      return;
    }

    await Promise.all(ids.map((id) => publishArticle(id)));
    message.success(`已发布 ${ids.length} 篇文章`);
    setSelectedArticleIds([]);
    await load();
  }

  async function batchDeleteArticles() {
    const ids = selectedArticleIds.map(Number);
    if (!ids.length) {
      return;
    }

    await Promise.all(ids.map((id) => deleteArticle(id)));
    message.success(`已删除 ${ids.length} 篇文章`);
    setSelectedArticleIds([]);
    await load();
  }

  useEffect(() => {
    void load();
  }, []);

  return (
    <AuthGuard>
      <AdminPage
        title="文章管理"
        subtitle="内容运营场景改为列表上下文内抽屉新建，避免新增文章时脱离当前筛选和状态判断。"
        extra={[
          <Button key="create" type="primary" onClick={openCreateDrawer}>
            新增文章
          </Button>,
          <Button key="refresh" onClick={load}>
            刷新列表
          </Button>
        ]}
        metrics={[
          { label: "文章总数", value: items.length, note: "后台内容资产" },
          { label: "草稿", value: items.filter((item) => item.status === "draft").length, note: "待编辑" },
          { label: "已发布", value: items.filter((item) => item.status === "published").length, note: "公开可见" },
          { label: "已归档", value: items.filter((item) => item.status === "archived").length, note: "不再展示" }
        ]}
      >
        <ProCard className="cheapai-admin-card" title="文章列表">
          <Space wrap style={{ marginBottom: 16 }}>
            <Input.Search
              allowClear
              placeholder="搜索标题、Slug、摘要"
              style={{ width: 360 }}
              onSearch={setKeyword}
              onChange={(event) => setKeyword(event.target.value)}
            />
            <Button disabled={!selectedArticleIds.length} onClick={batchPublishArticles}>
              批量发布
            </Button>
            <Popconfirm title={`确认删除选中的 ${selectedArticleIds.length} 篇文章？`} onConfirm={batchDeleteArticles}>
              <Button danger disabled={!selectedArticleIds.length}>
                批量删除
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
              selectedRowKeys: selectedArticleIds,
              onChange: setSelectedArticleIds
            }}
            columns={[
              { title: "标题", dataIndex: "title" },
              { title: "Slug", dataIndex: "slug" },
              { title: "摘要", dataIndex: "summary", ellipsis: true, render: (value?: string) => value || "-" },
              {
                title: "状态",
                dataIndex: "status",
                render: (status: string) => <Tag color={statusColors[status] ?? "default"}>{status}</Tag>
              },
              { title: "发布时间", dataIndex: "publishedAt", render: (value?: string) => value || "-" },
              {
                title: "操作",
                render: (_, record: ArticleListItem) => (
                  <Space>
                    <Button type="link" disabled={record.status === "published"} onClick={() => changeStatus(record.id, "published")}>
                      发布
                    </Button>
                    <Popconfirm title="确认删除该文章？删除后列表不再显示。" onConfirm={() => deleteSingleArticle(record.id)}>
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

        <DrawerForm<ArticleFormValues>
          form={form}
          open={drawerOpen}
          title="新增文章"
          width={620}
          drawerProps={{
            destroyOnClose: false,
            onClose: () => setDrawerOpen(false)
          }}
          submitter={{ searchConfig: { submitText: "保存文章" } }}
          onFinish={submit}
        >
          <ProFormText name="title" label="标题" rules={[{ required: true, message: "请输入标题" }]} />
          <ProFormText name="slug" label="Slug" rules={[{ required: true, message: "请输入 Slug" }]} />
          <ProFormTextArea name="summary" label="摘要" fieldProps={{ rows: 3 }} />
          <ProFormSelect
            name="status"
            label="状态"
            initialValue="draft"
            options={[
              { label: "草稿", value: "draft" },
              { label: "发布", value: "published" }
            ]}
          />
          <ProFormTextArea
            name="contentMd"
            label="正文 Markdown"
            fieldProps={{ rows: 14 }}
            rules={[{ required: true, message: "请输入正文" }]}
          />
        </DrawerForm>
      </AdminPage>
    </AuthGuard>
  );
}
