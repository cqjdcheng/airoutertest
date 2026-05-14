"use client";

import Link from "next/link";
import { usePublicBranding } from "@/app/components/PublicBrandProvider";

const footerGroups = [
  {
    title: "平台维度",
    links: [
      { href: "/sites", label: "中转站大全" },
      { href: "/models", label: "模型大全" },
      { href: "/tests", label: "测试口径" }
    ]
  },
  {
    title: "决策维度",
    links: [
      { href: "/capabilities", label: "能力榜" },
      { href: "/articles", label: "文章资料" },
      { href: "/self-test", label: "自助测试" }
    ]
  },
  {
    title: "使用原则",
    links: [
      { href: "/rankings/gpt-5.5", label: "先看价格排行" },
      { href: "/tests", label: "再看稳定与风险" },
      { href: "/sites", label: "最后核对企业属性" }
    ]
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
        <span>中转站比价、风险识别与真实测速参考</span>
      </div>
    </footer>
  );
}
