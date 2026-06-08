# CheapAI

CheapAI 是一个用于观察、测试和比较 AI 中转站稳定性的完整 Web 项目。它不把价格当成唯一指标，而是围绕真实连通性、响应结构、流式输出、延迟、模型匹配、风险证据和历史稳定性做持续检测，给用户一个更接近真实使用体验的参考。

项目包含用户端、管理端、后端 API、后台任务、Docker 发布脚本和自动化验收脚本。它也是一个完整 Vibe Coding 流程做出来的程序：从需求调研、竞品测试原理分析、产品方案、数据库设计、前后端实现、真实接口联调、并发验收、发布脚本到最小权限部署方案，主要通过人类产品判断 + AI 编码协作完成。

## 功能概览

- 用户端中转站列表：展示中转站稳定性、24 小时状态、最近测试、风险等级和详情。
- 用户端自助测试：用户可以提交自己的中转站 API 地址、Key 和模型，执行一次真实兼容性检测。
- 测试记录查询：公开展示平台测试和用户自测记录，支持详情页查看分数、检测项和证据。
- 管理端中转站管理：维护站点、模型报价、测试 Key、自动测试状态和上下架状态。
- 管理端模型管理：维护模型、供应商、能力分、API 类型和排序。
- 管理端运营视图：查看测试记录、风险证据、任务日志、站点提交、文章和站点设置。
- 后台任务：周期性执行价格抓取、平台测试、风险重算和排行榜聚合。
- 发布体系：本地构建 Docker 镜像，生成 release tar、manifest 和 SHA256SUMS，再上传到服务器执行受控发布。

## 技术栈

后端：

- .NET 10 / ASP.NET Core Web API
- 分层结构：`Api`、`Application`、`Domain`、`Infrastructure`、`BackgroundJobs`
- MySQL 8.4
- SqlSugarCore
- Redis / StackExchange.Redis
- Hangfire + Quartz 后台任务
- JWT 鉴权、BCrypt 密码、FluentValidation 参数校验
- Serilog 日志
- Swagger / OpenAPI

用户端：

- Next.js 15
- React 18
- Tailwind CSS 4
- ECharts

管理端：

- Umi Max
- React 18
- Ant Design / Pro Components
- Markdown 编辑能力

工程与发布：

- pnpm
- Docker / Docker Compose
- PowerShell release 脚本
- Playwright smoke 测试
- 本地并发验收脚本

## 目录结构

```text
.
├── deploy/production/              # 生产 Dockerfile、compose 和 release 脚本
├── docs/                           # 调研、设计、部署、测试和数据结构文档
├── skills/                         # 项目相关 Codex skill
├── src/backend/
│   ├── CheapAI.Api/                # ASP.NET Core API 入口和控制器
│   ├── CheapAI.Application/        # 应用服务、DTO、校验、接口定义和评分逻辑
│   ├── CheapAI.Domain/             # 领域模型和枚举
│   ├── CheapAI.Infrastructure/     # 数据库实体、仓储、爬虫、真实测试执行器
│   ├── CheapAI.BackgroundJobs/     # 后台任务 Worker
│   └── test/                       # 后端测试
├── src/frontend/
│   ├── web-public/                 # 用户端 Next.js 应用
│   └── web-admin/                  # 管理端 Umi Max 应用
└── tools/
    └── acceptance/                 # 页面 smoke、真实环境 smoke、并发 smoke
```

## 核心检测逻辑

CheapAI 的检测不是简单请求一个 `/models` 或只看 HTTP 200。核心流程是对目标中转站发起一次真实模型调用，再按协议、内容、性能和风险维度拆分评分。

### 1. API 类型识别

系统会根据用户选择或模型名判断目标是：

- `openai`：OpenAI Chat Completions / Responses 兼容接口。
- `anthropic`：Anthropic Messages 兼容接口，尤其针对 Claude Code / Claude CLI 类中转。

Claude 类模型会走单独逻辑，不强行套用 OpenAI 检测项。

### 2. 真实请求构造

OpenAI 兼容探针会发送一个极小 prompt，要求模型只返回 `OK`，用于降低成本并测试核心链路。

Anthropic / Claude 探针会模拟 Claude Code CLI 的请求特征，包括：

- Anthropic Messages payload
- `anthropic-version`
- Claude Code beta headers
- Claude Code session id
- Claude CLI 风格 User-Agent
- tool schema
- streaming 响应
- Claude 侧 usage/token 字段解析

这部分逻辑在：

```text
src/backend/CheapAI.Infrastructure/Participation/OpenAiCompatibleSelfTestRunner.cs
```

### 3. 检测项

一次测试会生成多项 probe result，典型维度包括：

- 协议连通性：是否真实请求成功。
- 响应结构：是否符合 OpenAI / Anthropic 的预期 JSON 或 SSE 结构。
- 内容完整性：是否能提取模型正文。
- 精确响应：是否按要求返回 `OK`。
- 延迟：首 token / 响应头时间、完整响应时间。
- 模型匹配：返回模型与期望模型族是否一致。
- token 用量：Claude 类接口会读取 input/output/total token。
- 流式完整性：SSE chunk、结束标记、流是否完整。
- 上游指纹：响应头和行为是否暴露异常风险。

### 4. 风险分与等级

风险分不是“通了就是满分”。当前风险计算会综合测试状态和延迟：

- 非成功状态会显著加分。
- 首响应超过阈值会加分。
- 完整响应超过阈值会加分。
- 正常成功也会保留基础风险分，避免“只要能通就是 100 分”的误导。

风险等级：

```text
0-20   low
21-50  medium
51-80  high
81-100 critical
```

评分逻辑入口：

```text
src/backend/CheapAI.Application/Scoring/RiskScoreCalculator.cs
```

### 5. 平台测试与用户自测分离

- 平台测试：由后台任务按中转站和模型配置周期性执行，结果进入公开统计和排行榜。
- 用户自测：用户主动提交接口信息，只记录自测结果，不直接污染平台排行榜。

## 本地开发

### 环境要求

- .NET SDK 10
- Node.js 22+
- pnpm
- MySQL 8.x
- Redis 7.x
- 可选：Docker Desktop

后端默认开发配置使用：

```text
MySQL: localhost:3334
Redis: 127.0.0.1:6379
API:   http://127.0.0.1:5157
用户端: http://127.0.0.1:3000
管理端: http://127.0.0.1:8000
```

生产环境必须覆盖默认密码和 JWT Key，不要使用仓库里的开发默认值。

### 安装依赖

```powershell
dotnet restore .\CheapAI.slnx
pnpm install --dir ".\tools"
pnpm install --dir ".\src\frontend\web-public"
pnpm install --dir ".\src\frontend\web-admin"
```

### 启动本地服务

Windows 下可以直接执行：

```powershell
.\run-local.cmd
```

也可以分开启动：

```powershell
dotnet run --project ".\src\backend\CheapAI.Api\CheapAI.Api.csproj" --urls "http://127.0.0.1:5157"
pnpm --dir ".\src\frontend\web-public" dev
pnpm --dir ".\src\frontend\web-admin" dev
```

本地默认入口：

```text
API:   http://127.0.0.1:5157/api/health
用户端: http://127.0.0.1:3000
管理端: http://127.0.0.1:8000
```

## 测试与验收

后端测试：

```powershell
dotnet test ".\src\backend\test\CheapAI.Application.Tests\CheapAI.Application.Tests.csproj" --no-restore
dotnet test ".\src\backend\test\CheapAI.Api.Tests\CheapAI.Api.Tests.csproj" --no-restore
```

前端构建：

```powershell
pnpm --dir ".\src\frontend\web-public" build
pnpm --dir ".\src\frontend\web-admin" build
```

用户端页面 smoke：

```powershell
node ".\tools\acceptance\public-frontend-smoke.cjs" --api-url http://127.0.0.1:5157 --public-url http://127.0.0.1:3000
```

并发 smoke：

```powershell
node ".\tools\acceptance\concurrency-smoke.cjs" --api-url http://127.0.0.1:5157 --public-url http://127.0.0.1:3000 --concurrency 12 --requests 30
```

真实外部环境 smoke 默认不会发送真实中转请求。只有显式传入 `-RunExternal` 并配置真实环境变量时，才会访问外部中转：

```powershell
powershell -ExecutionPolicy Bypass -File ".\tools\acceptance\real-env-smoke.ps1"
```

## 生产部署

当前生产部署按 4 个应用镜像发布：

- `realllmcn-api`
- `realllmcn-jobs`
- `realllmcn-public-web`
- `realllmcn-admin-web`

基础服务：

- `mysql`
- `redis`

生产 compose 文件：

```text
deploy/production/docker-compose.yml
```

默认端口绑定：

```text
api:        127.0.0.1:18080 -> 8080
public-web: 127.0.0.1:13000 -> 3000
admin-web:  127.0.0.1:18081 -> 80
```

建议由 Nginx 或宝塔反代：

```text
https://www.example.com        -> 127.0.0.1:13000
https://www.example.com/api    -> 127.0.0.1:18080
https://admin.example.com      -> 127.0.0.1:18081
```

### .env

生产服务器需要在 compose 目录放置 `.env`：

```bash
MYSQL_ROOT_PASSWORD=replace-with-strong-password
CHEAPAI_JWT_SIGNING_KEY=replace-with-long-random-secret
CHEAPAI_BOOTSTRAP_ADMIN_PASSWORD=replace-with-strong-admin-password
```

不要把 `.env` 提交到仓库。

### 数据卷

当前 compose 使用 Docker named volumes：

```yaml
volumes:
  mysql-data:
  redis-data:
  uploads-data:
```

容器内挂载点：

```text
mysql-data   -> /var/lib/mysql
redis-data   -> /data
uploads-data -> /app/wwwroot/uploads
```

如果你更希望使用宿主机显式目录，例如 `/www/wwwroot/cheapai/data/mysql`，需要先停服务、迁移 named volume 数据，再修改 compose。不要直接把 named volume 改成空目录 bind mount，否则 MySQL 会像丢库一样启动。

### 生成发布包

在本地或 CI 机器构建镜像，不建议在小内存生产服务器上构建。

```powershell
powershell -ExecutionPolicy Bypass -File ".\deploy\production\release\prepare-release.ps1"
```

输出目录：

```text
artifacts/release/<tag>/
├── manifest.json
├── SHA256SUMS
├── realllmcn-api_<tag>.tar
├── realllmcn-jobs_<tag>.tar
├── realllmcn-public-web_<tag>.tar
└── realllmcn-admin-web_<tag>.tar
```

### 应用发布包

上传到服务器：

```bash
scp artifacts/release/<tag>/* user@server:/www/wwwroot/realllm.cn/releases/<tag>/
```

执行发布：

```bash
bash /www/wwwroot/realllm.cn/deploy/production/release/apply-release.sh /www/wwwroot/realllm.cn/releases/<tag>
```

`apply-release.sh` 会执行：

1. `sha256sum -c SHA256SUMS`
2. 给现有 latest 镜像打 `rollback-before-<tag>` 标签
3. `docker load -i *.tar`
4. `docker compose --env-file .env up -d --no-deps --force-recreate api jobs public-web admin-web`
5. reload proxy nginx
6. 本机和公网健康检查

更安全的方式是创建一个最小权限发布用户，只允许写入 release 目录，并且只能 sudo 执行固定 apply 命令。参考：

```text
docs/最小权限发布方案.md
deploy/production/release/install-minimal-deployer.sh
```

## 安全边界

- 不要提交真实 API Key、数据库密码、JWT Key、服务器密码或 `.env`。
- 用户自测 API Key 只用于发起检测请求，不应出现在日志、报告或公开页面里。
- 自测接口会拒绝 localhost、内网 IP、link-local 和 metadata 地址，降低 SSRF 风险。
- 生产环境必须更换默认管理员密码和 JWT Signing Key。
- Docker 组权限等同高权限操作，自动发布建议使用受限 wrapper，而不是把部署用户直接加入 Docker 组。

## 开源说明

这个项目适合作为以下方向的参考：

- AI 中转站真实可用性检测
- OpenAI / Anthropic 兼容接口探针设计
- Claude Code / Claude CLI 类请求模拟
- 中转站稳定性评分和风险证据建模
- .NET + Next.js + Umi 的多端工程组织
- 小服务器上的本地构建、tar 包上传、受控发布流程
- Vibe Coding 在完整产品交付中的实际边界

需要注意：本仓库目前还没有附带正式开源协议文件。真正公开前请补充 `LICENSE`，并根据你的目标选择 MIT、Apache-2.0、AGPL-3.0 或其他协议。

## Vibe Coding 说明

CheapAI 不是一段一次性生成的 demo。它是一个完整 Vibe Coding 项目：人负责产品判断、验收标准、真实业务反馈和上线决策，AI 负责高密度实现、重构、文档、测试脚本和发布流程补齐。

这个过程暴露了一个很现实的结论：Vibe Coding 可以极大提高从想法到可运行系统的速度，但前提是必须有明确的工程边界，包括真实测试、慢查询审查、生产发布确认、最小权限部署、密钥隔离和回滚方案。没有这些边界，生成代码很快会变成不可维护的线上风险。

## 状态

项目仍在持续迭代中。欢迎基于真实使用场景提交 issue，例如：

- 新的中转站协议兼容问题
- Claude / Codex / OpenAI 新模型检测差异
- 评分维度不合理
- 慢查询或大数据量页面卡顿
- 部署脚本在不同服务器环境下的兼容问题
