"use client";

import type { AnchorHTMLAttributes, MouseEvent, ReactNode } from "react";

type TrackedOutboundLinkProps = Omit<AnchorHTMLAttributes<HTMLAnchorElement>, "href"> & {
  href: string;
  siteSlug: string;
  targetType: "website" | "invite" | "docs";
  children: ReactNode;
};

export function TrackedOutboundLink({
  href,
  siteSlug,
  targetType,
  children,
  onClick,
  ...props
}: TrackedOutboundLinkProps) {
  function handleClick(event: MouseEvent<HTMLAnchorElement>) {
    onClick?.(event);
    if (event.defaultPrevented) {
      return;
    }

    const payload = JSON.stringify({
      targetType,
      sourcePath: window.location.pathname
    });
    const endpoint = `/api/v1/public/sites/${encodeURIComponent(siteSlug)}/outbound-clicks`;

    if (navigator.sendBeacon && navigator.sendBeacon(endpoint, new Blob([payload], { type: "application/json" }))) {
      return;
    }

    void fetch(endpoint, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: payload,
      keepalive: true
    });
  }

  return (
    <a href={href} onClick={handleClick} {...props}>
      {children}
    </a>
  );
}
