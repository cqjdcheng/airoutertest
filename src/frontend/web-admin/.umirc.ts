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
      name: "主数据",
      icon: "Database",
      routes: [
        {
          path: "/sites",
          name: "中转站点",
          component: "@/pages/sites"
        },
        {
          path: "/models",
          name: "模型目录",
          component: "@/pages/models"
        },
        {
          path: "/offers",
          name: "报价快照",
          component: "@/pages/offers"
        }
      ]
    },
    {
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
      name: "内容与审核",
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
        }
      ]
    }
  ]
});
