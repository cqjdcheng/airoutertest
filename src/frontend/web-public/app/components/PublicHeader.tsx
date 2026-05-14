"use client";

import Link from "next/link";
import { usePublicBranding } from "@/app/components/PublicBrandProvider";

type PublicHeaderProps = {
  featuredModel?: string;
};

export function PublicHeader({ featuredModel = "gpt-5.5" }: PublicHeaderProps) {
  const branding = usePublicBranding();
  const links = [
    { href: "/sites", label: "中转站大全" },
    { href: "/models", label: "模型大全" },
    { href: "/tests", label: "中转测试" },
    { href: `/rankings/${featuredModel}`, label: "价格排行" },
    { href: "/capabilities", label: "能力榜" },
    { href: "/articles", label: "文章" }
  ];
  return (
    <header className="site-header">
      <div className="public-container site-header__inner">
        <Link href="/" className="site-brand">
          <span className="site-brand__mark" aria-hidden="true">
            {branding.siteIconUrl ? <img src={branding.siteIconUrl} alt="" className="site-brand__image" /> : branding.siteName.slice(0, 1)}
          </span>
          <span className="site-brand__content">
            <strong>{branding.siteName}</strong>
            <span>AI 中转比价与风险识别</span>
          </span>
        </Link>

        <nav className="site-nav" aria-label="主导航">
          {links.map((link) => (
            <Link key={link.href} href={link.href} className="site-nav__link">
              {link.label}
            </Link>
          ))}
        </nav>

        <div className="site-header__actions">
          <Link href="/self-test" className="primary-button">
            立即自测
          </Link>
        </div>
      </div>
    </header>
  );
}

export function BackLink({ href = "/", label = "返回首页" }: { href?: string; label?: string }) {
  return (
    <Link href={href} className="inline-flex items-center gap-2 text-sm font-medium text-[var(--text-secondary)] transition hover:text-[var(--brand)]">
      <span aria-hidden="true">←</span>
      {label}
    </Link>
  );
}
