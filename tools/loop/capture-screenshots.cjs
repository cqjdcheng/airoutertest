const { chromium } = require("playwright");
const fs = require("node:fs");
const path = require("node:path");

const [, , screenshotDir, publicUrl, adminUrl, apiUrl, modelSlug, siteSlug, adminUsername, adminPassword] = process.argv;

if (!screenshotDir || !publicUrl || !adminUrl || !apiUrl || !modelSlug || !siteSlug || !adminUsername || !adminPassword) {
  console.error(
    "Usage: node capture-screenshots.cjs <dir> <publicUrl> <adminUrl> <apiUrl> <modelSlug> <siteSlug> <adminUsername> <adminPassword>"
  );
  process.exit(1);
}

const targets = [
  { name: "public-home", url: publicUrl, viewport: { width: 1440, height: 1000 } },
  { name: "public-home-mobile", url: publicUrl, viewport: { width: 390, height: 844 } },
  { name: "public-tests", url: `${publicUrl}/tests`, viewport: { width: 1440, height: 1000 } },
  { name: "public-sites-list", url: `${publicUrl}/sites`, viewport: { width: 1440, height: 1000 } },
  { name: "public-models-list", url: `${publicUrl}/models`, viewport: { width: 1440, height: 1000 } },
  { name: "public-ranking", url: `${publicUrl}/rankings/${modelSlug}`, viewport: { width: 1440, height: 1000 } },
  { name: "public-site", url: `${publicUrl}/sites/${siteSlug}`, viewport: { width: 1440, height: 1000 } },
  { name: "public-self-test", url: `${publicUrl}/self-test`, viewport: { width: 1440, height: 1000 } },
  { name: "public-submission", url: `${publicUrl}/submissions`, viewport: { width: 1440, height: 1000 } },
  { name: "public-capabilities", url: `${publicUrl}/capabilities`, viewport: { width: 1440, height: 1000 } },
  { name: "public-articles", url: `${publicUrl}/articles`, viewport: { width: 1440, height: 1000 } },
  { name: "public-article-detail", url: `${publicUrl}/articles/cheapai-data-policy`, viewport: { width: 1440, height: 1000 } },
  { name: "admin-login", url: `${adminUrl}/login`, viewport: { width: 1440, height: 1000 } },
  { name: "admin-dashboard", url: `${adminUrl}/`, viewport: { width: 1440, height: 1000 }, authenticated: true },
  { name: "admin-sites", url: `${adminUrl}/sites`, viewport: { width: 1440, height: 1000 }, authenticated: true },
  { name: "admin-models", url: `${adminUrl}/models`, viewport: { width: 1440, height: 1000 }, authenticated: true },
  { name: "admin-offers", url: `${adminUrl}/offers`, viewport: { width: 1440, height: 1000 }, authenticated: true },
  { name: "admin-tests", url: `${adminUrl}/tests`, viewport: { width: 1440, height: 1000 }, authenticated: true },
  { name: "admin-risks", url: `${adminUrl}/risks`, viewport: { width: 1440, height: 1000 }, authenticated: true },
  { name: "admin-risk-detail", url: `${adminUrl}/risks`, viewport: { width: 1440, height: 1000 }, authenticated: true, action: "open-risk-detail" },
  { name: "admin-jobs", url: `${adminUrl}/jobs`, viewport: { width: 1440, height: 1000 }, authenticated: true },
  { name: "admin-submissions", url: `${adminUrl}/submissions`, viewport: { width: 1440, height: 1000 }, authenticated: true },
  { name: "admin-articles", url: `${adminUrl}/articles`, viewport: { width: 1440, height: 1000 }, authenticated: true }
];

async function waitForRenderablePage(page) {
  await page.waitForLoadState("networkidle", { timeout: 30000 }).catch(() => {});
  await page.waitForFunction(() => document.body && document.body.innerText.trim().length > 0, null, {
    timeout: 30000
  });
  await page.waitForTimeout(500);
}

(async () => {
  fs.mkdirSync(screenshotDir, { recursive: true });
  const browser = await chromium.launch({ headless: true });
  const pageErrors = [];
  let accessToken = "";

  const loginResponse = await fetch(`${apiUrl}/api/v1/auth/login`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ username: adminUsername, password: adminPassword })
  });
  if (loginResponse.ok) {
    const loginJson = await loginResponse.json();
    accessToken = loginJson?.data?.accessToken ?? "";
  }

  try {
    for (const target of targets) {
      const context = await browser.newContext({ viewport: target.viewport });
      if (target.authenticated && accessToken) {
        await context.addInitScript((token) => {
          window.localStorage.setItem("cheapai_admin_access_token", token);
        }, accessToken);
      }
      const page = await context.newPage();
      page.on("pageerror", (error) => pageErrors.push(`${target.name}: ${error.message}`));
      await page.goto(target.url, { waitUntil: "domcontentloaded", timeout: 30000 });
      await waitForRenderablePage(page);
      if (target.action === "open-risk-detail") {
        await page.getByText("查看证据").first().click();
        await page.getByText("风险证据详情").waitFor({ timeout: 10000 });
        await page.waitForTimeout(300);
      }
      await page.screenshot({
        path: path.join(screenshotDir, `${target.name}.png`),
        fullPage: true
      });
      await context.close();
    }
  } finally {
    await browser.close();
  }

  if (pageErrors.length > 0) {
    console.error(pageErrors.join("\n"));
    process.exit(1);
  }
})().catch((error) => {
  console.error(error);
  process.exit(1);
});
