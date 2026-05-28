# CheapAI API 设计文档

## 1. 文档目标

本文档用于定义 CheapAI 项目的接口分组、路径、请求参数、响应结构、错误码与鉴权要求。

说明：

1. 本文档按“用户端与管理端完全独立”设计
2. 用户端与管理端视为两套不同客户端
3. 不强行统一公开 DTO 和后台 DTO
4. 所有字段定义均以 `数据库设计文档.md` 为基础

---

## 2. 全局约定

## 2.1 路由前缀

### Public API

`/api/v1/public/...`

只服务用户端。

### Auth API

`/api/v1/auth/...`

只服务管理端登录流。

### Admin API

`/api/v1/admin/...`

只服务管理端。

### Internal Job API

`/api/v1/internal/jobs/...`

只服务后台内部任务触发或管理员手动触发。

---

## 2.2 统一响应结构

所有接口统一返回：

```json
{
  "code": 0,
  "message": "ok",
  "data": {},
  "requestId": "req_xxx",
  "timestamp": "2026-05-11T12:00:00Z"
}
```

分页结构统一为：

```json
{
  "items": [],
  "page": 1,
  "pageSize": 20,
  "total": 120
}
```

---

## 2.3 鉴权规则

### 用户端

1. 默认匿名访问
2. 不使用登录态

### 管理端

1. 使用 `JWT Access Token`
2. 使用 `HttpOnly Refresh Token`

### Internal Job API

1. 只允许内部服务或管理员触发
2. 不对公开前端暴露

---

## 2.4 通用查询参数

### 分页参数

| 参数 | 类型 | 必填 | 说明 |
|---|---|---:|---|
| page | int | 否 | 页码，默认 1 |
| pageSize | int | 否 | 每页数量，默认 20，最大 100 |

### 排行筛选参数

| 参数 | 类型 | 必填 | 说明 |
|---|---|---:|---|
| model | string | 是 | 模型 slug |
| rankingType | string | 是 | `price/stability/value/site-overall` |
| window | string | 否 | `24h/7d/30d` |
| riskFilter | string | 否 | `all/exclude-high/only-low-risk` |
| supportsInvoice | bool | 否 | 是否筛选开票 |
| supportsRefund | bool | 否 | 是否筛选退款 |
| hasDocs | bool | 否 | 是否筛选文档 |

---

## 3. Public API

## 3.1 首页总览

### GET `/api/v1/public/home/overview`

返回首页摘要数据。

请求参数：

| 参数 | 类型 | 必填 | 说明 |
|---|---|---:|---|
| model | string | 否 | 默认模型 slug |

响应数据：

```json
{
  "featuredModel": "gpt-4.1-mini",
  "stats": {
    "siteCount": 0,
    "modelCount": 0,
    "latestTestAt": ""
  },
  "topPriceCards": [],
  "topStabilityCards": [],
  "riskHighlights": [],
  "dataPolicySummary": {}
}
```

---

## 3.2 模型排行

### GET `/api/v1/public/rankings/models/{modelSlug}`

按模型获取榜单。

路径参数：

| 参数 | 类型 | 说明 |
|---|---|---|
| modelSlug | string | 模型 slug |

查询参数：

| 参数 | 类型 | 必填 | 说明 |
|---|---|---:|---|
| rankingType | string | 是 | `price/stability/value` |
| window | string | 否 | `24h/7d/30d` |
| riskFilter | string | 否 | 风险过滤 |
| supportsInvoice | bool | 否 | 企业筛选 |
| supportsRefund | bool | 否 | 企业筛选 |
| hasDocs | bool | 否 | 企业筛选 |
| page | int | 否 | 页码 |
| pageSize | int | 否 | 每页数量 |

响应数据：

```json
{
  "model": {
    "slug": "gpt-4.1-mini",
    "displayName": "GPT-4.1 mini"
  },
  "rankingType": "price",
  "window": "7d",
  "snapshotAt": "2026-05-11T12:00:00Z",
  "items": [
    {
      "siteSlug": "relay-port",
      "siteName": "RelayPort",
      "effectiveInputPriceUsd": 0.32,
      "effectiveOutputPriceUsd": 1.27,
      "availability24h": 0.988,
      "stability7d": 97.9,
      "firstTokenMs": 820,
      "fullResponseMs": 6300,
      "riskScore": 14,
      "riskLevel": "low",
      "supportsInvoice": true,
      "supportsRefund": true,
      "hasDocs": true
    }
  ],
  "page": 1,
  "pageSize": 20,
  "total": 1
}
```

---

## 3.3 站点详情

### GET `/api/v1/public/sites/{siteSlug}`

返回站点详情。

路径参数：

| 参数 | 类型 | 说明 |
|---|---|---|
| siteSlug | string | 站点 slug |

查询参数：

| 参数 | 类型 | 必填 | 说明 |
|---|---|---:|---|
| window | string | 否 | `24h/7d/30d` |
| model | string | 否 | 模型 slug |

响应数据：

```json
{
  "site": {},
  "supportedModels": [],
  "pricing": [],
  "latestTests": [],
  "riskSummary": {},
  "trends": {
    "price": [],
    "stability": []
  }
}
```

---

## 3.4 模型能力榜

### GET `/api/v1/public/model-capabilities`

返回第三方模型能力榜。

查询参数：

| 参数 | 类型 | 必填 | 说明 |
|---|---|---:|---|
| source | string | 否 | `artificial-analysis/lmarena` |
| vendor | string | 否 | 厂商筛选 |
| page | int | 否 | 页码 |
| pageSize | int | 否 | 每页数量 |

---

## 3.5 自助测试创建

### POST `/api/v1/public/self-tests`

创建一次用户自助测试。

请求体：

```json
{
  "baseUrl": "https://example.com/v1",
  "apiKey": "sk-xxx",
  "model": "gpt-4.1-mini",
  "stream": true,
  "testMode": "standard"
}
```

字段说明：

| 字段 | 类型 | 必填 | 说明 |
|---|---|---:|---|
| baseUrl | string | 是 | 站点地址 |
| apiKey | string | 是 | 用户 Key，不落库 |
| model | string | 是 | 模型名 |
| stream | bool | 否 | 是否流式 |
| testMode | string | 否 | `standard/quick` |

响应数据：

```json
{
  "selfTestId": "selftest_xxx",
  "status": "queued"
}
```

---

## 3.6 自助测试结果查询

### GET `/api/v1/public/self-tests/{selfTestId}`

返回当前自助测试结果。

响应数据：

```json
{
  "selfTestId": "selftest_xxx",
  "status": "succeeded",
  "result": {
    "success": true,
    "firstTokenMs": 820,
    "fullResponseMs": 6100,
    "riskScore": 36,
    "riskLevel": "medium",
    "evidenceSummary": []
  }
}
```

---

## 3.7 站点公开提交

### POST `/api/v1/public/submissions`

创建站点提交。

请求体：

```json
{
  "name": "Aurora Relay",
  "url": "https://aurora.example.com",
  "contactInfo": "tg:@aurora",
  "notes": "支持 OpenAI / Claude"
}
```

---

## 4. Auth API

## 4.1 登录

### POST `/api/v1/auth/login`

请求体：

```json
{
  "username": "admin",
  "password": "******"
}
```

响应数据：

```json
{
  "accessToken": "jwt",
  "expiresIn": 3600,
  "admin": {
    "id": 1,
    "username": "admin",
    "displayName": "管理员"
  }
}
```

说明：

1. Refresh Token 通过 HttpOnly Cookie 下发

## 4.2 刷新令牌

### POST `/api/v1/auth/refresh`

无请求体，通过 Refresh Token Cookie 刷新。

## 4.3 登出

### POST `/api/v1/auth/logout`

使当前 Refresh Token 失效。

## 4.4 当前管理员信息

### GET `/api/v1/auth/me`

返回当前管理员信息。

---

## 5. Admin API

## 5.1 站点管理

### GET `/api/v1/admin/sites`

查询站点列表。

### POST `/api/v1/admin/sites`

新建站点。

### GET `/api/v1/admin/sites/{siteId}`

查询站点详情。

### PUT `/api/v1/admin/sites/{siteId}`

更新站点。

### PATCH `/api/v1/admin/sites/{siteId}/status`

更新站点状态。

---

## 5.2 渠道管理

### GET `/api/v1/admin/sites/{siteId}/channels`

查询渠道列表。

### POST `/api/v1/admin/sites/{siteId}/channels`

新增渠道。

### PUT `/api/v1/admin/channels/{channelId}`

更新渠道。

---

## 5.3 模型管理

### GET `/api/v1/admin/models`

### POST `/api/v1/admin/models`

### GET `/api/v1/admin/models/{modelId}`

### PUT `/api/v1/admin/models/{modelId}`

### PATCH `/api/v1/admin/models/{modelId}/status`

---

## 5.4 报价与抓取管理

### GET `/api/v1/admin/offers`

查询报价快照。

查询参数：

| 参数 | 类型 | 必填 | 说明 |
|---|---|---:|---|
| siteId | long | 否 | 站点筛选 |
| modelId | long | 否 | 模型筛选 |
| page | int | 否 | 分页 |
| pageSize | int | 否 | 分页 |

### POST `/api/v1/admin/crawl-jobs/manual-run`

手动触发抓取。

请求体：

```json
{
  "siteId": 1,
  "priority": "high"
}
```

### GET `/api/v1/admin/crawl-jobs`

查询抓取任务列表。

### GET `/api/v1/admin/crawl-jobs/{jobId}`

查询抓取任务详情。

### POST `/api/v1/admin/offers/{offerId}/verify`

人工确认报价快照。

---

## 5.5 测试任务管理

### GET `/api/v1/admin/test-jobs`

查询测试任务列表。

### GET `/api/v1/admin/test-jobs/{jobId}`

查询测试任务详情。

### POST `/api/v1/admin/test-jobs/manual-run`

手动触发测试。

请求体：

```json
{
  "siteId": 1,
  "modelId": 2,
  "jobType": "manual-retest",
  "priority": "high",
  "executionWindow": "next-3-hours"
}
```

---

## 5.6 风险中心

### GET `/api/v1/admin/risks`

查询风险命中列表。

### GET `/api/v1/admin/risks/{riskId}`

查询风险详情。

### POST `/api/v1/admin/risks/{riskId}/review`

复核风险。

请求体：

```json
{
  "reviewStatus": "false_positive",
  "reviewNote": "人工判定为误报"
}
```

### POST `/api/v1/admin/risks/recalculate`

触发风险重算。

---

## 5.7 审核流

### GET `/api/v1/admin/submissions`

查询提交列表。

### POST `/api/v1/admin/submissions/{submissionId}/approve`

审核通过。

### POST `/api/v1/admin/submissions/{submissionId}/reject`

审核拒绝。

---

## 5.8 文章管理

### GET `/api/v1/admin/articles`

### POST `/api/v1/admin/articles`

### GET `/api/v1/admin/articles/{articleId}`

### PUT `/api/v1/admin/articles/{articleId}`

### PATCH `/api/v1/admin/articles/{articleId}/publish`

### PATCH `/api/v1/admin/articles/{articleId}/archive`

---

## 5.9 系统配置管理

### GET `/api/v1/admin/system-configs`

### PUT `/api/v1/admin/system-configs/{configKey}`

---

## 6. Internal Job API

这些接口只允许内部服务或管理员触发。

## 6.1 触发补测

### POST `/api/v1/internal/jobs/retest`

请求体：

```json
{
  "siteId": 1,
  "modelId": 2,
  "priority": "high",
  "executionWindow": "next-3-hours"
}
```

## 6.2 触发重抓

### POST `/api/v1/internal/jobs/recrawl`

## 6.3 风险重算

### POST `/api/v1/internal/jobs/recalculate-risk`

## 6.4 排行重建

### POST `/api/v1/internal/jobs/rebuild-rankings`

请求体：

```json
{
  "rankingType": "price",
  "window": "7d"
}
```

---

## 7. DTO 分层要求

## 7.1 Public DTO

只暴露：

1. 公开价格
2. 公开测试摘要
3. 公开风险摘要
4. 公开站点基础信息

不暴露：

1. 内部测试账号
2. 抓取日志
3. 复核备注
4. 原始证据 JSON

## 7.2 Admin DTO

允许暴露：

1. 完整主数据
2. 任务执行状态
3. 失败原因
4. 复核信息
5. 内部备注

## 7.3 Internal Job DTO

用于：

1. 任务触发
2. 任务重算
3. 内部运维操作

---

## 8. 错误码体系

## 8.1 通用错误码

| code | 说明 |
|---|---|
| 40001 | 参数错误 |
| 40101 | 未认证 |
| 40301 | 无权限 |
| 40401 | 资源不存在 |
| 40901 | 资源冲突 |
| 42901 | 请求过于频繁 |
| 50001 | 系统内部错误 |
| 50201 | 外部依赖失败 |

## 8.2 自助测试专用错误码

| code | 说明 |
|---|---|
| 46001 | 测试地址无效 |
| 46002 | API Key 无效 |
| 46003 | 模型不存在 |
| 46004 | 测试超时 |
| 46005 | 测试额度不足 |

## 8.3 后台任务专用错误码

| code | 说明 |
|---|---|
| 47001 | 抓取失败 |
| 47002 | 测试任务失败 |
| 47003 | 风险重算失败 |
| 47004 | 任务冲突 |
| 47005 | 补测排队中 |

---

## 9. 当前结论

接口设计遵循两个原则：

1. 用户端与管理端虽然共用一个后端，但它们是两套独立客户端
2. 同一业务实体允许存在公开视图 DTO 与后台视图 DTO 两套形态

后续实现时：

1. 所有 Controller、DTO、OpenAPI 都必须以本设计为准
2. 不允许为了“统一模板”而强行合并公开接口和后台接口

## 10. 当前实现补充：2026-05-14

本节记录当前代码中已经落地的接口事实。后续更新正式 OpenAPI 时，应优先以本节和 Controller 实现为准。

### 10.1 Public API 当前接口

| 方法 | 路径 | 说明 |
|---|---|---|
| GET | `/api/v1/public/site-settings` | 公开站点名称和图标 |
| GET | `/api/v1/public/home/overview` | 首页统计和热门模型 |
| GET | `/api/v1/public/models` | 公开模型目录 |
| GET | `/api/v1/public/sites` | 公开站点目录 |
| GET | `/api/v1/public/sites/{siteSlug}` | 公开站点详情 |
| GET | `/api/v1/public/rankings/models/{modelSlug}` | 单模型排行 |
| GET | `/api/v1/public/tests/latest` | 最新测试记录 |
| GET | `/api/v1/public/tests/{id}` | 测试记录详情 |
| GET | `/api/v1/public/self-tests/challenge` | 自助测试挑战题 |
| POST | `/api/v1/public/self-tests` | 创建用户自助测试 |
| GET | `/api/v1/public/self-tests/{id}` | 查询用户自助测试 |
| POST | `/api/v1/public/submissions` | 提交中转站线索 |
| GET | `/api/v1/public/model-capabilities` | 模型能力榜 |
| GET | `/api/v1/public/articles` | 公开文章列表 |
| GET | `/api/v1/public/articles/{slug}` | 公开文章详情 |

### 10.2 Admin API 当前接口

| 方法 | 路径 | 说明 |
|---|---|---|
| GET | `/api/v1/admin/site-settings` | 读取站点设置 |
| PUT | `/api/v1/admin/site-settings` | 更新站点设置 |
| GET | `/api/v1/admin/sites` | 中转站列表 |
| GET | `/api/v1/admin/sites/{id}` | 中转站详情，含报价和最近测试 |
| POST | `/api/v1/admin/sites` | 新增中转站 |
| PUT | `/api/v1/admin/sites/{id}` | 更新中转站 |
| PATCH | `/api/v1/admin/sites/{id}/status` | 修改中转站状态 |
| POST | `/api/v1/admin/sites/pricing-preview` | 抓取中转价格预览 |
| GET | `/api/v1/admin/models` | 模型列表 |
| GET | `/api/v1/admin/models/{id}` | 模型详情 |
| POST | `/api/v1/admin/models` | 新增模型 |
| PUT | `/api/v1/admin/models/{id}` | 更新模型 |
| PATCH | `/api/v1/admin/models/{id}/status` | 修改模型状态 |
| PATCH | `/api/v1/admin/models/{id}/metadata` | 修改模型排序和热门 |
| POST | `/api/v1/admin/models/import-preview` | 通用导入预览 |
| POST | `/api/v1/admin/models/openrouter-preview` | OpenRouter 抓取预览 |
| POST | `/api/v1/admin/models/import` | 批量导入模型 |
| GET | `/api/v1/admin/model-providers` | 模型提供商列表 |
| GET | `/api/v1/admin/model-providers/active` | 启用模型提供商 |
| GET | `/api/v1/admin/model-providers/{id}` | 模型提供商详情 |
| POST | `/api/v1/admin/model-providers` | 新增模型提供商 |
| PUT | `/api/v1/admin/model-providers/{id}` | 更新模型提供商 |
| PATCH | `/api/v1/admin/model-providers/{id}/status` | 修改模型提供商状态 |
| GET | `/api/v1/admin/offers` | 报价快照 |
| POST | `/api/v1/admin/offers/{offerId}/verify` | 人工确认报价 |
| GET | `/api/v1/admin/test-records` | 后台测试记录 |
| GET | `/api/v1/admin/risks` | 风险列表 |
| GET | `/api/v1/admin/risks/{riskId}` | 风险详情 |
| POST | `/api/v1/admin/risks/{riskId}/review` | 风险复核 |
| POST | `/api/v1/admin/risks/recalculate` | 风险重算 |
| GET | `/api/v1/admin/job-logs` | 任务执行日志 |
| POST | `/api/v1/admin/crawl-jobs/manual-run` | 手动抓取 |
| POST | `/api/v1/admin/test-jobs/manual-run` | 手动测试 |
| POST | `/api/v1/admin/rankings/rebuild` | 重建排行 |
| GET | `/api/v1/admin/submissions` | 线索列表 |
| POST | `/api/v1/admin/submissions/{id}/approve` | 线索审核通过 |
| POST | `/api/v1/admin/submissions/{id}/reject` | 线索审核拒绝 |
| GET | `/api/v1/admin/articles` | 文章列表 |
| GET | `/api/v1/admin/articles/{id}` | 文章详情 |
| POST | `/api/v1/admin/articles` | 新增文章 |
| PUT | `/api/v1/admin/articles/{id}` | 更新文章 |
| PATCH | `/api/v1/admin/articles/{id}/publish` | 发布文章 |
| PATCH | `/api/v1/admin/articles/{id}/archive` | 归档文章 |

### 10.3 模型 DTO 当前口径

后台模型列表和详情必须包含：

| 字段 | 说明 |
|---|---|
| `providerId` | 关联模型提供商 |
| `providerName` | 模型提供商名称 |
| `vendor` | 兼容厂商名 |
| `officialModelId` | 第三方原始 ID，例如 `openai/gpt-5.4` |
| `requestName` | 真实请求模型名，例如 `gpt-5.4` |
| `apiType` | `openai` / `anthropic` |
| `displayName` | 展示名，例如 `GPT-5.4` |
| `isHot` | 是否首页热门 |
| `sortOrder` | 排序 |
| `officialInputPriceUsd` | 官方输入价，USD / 1M tokens |
| `officialOutputPriceUsd` | 官方输出价，USD / 1M tokens |
| `capabilityScore` | 能力评分 |
| `capabilitySource` | 能力评分来源 |

### 10.4 中转价格预览当前口径

`POST /api/v1/admin/sites/pricing-preview` 当前用于抓取并预览中转站报价。

请求字段包括：

| 字段 | 说明 |
|---|---|
| `siteType` | `auto` / `newapi` / `oneapi` / `onehub` |
| `baseUrl` | 中转站 API Base URL |
| `apiKey` | 抓取使用的 API Key |
| `rechargeRatio` | 充值倍率 |
| `bonusRatio` | 赠送倍率 |
| `priceBaseline` | 价格基线 |
| `group` | 用户分组 |

返回预览项必须包含：

| 字段 | 说明 |
|---|---|
| `modelId` | 匹配到的模型 ID，可空 |
| `officialModelId` | 第三方原始 ID |
| `requestName` | 模型请求名称 |
| `apiType` | 接口类型 |
| `displayName` | 展示名 |
| `officialInputPriceUsd` | 官方输入价 |
| `officialOutputPriceUsd` | 官方输出价 |
| `siteInputPriceUsd` | 站点输入价 |
| `siteOutputPriceUsd` | 站点输出价 |
| `effectiveInputPriceUsd` | 折算输入价 |
| `effectiveOutputPriceUsd` | 折算输出价 |
