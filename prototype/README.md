# CheapAI 页面原型说明

## 原型入口

- 导航页：[index.html](./index.html)

## 页面清单

### 用户端

- [user-home.html](./user-home.html)
  - 首页 / 发现页
  - 重点展示四大主榜、高风险提醒、自助测试入口

- [user-ranking.html](./user-ranking.html)
  - 模型排行页
  - 重点展示价格、稳定性、企业筛选、风险字段

- [user-site-detail.html](./user-site-detail.html)
  - 中转站详情页
  - 重点展示站点资料、支持模型、价格体系、稳定性趋势、风险证据

- [user-self-test.html](./user-self-test.html)
  - 自助测试页
  - 重点展示输入表单、安全说明、测试结果和风险结论

- [user-submit-site.html](./user-submit-site.html)
  - 站点提交页
  - 重点展示公开提交和审核流

### 后台

- [admin-dashboard.html](./admin-dashboard.html)
  - 后台总览
  - 重点展示待审核站点、高风险告警、失败任务

- [admin-sites.html](./admin-sites.html)
  - 中转站管理
  - 重点展示资料完整度、抓取状态、测试状态、风险状态

- [admin-risk-center.html](./admin-risk-center.html)
  - 反造假 / 测试中心
  - 重点展示风险命中、证据明细、补测任务、规则配置入口

## 设计说明

- 原型形式：静态 HTML
- 目标：验证信息架构、核心模块和页面层级
- 范围：覆盖需求文档中的首期 MVP 核心页面
- 不包含：
  - 真实接口
  - 完整交互逻辑
  - 登录权限流程
  - 文章系统

## 已验证内容

- 首页版式与主入口结构
- 后台总览版式
- 中转站详情页的信息密度与模块布局

## 本地预览方式

直接在浏览器中打开以下文件之一即可：

- `d:/project/CheapAi/prototype/index.html`
- `d:/project/CheapAi/prototype/user-home.html`
- `d:/project/CheapAi/prototype/admin-dashboard.html`

## 截图自检文件

为便于快速回看，已生成以下截图：

- `prototype/index.png`
- `prototype/user-home.png`
- `prototype/user-site-detail.png`
- `prototype/admin-dashboard.png`
