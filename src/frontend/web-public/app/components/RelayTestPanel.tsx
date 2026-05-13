"use client";

import { FormEvent, type CSSProperties, useEffect, useMemo, useState } from "react";
import { getJson, postJson, type PublicEnvelope } from "@/lib/api";
import { riskLabel, riskTone } from "@/lib/format";

type ModelOption = {
  label: string;
  slug: string;
  badge?: string;
};

type SelfTestChallenge = {
  id: string;
  question: string;
  expiresAt: string;
};

type SelfTestProbeResult = {
  code: string;
  name: string;
  category: string;
  status: "pass" | "warn" | "fail" | "unknown";
  confidence: "high" | "medium" | "low";
  scoreImpact: number;
  riskImpact: number;
  evidence: string;
};

type SelfTestResponse = {
  id: string;
  status: string;
  firstTokenMs?: number | null;
  fullResponseMs?: number | null;
  riskScore: number;
  riskLevel: string;
  resultSummary: string;
  matchScore: number;
  inputTokens?: number | null;
  outputTokens?: number | null;
  totalTokens?: number | null;
  estimatedTokens: number;
  tokensPerSecond?: number | null;
  checks: SelfTestProbeResult[];
  createdAt: string;
};

const modelOptions: ModelOption[] = [
  { label: "GPT 5.5", slug: "gpt-5.5", badge: "HOT" },
  { label: "GPT 5.4", slug: "gpt-5.4" },
  { label: "Claude Code 4.7", slug: "claude-code-4.7" },
  { label: "Claude Code 4.6", slug: "claude-code-4.6" },
  { label: "Gemini 3.1", slug: "gemini-3.1-pro" }
];

export function RelayTestPanel({
  initialModel = "gpt-5.5",
  compact = false
}: {
  initialModel?: string;
  compact?: boolean;
}) {
  const initialPreset = modelOptions.find((item) => item.slug === initialModel) ?? modelOptions[0];
  const initialCustomModel = modelOptions.some((item) => item.slug === initialModel) ? "" : initialModel;

  const [siteUrl, setSiteUrl] = useState("");
  const [apiKey, setApiKey] = useState("");
  const [selectedModel, setSelectedModel] = useState(initialPreset);
  const [customModel, setCustomModel] = useState(initialCustomModel);
  const [isStream, setIsStream] = useState(true);
  const [challenge, setChallenge] = useState<SelfTestChallenge | null>(null);
  const [challengeAnswer, setChallengeAnswer] = useState("");
  const [result, setResult] = useState<SelfTestResponse | null>(null);
  const [loading, setLoading] = useState(false);
  const [challengeLoading, setChallengeLoading] = useState(false);
  const [error, setError] = useState("");

  const targetModel = customModel.trim() || selectedModel.slug;
  const targetModelLabel = customModel.trim() || selectedModel.label;

  useEffect(() => {
    void loadChallenge();
  }, []);

  async function loadChallenge() {
    setChallengeLoading(true);
    const response = await getJson<PublicEnvelope<SelfTestChallenge>>("/api/v1/public/self-tests/challenge");
    setChallenge(response?.data ?? null);
    setChallengeAnswer("");
    setChallengeLoading(false);
  }

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setLoading(true);
    setError("");
    setResult(null);

    if (!challenge) {
      setError("人机验证加载失败，请刷新挑战题后重试。");
      setLoading(false);
      return;
    }

    const response = await postJson<PublicEnvelope<SelfTestResponse>>("/api/v1/public/self-tests", {
      siteUrl,
      modelName: targetModel,
      apiKey,
      isStream,
      testMode: "comprehensive",
      challengeId: challenge.id,
      challengeAnswer
    });

    if (response?.data) {
      setResult(response.data);
      setError("");
    } else {
      setError("测试请求失败，请检查站点地址、模型名、API Key 或人机验证答案。");
    }

    await loadChallenge();
    setLoading(false);
  }

  const passCount = useMemo(() => result?.checks.filter((item) => item.status === "pass").length ?? 0, [result]);

  return (
    <div className="relay-test-panel">
      <form className="scan-card relay-test-panel__form" onSubmit={submit}>
        <div className="scan-card__header">
          <div>
            <p className="eyebrow">接口配置</p>
            <h2>中转测试</h2>
          </div>
          <span className="secure-badge">Key 不落库</span>
        </div>

        <div className="relay-form-grid mt-5">
          <label>
            <span>API 接口地址</span>
            <input
              className="form-input"
              onChange={(event) => setSiteUrl(event.target.value)}
              placeholder="https://api.example.com/v1 或 /v1/chat/completions"
              required
              type="url"
              value={siteUrl}
            />
          </label>
          <label>
            <span>API Key</span>
            <input
              className="form-input"
              onChange={(event) => setApiKey(event.target.value)}
              placeholder="sk-...，仅用于本次检测"
              required
              type="password"
              value={apiKey}
            />
          </label>
        </div>

        <div className="mt-5">
          <div className="mb-3 text-sm font-semibold text-[var(--text-secondary)]">目标模型</div>
          <div className="model-picker">
            {modelOptions.map((model) => (
              <button
                className="model-chip"
                data-active={!customModel.trim() && model.slug === selectedModel.slug}
                key={model.slug}
                onClick={() => {
                  setSelectedModel(model);
                  setCustomModel("");
                }}
                type="button"
              >
                <strong>{model.label}</strong>
                <span>{model.slug}</span>
                {model.badge ? <em>{model.badge}</em> : null}
              </button>
            ))}
          </div>
        </div>

        <label className="custom-model-field mt-5">
          <span>自定义模型名称</span>
          <input
            className="form-input"
            onChange={(event) => setCustomModel(event.target.value)}
            placeholder="例如 claude-opus-4-7、gpt-5.5 或站点自定义模型名"
            value={customModel}
          />
        </label>

        <div className="relay-test-options mt-5">
          <label className="relay-switch">
            <input checked={isStream} onChange={(event) => setIsStream(event.target.checked)} type="checkbox" />
            <span>开启流式测速，记录首 token 时间</span>
          </label>
          <div className="challenge-box">
            <div>
              <span>人机验证</span>
              <strong>{challengeLoading ? "加载中..." : challenge?.question ?? "挑战题加载失败"}</strong>
            </div>
            <input
              className="form-input"
              inputMode="numeric"
              onChange={(event) => setChallengeAnswer(event.target.value)}
              placeholder="答案"
              required
              value={challengeAnswer}
            />
            <button className="secondary-button" disabled={challengeLoading || loading} onClick={loadChallenge} type="button">
              换一题
            </button>
          </div>
        </div>

        <div className="scan-notice mt-5">
          将真实请求用户填写的 OpenAI 兼容接口。检测覆盖协议、响应结构、知识题、结构化输出、流完整性、Token 与响应时延。
        </div>

        <button className="primary-button mt-5 w-full" disabled={loading || !challenge}>
          {loading ? `正在检测 ${targetModelLabel}...` : `开始检测 ${targetModelLabel}`}
        </button>

        {error ? <div className="relay-error mt-4">{error}</div> : null}
      </form>

      {result ? (
        <section className={`relay-result ${compact ? "relay-result--compact" : ""}`}>
          <div className="relay-result__summary">
            <div className="score-ring" style={{ "--score": `${Math.max(0, Math.min(100, result.matchScore))}%` } as CSSProperties}>
              <strong>{Math.round(result.matchScore)}</strong>
              <span>匹配度</span>
            </div>
            <div>
              <p className="eyebrow">当前测试结果</p>
              <h3>{result.status === "succeeded" ? "检测完成" : "检测存在风险"}</h3>
              <p>{result.resultSummary}</p>
              <div className="relay-result__meta">
                <span>检测站点：{siteUrl}</span>
                <span>目标模型：{targetModel}</span>
                <span>通过探针：{passCount}/{result.checks.length}</span>
              </div>
            </div>
            <span className="status-pill" data-tone={riskTone(result.riskLevel)}>
              {riskLabel(result.riskLevel)} / 风险 {Math.round(result.riskScore)}
            </span>
          </div>

          <div className="probe-grid mt-5">
            {result.checks.map((check) => (
              <article className="probe-card" data-status={check.status} key={check.code}>
                <div>
                  <strong>{check.code}</strong>
                  <span>{check.category}</span>
                </div>
                <h4>{check.name}</h4>
                <p>{check.evidence}</p>
                <small>{statusText(check.status)} · {confidenceText(check.confidence)}</small>
              </article>
            ))}
          </div>

          <div className="relay-metrics mt-5">
            <Metric label="首 token" value={formatMs(result.firstTokenMs)} />
            <Metric label="完整响应" value={formatMs(result.fullResponseMs)} />
            <Metric label="Tokens/s" value={result.tokensPerSecond ? `${result.tokensPerSecond}` : "-"} />
            <Metric label="输入 Token" value={formatNumber(result.inputTokens)} />
            <Metric label="输出 Token" value={formatNumber(result.outputTokens)} />
            <Metric label="总 Token" value={formatNumber(result.totalTokens)} />
          </div>
        </section>
      ) : null}
    </div>
  );
}

function Metric({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <span>{label}</span>
      <strong>{value}</strong>
    </div>
  );
}

function formatMs(value?: number | null) {
  return typeof value === "number" ? `${value}ms` : "-";
}

function formatNumber(value?: number | null) {
  return typeof value === "number" ? value.toLocaleString("zh-CN") : "-";
}

function statusText(status: SelfTestProbeResult["status"]) {
  return {
    pass: "通过",
    warn: "可疑",
    fail: "失败",
    unknown: "未知"
  }[status];
}

function confidenceText(confidence: SelfTestProbeResult["confidence"]) {
  return {
    high: "高置信",
    medium: "中置信",
    low: "低置信"
  }[confidence];
}
