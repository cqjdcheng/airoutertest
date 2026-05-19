export function money(value?: number | null, digits = 4) {
  return typeof value === "number" ? `$${value.toFixed(digits)}` : "-";
}

export function score(value?: number | null, digits = 1) {
  return typeof value === "number" ? value.toFixed(digits) : "-";
}

export function percent(value?: number | null) {
  return typeof value === "number" ? `${value.toFixed(1)}%` : "-";
}

export function formatDateTime(value?: string | null) {
  if (!value) {
    return "暂无快照";
  }

  const date = new Date(value);
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

export function riskLabel(level?: string | null) {
  if (level === "low") return "低风险";
  if (level === "medium") return "中风险";
  if (level === "high") return "高风险";
  if (level === "critical") return "严重风险";
  return "未知";
}

export function riskTone(level?: string | null) {
  if (level === "low") return "success";
  if (level === "medium") return "warning";
  if (level === "high" || level === "critical") return "danger";
  return "neutral";
}
