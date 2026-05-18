const { chromium } = require("playwright");
const fs = require("node:fs");
const path = require("node:path");

const args = parseArgs(process.argv.slice(2));
const publicUrl = trimTrailingSlash(args.publicUrl || "http://127.0.0.1:3000");
const apiUrl = trimTrailingSlash(args.apiUrl || "http://127.0.0.1:5157");
const timestamp = new Date().toISOString().replace(/[:.]/g, "-");
const reportDir = path.resolve(args.outDir || path.join("artifacts", "frontend-tests", timestamp));
const screenshotDir = path.join(reportDir, "screenshots");
const reportPath = path.join(reportDir, "public-frontend-smoke-report.md");
const jsonPath = path.join(reportDir, "public-frontend-smoke-report.json");

const checks = [];
const artifacts = [];

const apiChecks = [
  {
    id: "api-health",
    name: "API 健康检查",
    url: `${apiUrl}/api/health`,
    assert: (data) => data && data.status === "ok"
  },
  {
    id: "api-site-settings",
    name: "站点配置接口",
    url: `${apiUrl}/api/v1/public/site-settings`,
    assert: (data) => data?.code === 0 && Boolean(data?.data?.siteName)
  },
  {
    id: "api-home-overview",
    name: "首页概览接口",
    url: `${apiUrl}/api/v1/public/home/overview`,
    assert: (data) => data?.code === 0 && data?.data?.stats
  },
  {
    id: "api-models",
    name: "模型列表接口",
    url: `${apiUrl}/api/v1/public/models?page=1&pageSize=20`,
    assert: (data) => data?.code === 0 && Array.isArray(data?.data?.items)
  },
  {
    id: "api-sites",
    name: "中转站列表接口",
    url: `${apiUrl}/api/v1/public/sites?page=1&pageSize=20`,
    assert: (data) => data?.code === 0 && Array.isArray(data?.data?.items)
  },
  {
    id: "api-rankings",
    name: "价格排行接口",
    url: `${apiUrl}/api/v1/public/rankings/cheapest?modelSlugs=gpt-4-1-mini&limit=5`,
    assert: (data) => data?.code === 0 && Array.isArray(data?.data)
  },
  {
    id: "api-tests-latest",
    name: "最新测试接口",
    url: `${apiUrl}/api/v1/public/tests/latest?page=1&pageSize=5`,
    assert: (data) => data?.code === 0 && Array.isArray(data?.data?.items)
  },
  {
    id: "api-capabilities",
    name: "模型能力榜接口",
    url: `${apiUrl}/api/v1/public/model-capabilities`,
    assert: (data) => data?.code === 0 && Array.isArray(data?.data)
  }
];

const pageChecks = [
  {
    id: "public-home-desktop",
    name: "首页桌面端",
    url: "/",
    viewport: { width: 1440, height: 1000 },
    requiredText: ["先看低价，再看风险", "主流模型低价排行", "直接测试中转接口"]
  },
  {
    id: "public-home-mobile",
    name: "首页移动端",
    url: "/",
    viewport: { width: 390, height: 844 },
    requiredText: ["先看低价，再看风险", "主流模型低价排行"]
  },
  {
    id: "models",
    name: "模型大全",
    url: "/models",
    viewport: { width: 1440, height: 1000 },
    requiredText: ["模型大全", "官方价", "最便宜中转"]
  },
  {
    id: "rankings",
    name: "价格排行",
    url: "/rankings",
    viewport: { width: 1440, height: 1000 },
    requiredText: ["价格排行", "模型选择", "查看排行", "站点", "折算价"]
  },
  {
    id: "legacy-ranking-redirect",
    name: "旧价格排行路径兼容",
    url: "/rankings/gpt-4-1-mini",
    viewport: { width: 1440, height: 1000 },
    requiredText: ["模型选择", "查看排行"]
  },
  {
    id: "sites",
    name: "中转站大全",
    url: "/sites",
    viewport: { width: 1440, height: 1000 },
    requiredText: ["中转站大全", "综合评分"]
  },
  {
    id: "site-detail",
    name: "中转站详情",
    url: "/sites/relay-port",
    viewport: { width: 1440, height: 1000 },
    requiredText: ["支持模型", "价格摘要", "最近测试摘要"]
  },
  {
    id: "tests",
    name: "中转测试",
    url: "/tests",
    viewport: { width: 1440, height: 1000 },
    requiredText: ["中转测试", "直接测试中转接口", "我的测试", "所有测试"]
  },
  {
    id: "test-detail-deeplink",
    name: "测试详情深链",
    url: "/tests?detail=1",
    viewport: { width: 1440, height: 1000 },
    requiredText: ["中转测试"]
  },
  {
    id: "capabilities",
    name: "模型能力榜",
    url: "/capabilities",
    viewport: { width: 1440, height: 1000 },
    requiredText: ["模型能力榜", "能力分"]
  },
  {
    id: "articles",
    name: "文章列表",
    url: "/articles",
    viewport: { width: 1440, height: 1000 },
    requiredText: ["文章"]
  },
  {
    id: "self-test",
    name: "自助测试表单",
    url: "/self-test",
    viewport: { width: 1440, height: 1000 },
    requiredText: ["中转接口自助测试", "API 接口地址", "API Key"]
  },
  {
    id: "submissions",
    name: "站点提交",
    url: "/submissions",
    viewport: { width: 1440, height: 1000 },
    requiredText: ["提交"]
  }
];

const interactionChecks = [
  {
    id: "ranking-form-submit",
    name: "价格排行模型选择提交",
    run: async (page) => {
      await page.goto(`${publicUrl}/rankings`, { waitUntil: "domcontentloaded", timeout: 30000 });
      const select = page.locator("select[name='model']");
      await select.waitFor({ state: "visible", timeout: 15000 });
      const values = await select.locator("option").evaluateAll((options) => options.map((option) => option.value).filter(Boolean));
      if (values.length === 0) {
        throw new Error("没有可选模型");
      }

      await select.selectOption(values[0]);
      await Promise.all([
        page.waitForURL(/\/rankings\?model=/, { timeout: 15000 }),
        page.getByRole("button", { name: "查看排行" }).click()
      ]);
    }
  },
  {
    id: "tests-all-tab",
    name: "中转测试所有测试切换",
    run: async (page) => {
      await page.goto(`${publicUrl}/tests`, { waitUntil: "domcontentloaded", timeout: 30000 });
      await page.getByRole("button", { name: "所有测试" }).click();
      await page.getByText("测试结果", { exact: true }).waitFor({ state: "visible", timeout: 15000 });
    }
  }
];

main().catch((error) => {
  console.error(error);
  process.exitCode = 1;
});

async function main() {
  fs.mkdirSync(screenshotDir, { recursive: true });

  await runApiChecks();

  const browser = await chromium.launch({ headless: true });
  try {
    for (const pageCheck of pageChecks) {
      await runPageCheck(browser, pageCheck);
    }

    for (const interactionCheck of interactionChecks) {
      await runInteractionCheck(browser, interactionCheck);
    }
  } finally {
    await browser.close();
  }

  writeReports();

  const failed = checks.filter((check) => check.status === "FAIL");
  if (failed.length > 0) {
    process.exitCode = 1;
  }
}

async function runApiChecks() {
  for (const apiCheck of apiChecks) {
    const startedAt = Date.now();
    try {
      const response = await fetch(apiCheck.url);
      const statusCode = response.status;
      const body = await response.text();
      let data = null;

      try {
        data = body ? JSON.parse(body) : null;
      } catch {
        data = null;
      }

      if (!response.ok) {
        throw new Error(`HTTP ${statusCode}: ${body.slice(0, 300)}`);
      }

      if (!apiCheck.assert(data)) {
        throw new Error(`响应结构不符合预期: ${body.slice(0, 300)}`);
      }

      addCheck(apiCheck.id, apiCheck.name, "PASS", `HTTP ${statusCode}`, Date.now() - startedAt);
    } catch (error) {
      addCheck(apiCheck.id, apiCheck.name, "FAIL", error.message, Date.now() - startedAt);
    }
  }
}

async function runPageCheck(browser, pageCheck) {
  const startedAt = Date.now();
  const context = await browser.newContext({ viewport: pageCheck.viewport });
  const page = await context.newPage();
  const errors = [];
  const failedResponses = [];

  page.on("pageerror", (error) => errors.push(error.message));
  page.on("console", (message) => {
    if (message.type() === "error") {
      errors.push(message.text());
    }
  });
  page.on("response", (response) => {
    const status = response.status();
    const url = response.url();
    if (status >= 400 && (url.startsWith(publicUrl) || url.startsWith(apiUrl))) {
      failedResponses.push(`${status} ${url}`);
    }
  });

  try {
    const response = await page.goto(`${publicUrl}${pageCheck.url}`, {
      waitUntil: "domcontentloaded",
      timeout: 30000
    });
    const status = response?.status() ?? 0;

    await page.waitForLoadState("networkidle", { timeout: 15000 }).catch(() => {});
    await page.waitForFunction(() => document.body && document.body.innerText.trim().length > 0, null, { timeout: 15000 });

    const text = await page.locator("body").innerText({ timeout: 5000 });
    const screenshotPath = path.join(screenshotDir, `${pageCheck.id}.png`);
    await page.screenshot({ path: screenshotPath, fullPage: true });
    artifacts.push(toRelative(screenshotPath));

    if (status >= 400) {
      throw new Error(`页面 HTTP ${status}`);
    }

    assertNoRuntimeError(text);
    assertRequiredText(text, pageCheck.requiredText);

    if (errors.length > 0) {
      throw new Error(`控制台/运行时错误: ${errors.slice(0, 3).join(" | ")}`);
    }

    const criticalFailedResponses = failedResponses.filter((item) => !item.includes("/icon.svg"));
    if (criticalFailedResponses.length > 0) {
      throw new Error(`页面请求失败: ${criticalFailedResponses.slice(0, 3).join(" | ")}`);
    }

    addCheck(pageCheck.id, pageCheck.name, "PASS", `HTTP ${status}`, Date.now() - startedAt, screenshotPath);
  } catch (error) {
    const screenshotPath = path.join(screenshotDir, `${pageCheck.id}-failed.png`);
    await page.screenshot({ path: screenshotPath, fullPage: true }).catch(() => {});
    artifacts.push(toRelative(screenshotPath));
    addCheck(pageCheck.id, pageCheck.name, "FAIL", error.message, Date.now() - startedAt, screenshotPath);
  } finally {
    await context.close();
  }
}

async function runInteractionCheck(browser, interactionCheck) {
  const startedAt = Date.now();
  const context = await browser.newContext({ viewport: { width: 1440, height: 1000 } });
  const page = await context.newPage();
  const errors = [];

  page.on("pageerror", (error) => errors.push(error.message));
  page.on("console", (message) => {
    if (message.type() === "error") {
      errors.push(message.text());
    }
  });

  try {
    await interactionCheck.run(page);
    const text = await page.locator("body").innerText({ timeout: 5000 });
    assertNoRuntimeError(text);

    if (errors.length > 0) {
      throw new Error(`控制台/运行时错误: ${errors.slice(0, 3).join(" | ")}`);
    }

    const screenshotPath = path.join(screenshotDir, `${interactionCheck.id}.png`);
    await page.screenshot({ path: screenshotPath, fullPage: true });
    artifacts.push(toRelative(screenshotPath));
    addCheck(interactionCheck.id, interactionCheck.name, "PASS", "交互完成", Date.now() - startedAt, screenshotPath);
  } catch (error) {
    const screenshotPath = path.join(screenshotDir, `${interactionCheck.id}-failed.png`);
    await page.screenshot({ path: screenshotPath, fullPage: true }).catch(() => {});
    artifacts.push(toRelative(screenshotPath));
    addCheck(interactionCheck.id, interactionCheck.name, "FAIL", error.message, Date.now() - startedAt, screenshotPath);
  } finally {
    await context.close();
  }
}

function addCheck(id, name, status, detail, durationMs, screenshotPath) {
  const row = {
    id,
    name,
    status,
    detail: sanitizeDetail(detail),
    durationMs,
    screenshot: screenshotPath ? toReportRelative(screenshotPath) : ""
  };
  checks.push(row);
  console.log(`[${status}] ${name} - ${row.detail}`);
}

function assertNoRuntimeError(text) {
  const markers = [
    "Runtime Error",
    "Application error",
    "Unhandled Runtime Error",
    "Cannot find module",
    "React Client Manifest",
    "__webpack_modules__"
  ];
  const found = markers.find((marker) => text.includes(marker));
  if (found) {
    throw new Error(`页面出现运行时错误标记: ${found}`);
  }
}

function assertRequiredText(text, requiredText) {
  const missing = requiredText.filter((item) => !text.includes(item));
  if (missing.length > 0) {
    throw new Error(`缺少关键文案: ${missing.join(", ")}`);
  }
}

function writeReports() {
  fs.mkdirSync(reportDir, { recursive: true });

  const total = checks.length;
  const passed = checks.filter((check) => check.status === "PASS").length;
  const failed = checks.filter((check) => check.status === "FAIL").length;
  const status = failed === 0 ? "PASSING" : "FAILING";

  const markdown = [
    "# CheapAI 用户端自动化测试报告",
    "",
    `- 测试时间：${new Date().toLocaleString("zh-CN", { hour12: false })}`,
    `- 用户端地址：${publicUrl}`,
    `- API 地址：${apiUrl}`,
    `- 总体状态：${status}`,
    `- 结果统计：总计 ${total}，通过 ${passed}，失败 ${failed}`,
    "",
    "## 测试方案",
    "",
    "| 层级 | 覆盖目标 | 通过标准 |",
    "|---|---|---|",
    "| API 合约 | 健康检查、站点配置、首页概览、模型、中转站、价格排行、测试记录、能力榜 | HTTP 2xx 且响应结构符合页面消费字段 |",
    "| 页面加载 | 首页、模型大全、价格排行、中转站列表/详情、中转测试、能力榜、文章、自助测试、提交页 | 页面 HTTP < 400，无 Next Runtime Error，无关键文案缺失 |",
    "| 交互冒烟 | 价格排行模型选择提交、中转测试“所有测试”切换 | 交互完成后页面无控制台错误和运行时错误 |",
    "| 响应式 | 首页移动端核心区域 | 移动视口可渲染核心文案 |",
    "",
    "## 执行结果",
    "",
    "| 用例 | 状态 | 耗时 | 详情 | 截图 |",
    "|---|---|---:|---|---|",
    ...checks.map((check) =>
      `| ${check.name} | ${check.status} | ${check.durationMs}ms | ${escapePipe(check.detail)} | ${check.screenshot ? `[截图](${normalizeMarkdownPath(check.screenshot)})` : ""} |`
    ),
    "",
    "## 失败项分析",
    ""
  ];

  const failedChecks = checks.filter((check) => check.status === "FAIL");
  if (failedChecks.length === 0) {
    markdown.push("未发现失败项。");
  } else {
    for (const check of failedChecks) {
      markdown.push(`### ${check.name}`);
      markdown.push("");
      markdown.push(`- 用例 ID：\`${check.id}\``);
      markdown.push(`- 错误：${check.detail}`);
      if (check.screenshot) {
        markdown.push(`- 截图：${check.screenshot}`);
      }
      markdown.push("");
    }
  }

  markdown.push("## 建议");
  markdown.push("");
  if (failedChecks.some((check) => check.detail.includes("Cannot find module") || check.detail.includes("React Client Manifest") || check.detail.includes("运行时错误"))) {
    markdown.push("- 当前 3000 前端服务存在 `.next` 构建缓存/开发服务产物错位。建议停止当前 public web 进程，删除 `src/frontend/web-public/.next` 后重新启动。");
  }
  markdown.push("- 将该脚本纳入回归流程：后端/前端启动后执行 `node tools/acceptance/public-frontend-smoke.cjs --public-url http://127.0.0.1:3000 --api-url http://127.0.0.1:5157`。");
  markdown.push("- 对真实自助测试提交单独做受控环境测试，避免在自动化里传递真实 API Key。");
  markdown.push("");

  fs.writeFileSync(reportPath, markdown.join("\n"), "utf8");
  fs.writeFileSync(
    jsonPath,
    JSON.stringify(
      {
        publicUrl,
        apiUrl,
        generatedAt: new Date().toISOString(),
        total,
        passed,
        failed,
        checks,
        artifacts
      },
      null,
      2
    ),
    "utf8"
  );

  console.log(`Report: ${reportPath}`);
  console.log(`JSON: ${jsonPath}`);
}

function parseArgs(values) {
  const result = {};
  for (let index = 0; index < values.length; index += 1) {
    const value = values[index];
    if (value === "--public-url") {
      result.publicUrl = values[index + 1];
      index += 1;
    } else if (value === "--api-url") {
      result.apiUrl = values[index + 1];
      index += 1;
    } else if (value === "--out-dir") {
      result.outDir = values[index + 1];
      index += 1;
    }
  }
  return result;
}

function sanitizeDetail(value) {
  return String(value || "").replace(/\s+/g, " ").slice(0, 800);
}

function escapePipe(value) {
  return String(value || "").replace(/\|/g, "/");
}

function trimTrailingSlash(value) {
  return value.replace(/\/+$/, "");
}

function toRelative(value) {
  return path.relative(process.cwd(), value).replace(/\\/g, "/");
}

function toReportRelative(value) {
  return path.relative(reportDir, value).replace(/\\/g, "/");
}

function normalizeMarkdownPath(value) {
  return value.replace(/ /g, "%20");
}
