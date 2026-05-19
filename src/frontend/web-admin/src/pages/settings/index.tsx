import { useEffect, useState } from "react";
import { ProCard, ProForm, ProFormText } from "@ant-design/pro-components";
import { Button, Form, Space, Upload, message } from "antd";
import type { UploadProps } from "antd";
import AuthGuard from "@/components/AuthGuard";
import AdminPage from "@/components/AdminPage";
import { fetchSiteSettings, updateSiteSettings, type SiteSettings } from "@/services/site-settings";
import { uploadFile } from "@/services/uploads";

export default function SettingsPage() {
  const [loading, setLoading] = useState(false);
  const [initialValues, setInitialValues] = useState<SiteSettings>({
    siteName: "CheapAI",
    siteIconUrl: "",
    faviconUrl: ""
  });
  const [form] = Form.useForm<SiteSettings>();

  async function load() {
    setLoading(true);
    try {
      const result = await fetchSiteSettings();
      const next = {
        siteName: result.siteName,
        siteIconUrl: result.siteIconUrl ?? "",
        faviconUrl: result.faviconUrl ?? ""
      };
      setInitialValues(next);
      form.setFieldsValue(next);
    } catch (error) {
      message.error((error as Error).message);
    } finally {
      setLoading(false);
    }
  }

  async function submit(values: SiteSettings) {
    await updateSiteSettings({
      siteName: values.siteName,
      siteIconUrl: values.siteIconUrl?.trim() || "",
      faviconUrl: values.faviconUrl?.trim() || ""
    });
    message.success("站点品牌配置已更新");
    await load();
    return true;
  }

  function uploadToField(field: keyof SiteSettings): UploadProps {
    return {
      showUploadList: false,
      beforeUpload: async (file) => {
        const result = await uploadFile(file);
        form.setFieldValue(field, result.url);
        message.success("文件已上传");
        return false;
      }
    };
  }

  useEffect(() => {
    void load();
  }, []);

  return (
    <AuthGuard>
      <AdminPage
        title="站点设置"
        subtitle="维护前台站点名称、品牌图标和浏览器 favicon。"
        extra={[<Button key="refresh" onClick={load}>刷新</Button>]}
        metrics={[
          { label: "当前站点名", value: initialValues.siteName, note: "用于前台品牌标题" },
          { label: "品牌图标", value: initialValues.siteIconUrl ? "已配置" : "未配置", note: "支持上传文件或填写 URL" },
          { label: "Favicon", value: initialValues.faviconUrl ? "已配置" : "未配置", note: "浏览器标签页图标" }
        ]}
      >
        <ProCard className="cheapai-admin-card" title="品牌配置" loading={loading}>
          <ProForm<SiteSettings>
            form={form}
            initialValues={initialValues}
            submitter={{ searchConfig: { submitText: "保存设置" } }}
            onFinish={submit}
          >
            <ProFormText
              name="siteName"
              label="站点名称"
              rules={[{ required: true, message: "请输入站点名称" }]}
              fieldProps={{ maxLength: 64 }}
            />
            <Space align="end" style={{ width: "100%" }}>
              <ProFormText name="siteIconUrl" label="站点图标 URL" fieldProps={{ maxLength: 512, style: { width: 520 } }} />
              <Upload {...uploadToField("siteIconUrl")}>
                <Button>上传站点图标</Button>
              </Upload>
            </Space>
            <Space align="end" style={{ width: "100%" }}>
              <ProFormText name="faviconUrl" label="Favicon URL" fieldProps={{ maxLength: 512, style: { width: 520 } }} />
              <Upload {...uploadToField("faviconUrl")}>
                <Button>上传 Favicon</Button>
              </Upload>
            </Space>
          </ProForm>
        </ProCard>
      </AdminPage>
    </AuthGuard>
  );
}
