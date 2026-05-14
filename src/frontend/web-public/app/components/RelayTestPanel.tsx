"use client";

import { FormEvent, type CSSProperties, useEffect, useMemo, useRef, useState } from "react";
import { postJson, type PublicEnvelope } from "@/lib/api";
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

type TurnstileApi = {
  render: (
    container: HTMLElement,
    options: {
      sitekey: string;
      theme?: "light" | "dark" | "auto";
      callback?: (token: string) => void;
      "error-callback"?: () => void;
      "expired-callback"?: () => void;
    }
  ) => string;
  remove?: (widgetId: string) => void;
  reset?: (widgetId: string) => void;
};

declare global {
  interface Window {
    turnstile?: TurnstileApi;
  }
}

const turnstileSiteKey = process.env.NEXT_PUBLIC_TURNSTILE_SITE_KEY ?? "1x00000000000000000000AA";

const fallbackModelOptions: ModelOption[] = [
  { label: "GPT 5.5", slug: "gpt-5.5", badge: "HOT" },
  { label: "GPT 5.4", slug: "gpt-5.4" },
  { label: "Claude Code 4.7", slug: "claude-code-4.7" },
  { label: "Claude Code 4.6", slug: "claude-code-4.6" },
  { label: "Gemini 3.1", slug: "gemini-3.1-pro" }
];

const methodItems = [
  ["协议兼容", "检查 OpenAI Chat Completions 与 SSE 基础结构。"],
  ["流式完整性", "记录首 token、chunk 连续性、结束事件和异常中断。"],
  ["Token 与时延", "记录输入、输出、总 token、完整耗时和吞吐。"],
  ["风险线索", "识别模型自称、上游特征、异常响应和降级迹象。"]
];

export function RelayTestPanel({
  initialModel,
  compact = false,
  showHistory,
  modelOptions
}: {
  initialModel?: string;
  compact?: boolean;
  showHistory?: boolean;
  modelOptions?: ModelOption[];
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
  const [turnstileToken, setTurnstileToken] = useState("");
  const [result, setResult] = useState<SelfTestResponse | null>(null);
  const [historyItems, setHistoryItems] = useState<SelfTestHistoryItem[]>([]);
  const [loading, setLoading] = useState(false);
  const [verificationOpen, setVerificationOpen] = useState(false);
  const [methodsOpen, setMethodsOpen] = useState(false);
  const [turnstileReady, setTurnstileReady] = useState(false);
  const [turnstileError, setTurnstileError] = useState("");
  const [error, setError] = useState("");
  const turnstileContainerRef = useRef<HTMLDivElement>(null);
  const turnstileWidgetIdRef = useRef<string | null>(null);

  const targetModel = customModel.trim() || selectedModel.label;
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
    setTurnstileToken("");
    setTurnstileReady(false);
    setTurnstileError("");

    const renderTurnstile = () => {
      if (cancelled || !turnstileContainerRef.current || !window.turnstile) {
        return;
      }

      if (turnstileWidgetIdRef.current && window.turnstile.remove) {
        window.turnstile.remove(turnstileWidgetIdRef.current);
      }

      turnstileWidgetIdRef.current = window.turnstile.render(turnstileContainerRef.current, {
        sitekey: turnstileSiteKey,
        theme: "light",
        callback: (token) => {
          setTurnstileToken(token);
          setTurnstileError("");
        },
        "error-callback": () => {
          setTurnstileToken("");
          setTurnstileError("安全校验未完成，请重试。");
        },
        "expired-callback": () => {
          setTurnstileToken("");
        }
      });
      setTurnstileReady(true);
    };

    if (window.turnstile) {
      renderTurnstile();
    } else {
      const existingScript = document.getElementById("cloudflare-turnstile-script") as HTMLScriptElement | null;
      const script = existingScript ?? document.createElement("script");
      script.id = "cloudflare-turnstile-script";
      script.src = "https://challenges.cloudflare.com/turnstile/v0/api.js?render=explicit";
      script.async = true;
      script.defer = true;
      script.onload = renderTurnstile;
      script.onerror = () => {
        if (!cancelled) {
          setTurnstileError("安全组件加载失败，请稍后重试。");
        }
      };

      if (!existingScript) {
        document.head.appendChild(script);
      }
    }

    return () => {
      cancelled = true;
      if (turnstileWidgetIdRef.current && window.turnstile?.remove) {
        window.turnstile.remove(turnstileWidgetIdRef.current);
      }
      turnstileWidgetIdRef.current = null;
    };
  }, [verificationOpen]);

  function openVerificationModal(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError("");
    setVerificationOpen(true);
  }

  async function runTest() {
    const verifiedToken = turnstileToken;

    if (!verifiedToken) {
      setTurnstileError("请先完成安全校验。");
      return;
    }

    setVerificationOpen(false);
    setTurnstileError("");
    setLoading(true);
    setError("");

    const response = await postJson<PublicEnvelope<SelfTestResponse>>("/api/v1/public/self-tests", {
      siteUrl,
      modelName: targetModel,
      apiKey,
      isStream,
      testMode: "comprehensive",
      challengeId: "cloudflare-turnstile",
      challengeAnswer: verifiedToken
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
    } else {
      setError("测试请求失败，请检查站点地址、模型名、API Key 或安全校验状态。");
    }

    setTurnstileToken("");
    setLoading(false);
  }

  const passCount = useMemo(() => result?.checks.filter((item) => item.status === "pass").length ?? 0, [result]);

  return (
    <div className="relay-test-panel">
      <form className="scan-card relay-test-panel__form" onSubmit={openVerificationModal}>
        <div className="scan-card__header">
          <div>
            <p className="eyebrow">接口测试</p>
            <h2>填写接口信息</h2>
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
              placeholder="https://api.example.com/v1"
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
            <span>流式测速</span>
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
              <div ref={turnstileContainerRef} className="turnstile-container" />
              {!turnstileReady && !turnstileError && !turnstileToken ? <span>加载中...</span> : null}
              {turnstileToken ? <span className="turnstile-status">安全校验已完成</span> : null}
              {turnstileError ? <span className="relay-error">{turnstileError}</span> : null}
            </div>

            <div className="challenge-modal__actions">
              <button className="secondary-button" disabled={loading} onClick={() => setVerificationOpen(false)} type="button">
                取消
              </button>
              <button className="primary-button" disabled={loading || !turnstileToken} onClick={runTest} type="button">
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
