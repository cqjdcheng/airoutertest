import { score } from "@/lib/format";

type SelfTestProbeLabelInput = {
  status?: string | null;
  confidence?: string | null;
  riskImpact?: number | null;
};

export function selfTestDisplayIndex(index: number) {
  return String(index + 1).padStart(2, "0");
}

export function selfTestStatusLabel(status?: string | null) {
  if (status === "pass") return "正常";
  if (status === "warn") return "需关注";
  if (status === "fail") return "请求失败";
  return "未检测";
}

export function selfTestConfidenceLabel(confidence?: string | null) {
  if (confidence === "high") return "依据充分";
  if (confidence === "medium") return "一般参考";
  if (confidence === "low") return "辅助参考";
  return "参考信息";
}

export function selfTestDeductionLabel(check: SelfTestProbeLabelInput) {
  if (check.status === "pass") {
    return "不扣分";
  }

  if (typeof check.riskImpact === "number" && check.riskImpact > 0) {
    return `扣 ${score(check.riskImpact, 0)} 分`;
  }

  return "仅参考";
}
