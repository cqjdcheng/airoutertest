const fs = require("node:fs");
const path = require("node:path");

const args = parseArgs(process.argv.slice(2));
const apiUrl = trimTrailingSlash(args.apiUrl || "http://127.0.0.1:5157");
const publicUrl = trimTrailingSlash(args.publicUrl || "http://127.0.0.1:3000");
const concurrency = clampNumber(Number(args.concurrency ?? 8), 1, 64);
const requestsPerTarget = clampNumber(Number(args.requests ?? 20), 1, 500);
const timeoutMs = clampNumber(Number(args.timeoutMs ?? 10000), 1000, 60000);
const timestamp = new Date().toISOString().replace(/[:.]/g, "-");
const reportDir = path.resolve(args.outDir || path.join("artifacts", "acceptance", `concurrency-${timestamp}`));
const jsonPath = path.join(reportDir, "concurrency-smoke-report.json");
const markdownPath = path.join(reportDir, "concurrency-smoke-report.md");

const targets = [
  { id: "api-health", name: "API 健康检查", url: `${apiUrl}/api/health` },
  { id: "api-home-overview", name: "首页概览接口", url: `${apiUrl}/api/v1/public/home/overview` },
  { id: "api-sites", name: "中转站列表接口", url: `${apiUrl}/api/v1/public/sites?page=1&pageSize=20` },
  { id: "api-tests-latest", name: "最新测试接口", url: `${apiUrl}/api/v1/public/tests/latest?page=1&pageSize=5` },
  { id: "api-models", name: "模型选项接口", url: `${apiUrl}/api/v1/public/models?page=1&pageSize=5` },
  { id: "public-home", name: "首页页面", url: `${publicUrl}/` },
  { id: "public-sites", name: "中转站列表页面", url: `${publicUrl}/sites` },
  { id: "public-tests", name: "测试记录页面", url: `${publicUrl}/tests` },
  { id: "public-models-redirect", name: "模型旧路径重定向", url: `${publicUrl}/models` },
  { id: "public-rankings-redirect", name: "排行旧路径重定向", url: `${publicUrl}/rankings` }
];

main().catch((error) => {
  console.error(error);
  process.exitCode = 1;
});

async function main() {
  const startedAt = Date.now();
  const results = [];

  for (const target of targets) {
    results.push(await runTarget(target));
  }

  const failedTargets = results.filter((result) => result.errors > 0 || result.status5xx > 0);
  const report = {
    startedAt: new Date(startedAt).toISOString(),
    durationMs: Date.now() - startedAt,
    apiUrl,
    publicUrl,
    concurrency,
    requestsPerTarget,
    timeoutMs,
    status: failedTargets.length === 0 ? "PASS" : "FAIL",
    targets: results
  };

  writeReports(report);

  console.log(`Concurrency smoke: ${report.status}`);
  console.log(`Targets: ${results.length}, requests: ${results.reduce((sum, item) => sum + item.total, 0)}, report: ${markdownPath}`);

  if (failedTargets.length > 0) {
    for (const result of failedTargets) {
      console.error(`${result.id}: errors=${result.errors}, 5xx=${result.status5xx}`);
    }
    process.exitCode = 1;
  }
}

async function runTarget(target) {
  const latencies = [];
  const statuses = {};
  const errors = [];
  const tasks = Array.from({ length: requestsPerTarget }, (_, index) => index);
  let cursor = 0;

  async function worker() {
    while (cursor < tasks.length) {
      const index = cursor++;
      const startedAt = Date.now();
      try {
        const response = await fetchWithTimeout(target.url, timeoutMs);
        const latencyMs = Date.now() - startedAt;
        latencies.push(latencyMs);
        statuses[response.status] = (statuses[response.status] || 0) + 1;
        if (response.status >= 500) {
          errors.push(`request ${index + 1}: HTTP ${response.status}`);
        }
        await response.arrayBuffer();
      } catch (error) {
        latencies.push(Date.now() - startedAt);
        errors.push(`request ${index + 1}: ${error.message}`);
      }
    }
  }

  await Promise.all(Array.from({ length: Math.min(concurrency, requestsPerTarget) }, worker));

  const sorted = [...latencies].sort((a, b) => a - b);
  return {
    id: target.id,
    name: target.name,
    url: target.url,
    total: requestsPerTarget,
    statuses,
    errors: errors.length,
    status5xx: Object.entries(statuses)
      .filter(([status]) => Number(status) >= 500)
      .reduce((sum, [, count]) => sum + count, 0),
    minMs: sorted[0] ?? 0,
    avgMs: sorted.length === 0 ? 0 : Math.round(sorted.reduce((sum, value) => sum + value, 0) / sorted.length),
    p95Ms: percentile(sorted, 0.95),
    maxMs: sorted.at(-1) ?? 0,
    samples: errors.slice(0, 5)
  };
}

async function fetchWithTimeout(url, timeoutMs) {
  const controller = new AbortController();
  const timeout = setTimeout(() => controller.abort(), timeoutMs);
  try {
    return await fetch(url, {
      redirect: "follow",
      signal: controller.signal,
      headers: {
        "user-agent": "CheapAI-Concurrency-Smoke/0.1"
      }
    });
  } finally {
    clearTimeout(timeout);
  }
}

function writeReports(report) {
  fs.mkdirSync(reportDir, { recursive: true });
  fs.writeFileSync(jsonPath, JSON.stringify(report, null, 2), "utf8");

  const markdown = [
    "# CheapAI 并发 Smoke 报告",
    "",
    `- 状态：${report.status}`,
    `- API：${report.apiUrl}`,
    `- 用户端：${report.publicUrl}`,
    `- 并发数：${report.concurrency}`,
    `- 每目标请求数：${report.requestsPerTarget}`,
    `- 总耗时：${report.durationMs}ms`,
    "",
    "| 目标 | 状态码 | 错误 | 5xx | avg | p95 | max |",
    "|---|---|---:|---:|---:|---:|---:|",
    ...report.targets.map((target) =>
      `| ${target.name} | ${formatStatuses(target.statuses)} | ${target.errors} | ${target.status5xx} | ${target.avgMs}ms | ${target.p95Ms}ms | ${target.maxMs}ms |`
    )
  ];

  const samples = report.targets.flatMap((target) => target.samples.map((sample) => `- ${target.name}: ${sample}`));
  if (samples.length > 0) {
    markdown.push("", "## 错误样本", "", ...samples);
  }

  fs.writeFileSync(markdownPath, `${markdown.join("\n")}\n`, "utf8");
}

function percentile(sorted, ratio) {
  if (sorted.length === 0) return 0;
  return sorted[Math.min(sorted.length - 1, Math.ceil(sorted.length * ratio) - 1)];
}

function formatStatuses(statuses) {
  return Object.entries(statuses)
    .sort(([left], [right]) => Number(left) - Number(right))
    .map(([status, count]) => `${status}:${count}`)
    .join(", ");
}

function parseArgs(argv) {
  const parsed = {};
  for (let index = 0; index < argv.length; index += 1) {
    const arg = argv[index];
    if (!arg.startsWith("--")) continue;
    const key = arg.slice(2).replace(/-([a-z])/g, (_, letter) => letter.toUpperCase());
    const next = argv[index + 1];
    if (!next || next.startsWith("--")) {
      parsed[key] = true;
      continue;
    }
    parsed[key] = next;
    index += 1;
  }
  return parsed;
}

function trimTrailingSlash(value) {
  return value.replace(/\/+$/, "");
}

function clampNumber(value, min, max) {
  if (!Number.isFinite(value)) return min;
  return Math.min(Math.max(Math.trunc(value), min), max);
}
