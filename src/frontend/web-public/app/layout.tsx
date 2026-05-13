import type { Metadata } from "next";
import "./globals.css";

export const metadata: Metadata = {
  title: "CheapAI - AI 中转站比价与风险识别",
  description: "按实际折算价、稳定性测试和风险证据筛选 AI 中转服务。"
};

export default function RootLayout({ children }: Readonly<{ children: React.ReactNode }>) {
  return (
    <html lang="zh-CN">
      <body>{children}</body>
    </html>
  );
}
