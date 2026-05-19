"use client";

import { FormEvent, type CSSProperties, useEffect, useMemo, useState } from "react";
import { getJson, postJson, type PublicEnvelope } from "@/lib/api";
import { formatDateTime, riskLabel, riskTone } from "@/lib/format";
import {
  readSelfTestHistory,
  saveSelfTestHistoryItem,
  subscribeSelfTestHistory,
  type SelfTestHistoryItem
} from "@/lib/selfTestHistory";

export type ModelOption = {
  label: string;
  slug: string;
  requestName?: string;
  apiType?: "openai" | "anthropic";
  badge?: string;
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
  testRecordId?: number | null;
  siteSlug?: string | null;
  siteName?: string | null;
  modelSlug?: string | null;
  modelName?: string | null;
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

type SelfTestChallenge = {
  id: string;
  question: string;
  expiresAt: string;
};

const fallbackModelOptions: ModelOption[] = [
  { label: "GPT 5.5", slug: "gpt-5.5", requestName: "gpt-5.5", apiType: "openai", badge: "HOT" },
  { label: "GPT 5.4", slug: "gpt-5.4", requestName: "gpt-5.4", apiType: "openai" },
  { label: "Claude Code 4.7", slug: "claude-code-4.7", requestName: "claude-code-4.7", apiType: "anthropic" },
  { label: "Claude Code 4.6", slug: "claude-code-4.6", requestName: "claude-code-4.6", apiType: "anthropic" },
  { label: "Gemini 3.1", slug: "gemini-3.1-pro", requestName: "gemini-3.1-pro", apiType: "openai" }
];

const methodItems = [
  ["协议兼容", "检查 OpenAI Chat Completions 与 SSE 基础结构。"],
  ["流式完整性", "记录首 token、chunk 连续性、结束事件和异常中断。"],
  ["Token 与时延", "记录输入、输出、总 token、完整耗时和吞吐。"],
  ["风险线索", "识别模型自称、上游特征、异常响应和降级痕迹。"]
];

export function RelayTestPanel({
  initialModel,
  compact = false,
  showHistory,
  modelOptions,
  onTestCompleted
}: {
  initialModel?: string;
  compact?: boolean;
  showHistory?: boolean;
  modelOptions?: ModelOption[];
  onTestCompleted?: () => void;
}) {
  const resolvedModelOptions = modelOptions?.length ? modelOptions : fallbackModelOptions;
  const initialModelSlug = initialModel ?? resolvedModelOptions[0]?.slug ?? "gpt-5.5";
  const initialPreset = resolvedModelOptions.find((item) => item.slug === initialModelSlug) ?? resolvedModelOptions[0] ?? fallbackModelOptions[0];
  const initialCustomModel = resolvedModelOptions.some((item) => item.slug === initialModelSlug) ? "" : initialModelSlug;
  const shouldShowHistory = showHistory ?? !compact;

  const [siteUrl, setSiteUrl] = useState("");
  const [apiKey, setApiKey] = useState("");
  const [selectedModel, setSelectedModel] = useState(initialPreset);
  const [customModel, setCustomModel] = useState(initialCustomModel);
  const [isStream, setIsStream] = useState(true);
  const [challenge, setChallenge] = useState<SelfTestChallenge | null>(null);
  const [challengeAnswer, setChallengeAnswer] = useState("");
  const [result, setResult] = useState<SelfTestResponse | null>(null);
  const [historyItems, setHistoryItems] = useState<SelfTestHistoryItem[]>([]);
  const [loading, setLoading] = useState(false);
  const [verificationOpen, setVerificationOpen] = useState(false);
  const [methodsOpen, setMethodsOpen] = useState(false);
  const [challengeLoading, setChallengeLoading] = useState(false);
  const [challengeError, setChallengeError] = useState("");
  const [error, setError] = useState("");

  const targetModel = customModel.trim() || selectedModel.requestName || selectedModel.slug;
  const targetModelLabel = customModel.trim() || selectedModel.label;

  useEffect(() => {
    setHistoryItems(readSelfTestHistory());

    return subscribeSelfTestHistory(() => {
      setHistoryItems(readSelfTestHistory());
    });
  }, []);

  useEffect(() => {
    if (!verificationOpen) {
      return;
    }

    let cancelled = false;
    setChallenge(null);
    setChallengeAnswer("");
    setChallengeError("");
    setChallengeLoading(true);

    async function loadChallenge() {
      const response = await getJson<PublicEnvelope<SelfTestChallenge>>("/api/v1/public/self-tests/challenge");
      if (cancelled) {
        return;
      }

      if (response?.data) {
        setChallenge(response.data);
      } else {
        setChallengeError("安全校验加载失败，请稍后重试。");
      }
      setChallengeLoading(false);
    }

    loadChallenge();

    return () => {
      cancelled = true;
    };
  }, [verificationOpen]);

  function openVerificationModal(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError("");
    setVerificationOpen(true);
  }

  async function runTest() {
    const verifiedAnswer = challengeAnswer.trim();

    if (!challenge || !verifiedAnswer) {
      setChallengeError("请先完成安全校验。");
      return;
    }

    setVerificationOpen(false);
    setChallengeError("");
    setLoading(true);
    setError("");

    const response = await postJson<PublicEnvelope<SelfTestResponse>>("/api/v1/public/self-tests", {
      siteUrl,
      modelName: targetModel,
      apiKey,
      isStream,
      testMode: "comprehensive",
      challengeId: challenge.id,
      challengeAnswer: verifiedAnswer
    });

    if (response?.data) {
      setResult(response.data);
      saveSelfTestHistoryItem({
        id: response.data.id,
        testRecordId: response.data.testRecordId,
        siteUrl,
        siteName: response.data.siteName,
        siteSlug: response.data.siteSlug,
        modelName: response.data.modelName ?? targetModel,
        modelSlug: response.data.modelSlug,
        status: response.data.status,
        riskScore: response.data.riskScore,
        riskLevel: response.data.riskLevel,
        resultSummary: response.data.resultSummary,
        firstTokenMs: response.data.firstTokenMs,
        fullResponseMs: response.data.fullResponseMs,
        testedAt: response.data.createdAt
      });
      onTestCompleted?.();
    } else {
      setError("测试请求失败，请检查站点地址、模型名、API Key 或安全校验状态。");
    }

    setChallengeAnswer("");
    setLoading(false);
  }

  const passCount = useMemo(() => result?.checks.filter((item) => item.status === "pass").length ?? 0, [result]);

  return (
    <div className="relay-test-panel">
      <form className="scan-card relay-test-panel__form" onSubmit={openVerificationModal}>
        <div className="scan-card__header">
          <div>
            <p className="eyebrow">接口测试</p>
            <h2>直接测试中转接口</h2>
          </div>
          <div className="relay-panel-actions">
            <button className="help-icon-button" aria-label="查看检测手段" onClick={() => setMethodsOpen(true)} type="button">
              ?
            </button>
            <span className="secure-badge">Key 不落库</span>
          </div>
        </div>

        <div className="relay-form-grid mt-5">
          <label>
            <span>API 接口地址</span>
            <input
              className="form-input"
              onChange={(event) => setSiteUrl(event.target.value)}
              placeholder="https://api.example.com"
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
              placeholder="sk-..."
              required
              type="password"
              value={apiKey}
            />
          </label>
        </div>

        <div className="mt-5">
          <div className="mb-3 text-sm font-semibold text-[var(--text-secondary)]">目标模型</div>
          <div className="model-picker">
            {resolvedModelOptions.map((model) => (
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
                {/* <span>{model.slug}</span> */}
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
            <span>流式测试</span>
          </label>
        </div>

        <button className="primary-button mt-5 w-full" disabled={loading}>
          {loading ? `正在检测 ${targetModelLabel}...` : `开始检测 ${targetModelLabel}`}
        </button>

        {error ? <div className="relay-error mt-4">{error}</div> : null}
      </form>

      {verificationOpen ? (
        <div className="challenge-modal" role="dialog" aria-modal="true" aria-labelledby="cloudflare-check-title">
          <div className="challenge-modal__backdrop" onClick={() => (!loading ? setVerificationOpen(false) : null)} />
          <div className="challenge-modal__panel">
            <div className="scan-card__header">
              <div>
                <p className="eyebrow">Cloudflare</p>
                <h2 id="cloudflare-check-title">安全校验</h2>
              </div>
              <button className="challenge-modal__close" disabled={loading} onClick={() => setVerificationOpen(false)} type="button">
                关闭
              </button>
            </div>

            <div className="turnstile-shell mt-5">
              {challengeLoading ? <span>加载中...</span> : null}
              {challenge ? (
                <label className="custom-model-field">
                  <span>{challenge.question}</span>
                  <input
                    className="form-input"
                    inputMode="numeric"
                    onChange={(event) => setChallengeAnswer(event.target.value)}
                    placeholder="请输入答案"
                    value={challengeAnswer}
                  />
                </label>
              ) : null}
              {challengeError ? <span className="relay-error">{challengeError}</span> : null}
            </div>

            <div className="challenge-modal__actions">
              <button className="secondary-button" disabled={loading} onClick={() => setVerificationOpen(false)} type="button">
                取消
              </button>
              <button className="primary-button" disabled={loading || challengeLoading || !challenge || !challengeAnswer.trim()} onClick={runTest} type="button">
                {loading ? "检测中..." : "继续测试"}
              </button>
            </div>
          </div>
        </div>
      ) : null}

      {methodsOpen ? (
        <div className="challenge-modal" role="dialog" aria-modal="true" aria-labelledby="test-methods-title">
          <div className="challenge-modal__backdrop" onClick={() => setMethodsOpen(false)} />
          <div className="challenge-modal__panel method-modal">
            <div className="scan-card__header">
              <div>
                <p className="eyebrow">检测口径</p>
                <h2 id="test-methods-title">我们会看什么</h2>
              </div>
              <button className="challenge-modal__close" onClick={() => setMethodsOpen(false)} type="button">
                关闭
              </button>
            </div>
            <div className="method-list">
              {methodItems.map(([title, text]) => (
                <article key={title}>
                  <strong>{title}</strong>
                  <span>{text}</span>
                </article>
              ))}
            </div>
          </div>
        </div>
      ) : null}

      {result ? (
        <section className={`relay-result ${compact ? "relay-result--compact" : ""}`}>
          <div className="relay-result__summary">
            <div className="score-ring" style={{ "--score": `${Math.max(0, Math.min(100, result.matchScore))}%` } as CSSProperties}>
              <strong>{Math.round(result.matchScore)}</strong>
              <span>匹配度</span>
            </div>
            <div>
              <p className="eyebrow">测试结果</p>
              <h3>{result.status === "succeeded" ? "检测完成" : "存在风险"}</h3>
              <p>{result.resultSummary}</p>
              <div className="relay-result__meta">
                <span>{siteUrl}</span>
                <span>{targetModel}</span>
                <span>{passCount}/{result.checks.length} 项通过</span>
              </div>
            </div>
            <span className="status-pill" data-tone={riskTone(result.riskLevel)}>
              {riskLabel(result.riskLevel)} / {Math.round(result.riskScore)}
            </span>
          </div>

          <div className="relay-metrics mt-5">
            <Metric label="首 token" value={formatMs(result.firstTokenMs)} />
            <Metric label="完整响应" value={formatMs(result.fullResponseMs)} />
            <Metric label="Tokens/s" value={result.tokensPerSecond ? `${result.tokensPerSecond}` : "-"} />
            <Metric label="输入 Token" value={formatNumber(result.inputTokens)} />
            <Metric label="输出 Token" value={formatNumber(result.outputTokens)} />
            <Metric label="总 Token" value={formatNumber(result.totalTokens)} />
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
                <small>{statusText(check.status)} / {confidenceText(check.confidence)}</small>
              </article>
            ))}
          </div>
        </section>
      ) : null}

      {shouldShowHistory ? (
        <section className="history-card">
          <div className="history-card__header">
            <div>
              <p className="eyebrow">Local Cache</p>
              <h2>我的最近测试</h2>
            </div>
            <span className="status-pill" data-tone="neutral">
              当前浏览器
            </span>
          </div>

          {historyItems.length ? (
            <div className="history-list">
              {historyItems.slice(0, 5).map((item) => (
                <article className="history-row" key={item.id}>
                  <div className="history-row__identity">
                    <strong>{item.modelName}</strong>
                    <span>{item.siteUrl}</span>
                    <small>{formatDateTime(item.testedAt)}</small>
                  </div>
                  <div className="history-row__signals">
                    <span className="status-pill" data-tone={item.status === "succeeded" ? "success" : "danger"}>
                      {item.status}
                    </span>
                    <span className="status-pill" data-tone={riskTone(item.riskLevel)}>
                      {riskLabel(item.riskLevel)}
                    </span>
                  </div>
                </article>
              ))}
            </div>
          ) : (
            <div className="empty-state">自测结果会按最新优先保存在本地。</div>
          )}
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
