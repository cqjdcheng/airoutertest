import type { ReactNode } from "react";

type PublicPageHeroProps = {
  eyebrow: string;
  title: string;
  description: string;
  aside?: ReactNode;
  children?: ReactNode;
};

export function PublicPageHero({ eyebrow, title, description, aside, children }: PublicPageHeroProps) {
  return (
    <section className="page-hero">
      <div>
        <p className="eyebrow">{eyebrow}</p>
        <h1 className="page-title mt-4">{title}</h1>
        <p className="body-lead mt-5 max-w-3xl">{description}</p>
        {children ? <div className="mt-6">{children}</div> : null}
      </div>
      {aside ? <aside className="page-hero__aside">{aside}</aside> : null}
    </section>
  );
}
