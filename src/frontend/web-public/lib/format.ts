export function money(value?: number | null, digits = 4) {
  return typeof value === "number" ? `$${value.toFixed(digits)}` : "-";
}

export function score(value?: number | null, digits = 1) {
  return typeof value === "number" ? value.toFixed(digits) : "-";
}

export function trustScore(matchScore?: number | null, riskScore?: number | null) {
  if (typeof matchScore === "number") {
    return Math.max(0, Math.min(100, matchScore));
  }

  if (typeof riskScore === "number") {
    return Math.max(0, Math.min(100, 100 - riskScore));
  }

  return null;
}

export function percent(value?: number | null) {
  return typeof value === "number" ? `${value.toFixed(1)}%` : "-";
}

export function formatDateTime(value?: string | null) {
  if (!value) {
    return "暂无快照";
  }

  const normalizedValue = /(?:z|[+-]\d{2}:?\d{2})$/i.test(value) ? value : `${value}Z`;
  const date = new Date(normalizedValue);
  if (Number.isNaN(date.getTime())) {
    return value;
  }

  const parts = new Intl.DateTimeFormat("en-CA", {
    timeZone: "Asia/Shanghai",
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
    hour: "2-digit",
    minute: "2-digit",
    hour12: false
  }).formatToParts(date);
  const get = (type: string) => parts.find((part) => part.type === type)?.value ?? "00";

  return `${get("year")}/${get("month")}/${get("day")} ${get("hour")}:${get("minute")}`;
}
