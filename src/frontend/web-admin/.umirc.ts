import { defineConfig } from "@umijs/max";

export default defineConfig({
  npmClient: "pnpm",
  esbuildMinifyIIFE: true,
  antd: {},
  access: {},
  model: {},
  initialState: {},
  request: {},
  layout: {
    title: "CheapAI Admin",
    layout: "mix",
    fixedHeader: true,
    splitMenus: false
  },
  routes: [
    {
      path: "/login",
      name: "登录",
      layout: false,
      component: "@/pages/Login"
    },
    {
      path: "/",
      redirect: "/dashboard"
    },
    {
      path: "/dashboard",
      name: "工作台",
      icon: "Dashboard",
      component: "@/pages/Dashboard"
    },
    {
      key: "master-data",
      name: "主数据",
      icon: "Database",
      routes: [
        {
          path: "/sites",
          name: "中转站点",
          component: "@/pages/sites"
        },
        {
          path: "/sites/:id",
          name: "站点详情",
          hideInMenu: true,
          component: "@/pages/sites/detail"
        },
        {
          path: "/models",
          name: "模型目录",
          component: "@/pages/models"
        },
        {
          path: "/model-providers",
          name: "模型提供商",
          component: "@/pages/model-providers"
        },
        {
          path: "/offers",
          name: "报价快照",
          component: "@/pages/offers"
        }
      ]
    },
    {
      key: "operations",
      name: "运营与风控",
      icon: "Block",
      routes: [
        {
          path: "/tests",
          name: "测试记录",
          component: "@/pages/tests"
        },
        {
          path: "/risks",
          name: "风险中心",
          component: "@/pages/risks"
        },
        {
          path: "/jobs",
          name: "任务执行",
          component: "@/pages/jobs"
        }
      ]
    },
    {
      key: "content",
      name: "内容与配置",
      icon: "Read",
      routes: [
        {
          path: "/submissions",
          name: "线索审核",
          component: "@/pages/submissions"
        },
        {
          path: "/articles",
          name: "文章管理",
          component: "@/pages/articles"
        },
        {
          path: "/settings",
          name: "站点设置",
          icon: "Setting",
          component: "@/pages/settings"
        }
      ]
    }
  ]
});
