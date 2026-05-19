import { LockOutlined, UserOutlined } from "@ant-design/icons";
import { LoginForm, ProFormText } from "@ant-design/pro-components";
import { history, useModel } from "@umijs/max";
import { message } from "antd";
import { login, setAccessToken } from "@/services/api";

export default function LoginPage() {
  const { setInitialState } = useModel("@@initialState");

  return (
    <main className="cheapai-admin-login">
      <section className="cheapai-admin-login-card">
        <div className="cheapai-admin-login-copy">
          <div className="cheapai-admin-eyebrow">CheapAI Console</div>
          <h1>管理可信数据资产</h1>
          <p>登录后维护中转站点、模型、报价、测试记录、风险证据和内容审核，管理端操作收敛到统一工作流。</p>
        </div>

        <LoginForm
          title="CheapAI Admin"
          subTitle="管理员登录页已接入后端认证 API"
          onFinish={async (values) => {
            try {
              const result = await login(values.username, values.password);
              setAccessToken(result.accessToken);
              await setInitialState({
                currentAdmin: result.admin
              });
              message.success(`欢迎回来，${result.admin.displayName}`);
              history.replace("/dashboard");
              return true;
            } catch (error) {
              message.error((error as Error).message);
              return false;
            }
          }}
        >
          <ProFormText
            name="username"
            fieldProps={{ size: "large", prefix: <UserOutlined /> }}
            placeholder="管理员账号"
            rules={[{ required: true, message: "请输入管理员账号" }]}
          />
          <ProFormText.Password
            name="password"
            fieldProps={{ size: "large", prefix: <LockOutlined /> }}
            placeholder="管理员密码"
            rules={[{ required: true, message: "请输入管理员密码" }]}
          />
        </LoginForm>
      </section>
    </main>
  );
}
