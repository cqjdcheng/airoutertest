import type { Metadata } from "next";
import { PublicBrandProvider } from "@/app/components/PublicBrandProvider";
import { PublicFooter } from "@/app/components/PublicFooter";
import { getPublicSiteSettings } from "@/lib/siteSettings";
import "./globals.css";

const description = "按实际折算价、稳定性测试和风险证据筛选 AI 中转服务。";

export async function generateMetadata(): Promise<Metadata> {
  const settings = await getPublicSiteSettings();

  return {
    title: `${settings.siteName} - AI 中转站比价与风险识别`,
    description,
    icons: settings.siteIconUrl ? [{ url: settings.siteIconUrl }] : undefined
  };
}

export default async function RootLayout({ children }: Readonly<{ children: React.ReactNode }>) {
  const settings = await getPublicSiteSettings();

  return (
    <html lang="zh-CN">
      <body>
        <PublicBrandProvider initialBranding={settings}>
          <div className="site-frame">
            {children}
            <PublicFooter />
          </div>
        </PublicBrandProvider>
      </body>
    </html>
  );
}
