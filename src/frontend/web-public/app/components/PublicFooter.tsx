"use client";

import Link from "next/link";
import { usePublicBranding } from "@/app/components/PublicBrandProvider";

const footerGroups = [
  {
    title: "平台维度",
    links: [
      { href: "/sites", label: "中转站大全" },
      { href: "/tests", label: "测试口径" }
    ]
  },
  {
    title: "决策维度",
    links: [
      { href: "/capabilities", label: "能力榜" },
      { href: "/articles", label: "文章资料" },
      { href: "/tests", label: "自助测试" }
    ]
  },
  {
    title: "使用原则",
    links: [
      { href: "/sites", label: "先看稳定性" },
      { href: "/tests", label: "再看测试记录" },
      { href: "/contact", label: "最后人工确认" }
    ]
  },
  {
    title: "联系",
    links: [{ href: "/contact", label: "联系我" }]
  }
];

export function PublicFooter() {
  const branding = usePublicBranding();

  return (
    <footer className="site-footer">
      <div className="public-container site-footer__grid">
        {footerGroups.map((group) => (
          <section key={group.title}>
            <h2>{group.title}</h2>
            <div className="site-footer__links">
              {group.links.map((link) => (
                <Link key={link.href} href={link.href}>
                  {link.label}
                </Link>
              ))}
            </div>
          </section>
        ))}
      </div>

      <div className="public-container site-footer__legal">
        <span>{branding.siteName}</span>
        <span>中转站稳定性测试、风险识别与真实测速参考</span>
      </div>
    </footer>
  );
}
