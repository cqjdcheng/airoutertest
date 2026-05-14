import { LogoutOutlined, UserOutlined } from "@ant-design/icons";
import { history, Link } from "@umijs/max";
import { Avatar, Button, Dropdown, Space, Spin, Typography } from "antd";
import type { MenuProps } from "antd";
import type { ReactNode } from "react";
import {
  clearAccessToken,
  fetchCurrentAdmin,
  getAccessToken,
  logout,
  refreshAccessToken,
  type AdminProfile
} from "@/services/api";

const LOGIN_PATH = "/login";

function isPublicRoute(pathname: string) {
  return pathname === LOGIN_PATH;
}

async function resolveCurrentAdmin() {
  const token = getAccessToken();

  if (!token) {
    const refreshed = await refreshAccessToken();
    if (!refreshed) {
      return null;
    }
  }

  try {
    return await fetchCurrentAdmin();
  } catch {
    const refreshed = await refreshAccessToken();
    if (!refreshed) {
      clearAccessToken();
      return null;
    }

    try {
      return await fetchCurrentAdmin();
    } catch {
      clearAccessToken();
      return null;
    }
  }
}

export async function getInitialState(): Promise<{ currentAdmin: AdminProfile | null }> {
  if (typeof window === "undefined") {
    return { currentAdmin: null };
  }

  if (isPublicRoute(window.location.pathname)) {
    return { currentAdmin: null };
  }

  return {
    currentAdmin: await resolveCurrentAdmin()
  };
}

function HeaderAccount({
  currentAdmin,
  onLogout
}: {
  currentAdmin: AdminProfile;
  onLogout: () => Promise<void>;
}) {
  const items: MenuProps["items"] = [
    {
      key: "logout",
      icon: <LogoutOutlined />,
      label: "退出登录"
    }
  ];

  return (
    <Dropdown
      menu={{
        items,
        onClick: ({ key }) => {
          if (key === "logout") {
            void onLogout();
          }
        }
      }}
      placement="bottomRight"
      arrow
    >
      <Button className="cheapai-admin-account" type="text">
        <Space size={12}>
          <Avatar size={36} icon={<UserOutlined />} />
          <span className="cheapai-admin-account-copy">
            <strong>{currentAdmin.displayName}</strong>
            <Typography.Text type="secondary">{currentAdmin.username}</Typography.Text>
          </span>
        </Space>
      </Button>
    </Dropdown>
  );
}

type LayoutRuntimeProps = {
  initialState?: { currentAdmin: AdminProfile | null };
  loading: boolean;
  setInitialState: (state: { currentAdmin: AdminProfile | null }) => Promise<void> | void;
};

export const layout = ({ initialState, loading, setInitialState }: LayoutRuntimeProps) => {
  async function handleLogout() {
    await logout();
    await setInitialState({
      currentAdmin: null
    });
    history.replace(LOGIN_PATH);
  }

  return {
    menu: {
      locale: false
    },
    siderWidth: 236,
    collapsedButtonRender: false,
    disableContentMargin: false,
    headerTitleRender: () => <Link to="/dashboard">CheapAI Admin</Link>,
    onPageChange: () => {
      const pathname = typeof window === "undefined" ? "/dashboard" : window.location.pathname;
      const hasToken = Boolean(getAccessToken());

      if (loading) {
        return;
      }

      if (!initialState?.currentAdmin && !hasToken && !isPublicRoute(pathname)) {
        history.replace(LOGIN_PATH);
        return;
      }

      if ((initialState?.currentAdmin || hasToken) && isPublicRoute(pathname)) {
        history.replace("/dashboard");
      }
    },
    rightContentRender: () =>
      initialState?.currentAdmin ? (
        <HeaderAccount currentAdmin={initialState.currentAdmin} onLogout={handleLogout} />
      ) : (
        <Button type="primary" href={LOGIN_PATH}>
          登录
        </Button>
      ),
    childrenRender: (dom: ReactNode) =>
      loading ? (
        <div className="cheapai-admin-loading">
          <Spin size="large" />
        </div>
      ) : (
        dom
      )
  };
};
