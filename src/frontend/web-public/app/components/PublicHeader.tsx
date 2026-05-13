import Link from "next/link";

type PublicHeaderProps = {
  featuredModel?: string;
};

export function PublicHeader({ featuredModel = "gpt-5.5" }: PublicHeaderProps) {
  const links = [
    { href: "/sites", label: "中转站大全" },
    { href: "/models", label: "模型大全" },
    { href: "/tests", label: "中转测试" },
    { href: `/rankings/${featuredModel}`, label: "价格排行" },
    { href: "/capabilities", label: "能力榜" },
    { href: "/articles", label: "文章" }
  ];

  return (
    <header className="sticky top-0 z-40 border-b border-[var(--line)] bg-white/78 backdrop-blur-2xl">
      <div className="public-container flex min-h-16 flex-col items-start justify-between gap-3 py-3 sm:flex-row sm:items-center sm:gap-4">
        <Link href="/" className="flex items-center gap-2 text-lg font-semibold tracking-tight text-[var(--text-primary)]">
          <span className="inline-flex size-8 items-center justify-center rounded-2xl bg-[var(--text-primary)] text-sm text-white">
            C
          </span>
          CheapAI
        </Link>
        <nav className="flex w-full items-center gap-1 overflow-x-auto whitespace-nowrap text-sm text-[var(--text-secondary)] sm:w-auto sm:justify-end">
          {links.map((link) => (
            <Link key={link.href} href={link.href} className="rounded-full px-3 py-2 transition hover:bg-[var(--surface-muted)] hover:text-[var(--text-primary)]">
              {link.label}
            </Link>
          ))}
        </nav>
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
