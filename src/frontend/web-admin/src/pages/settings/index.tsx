import { useEffect, useState } from "react";
import { ProCard, ProForm, ProFormText } from "@ant-design/pro-components";
import { Button, message } from "antd";
import AuthGuard from "@/components/AuthGuard";
import AdminPage from "@/components/AdminPage";
import { fetchSiteSettings, updateSiteSettings, type SiteSettings } from "@/services/site-settings";

export default function SettingsPage() {
  const [loading, setLoading] = useState(false);
  const [initialValues, setInitialValues] = useState<SiteSettings>({
    siteName: "CheapAI",
    siteIconUrl: ""
  });

  async function load() {
    setLoading(true);

    try {
      const result = await fetchSiteSettings();
      setInitialValues({
        siteName: result.siteName,
        siteIconUrl: result.siteIconUrl ?? ""
      });
    } catch (error) {
      message.error((error as Error).message);
    } finally {
      setLoading(false);
    }
  }

  async function submit(values: SiteSettings) {
    await updateSiteSettings({
      siteName: values.siteName,
      siteIconUrl: values.siteIconUrl?.trim() || ""
    });
    message.success("站点品牌配置已更新");
    await load();
    return true;
  }

  useEffect(() => {
    void load();
  }, []);

  return (
    <AuthGuard>
      <AdminPage
        title="站点设置"
        subtitle="维护前台站点名称与图标 URL。更新后，公共站点头部品牌与浏览器图标会使用这里的最新配置。"
        extra={[
          <Button key="refresh" onClick={load}>
            刷新
          </Button>
        ]}
        metrics={[
          { label: "当前站点名", value: initialValues.siteName, note: "用于前台品牌标题" },
          { label: "图标状态", value: initialValues.siteIconUrl ? "已配置" : "未配置", note: "支持公开图片 URL" }
        ]}
      >
        <ProCard className="cheapai-admin-card" title="品牌配置" loading={loading}>
          <ProForm<SiteSettings>
            initialValues={initialValues}
            key={`${initialValues.siteName}-${initialValues.siteIconUrl ?? ""}`}
            submitter={{
              searchConfig: {
                submitText: "保存设置"
              }
            }}
            onFinish={submit}
          >
            <ProFormText
              name="siteName"
              label="站点名称"
              rules={[{ required: true, message: "请输入站点名称" }]}
              fieldProps={{ maxLength: 64 }}
            />
            <ProFormText
              name="siteIconUrl"
              label="站点图标 URL"
              extra="可填写公开可访问的 PNG、SVG 或 ICO 地址。留空时默认显示站点名称首字母。"
              fieldProps={{ maxLength: 512 }}
            />
          </ProForm>
        </ProCard>
      </AdminPage>
    </AuthGuard>
  );
}
