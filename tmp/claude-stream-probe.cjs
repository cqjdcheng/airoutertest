const token = process.env.ANTHROPIC_AUTH_TOKEN;
if (!token) {
  console.error("ANTHROPIC_AUTH_TOKEN is required");
  process.exit(1);
}

const controller = new AbortController();
const started = Date.now();
const timeout = setTimeout(() => controller.abort(), 60000);
const now = () => Date.now() - started;
const crypto = require("crypto");
const prompt = "Reply with exactly OK.";
const version = process.env.CLAUDE_PROBE_VERSION || "2.1.150";
const salt = "59cf53e54c78";
const sample = [4, 7, 20].map((index) => prompt[index] || "0").join("");
const versionSuffix = crypto.createHash("sha256").update(`${salt}${sample}${version}`).digest("hex").slice(0, 3);
const contentHash = crypto.createHash("sha256").update(prompt).digest("hex").slice(0, 5);
const useNativeCch = process.env.CLAUDE_PROBE_CCH === "xxh";
const billingHeader = `x-anthropic-billing-header: cc_version=${version}.${versionSuffix}; cc_entrypoint=cli; cch=${useNativeCch ? "00000" : contentHash};`;

const mask64 = (value) => BigInt.asUintN(64, value);
const read64 = (buffer, offset) => {
  let value = 0n;
  for (let index = 0; index < 8; index += 1) {
    value |= BigInt(buffer[offset + index]) << BigInt(index * 8);
  }
  return value;
};
const read32 = (buffer, offset) => {
  let value = 0;
  for (let index = 0; index < 4; index += 1) {
    value |= buffer[offset + index] << (index * 8);
  }
  return value >>> 0;
};
const prime1 = 0x9e3779b185ebca87n;
const prime2 = 0xc2b2ae3d27d4eb4fn;
const prime3 = 0x165667b19e3779f9n;
const prime4 = 0x85ebca77c2b2ae63n;
const prime5 = 0x27d4eb2f165667c5n;
const rotl = (value, bits) => mask64((value << BigInt(bits)) | (value >> BigInt(64 - bits)));
const round = (acc, input) => mask64(rotl(mask64(acc + mask64(input * prime2)), 31) * prime1);
const mergeRound = (acc, value) => mask64((mask64(acc ^ round(0n, value)) * prime1) + prime4);
const avalanche = (value) => {
  let hash = value;
  hash = mask64((hash ^ (hash >> 33n)) * prime2);
  hash = mask64((hash ^ (hash >> 29n)) * prime3);
  return mask64(hash ^ (hash >> 32n));
};
const xxhash64 = (input, seed) => {
  const buffer = Buffer.from(input, "utf8");
  let offset = 0;
  let hash;

  if (buffer.length >= 32) {
    const limit = buffer.length - 32;
    let v1 = mask64(seed + prime1 + prime2);
    let v2 = mask64(seed + prime2);
    let v3 = mask64(seed);
    let v4 = mask64(seed - prime1);

    while (offset <= limit) {
      v1 = round(v1, read64(buffer, offset));
      offset += 8;
      v2 = round(v2, read64(buffer, offset));
      offset += 8;
      v3 = round(v3, read64(buffer, offset));
      offset += 8;
      v4 = round(v4, read64(buffer, offset));
      offset += 8;
    }

    hash = mask64(rotl(v1, 1) + rotl(v2, 7) + rotl(v3, 12) + rotl(v4, 18));
    hash = mergeRound(hash, v1);
    hash = mergeRound(hash, v2);
    hash = mergeRound(hash, v3);
    hash = mergeRound(hash, v4);
  } else {
    hash = mask64(seed + prime5);
  }

  hash = mask64(hash + BigInt(buffer.length));

  while (offset + 8 <= buffer.length) {
    const k1 = round(0n, read64(buffer, offset));
    hash = mask64(rotl(mask64(hash ^ k1), 27) * prime1 + prime4);
    offset += 8;
  }

  if (offset + 4 <= buffer.length) {
    hash = mask64(rotl(mask64(hash ^ (BigInt(read32(buffer, offset)) * prime1)), 23) * prime2 + prime3);
    offset += 4;
  }

  while (offset < buffer.length) {
    hash = mask64(rotl(mask64(hash ^ (BigInt(buffer[offset]) * prime5)), 11) * prime1);
    offset += 1;
  }

  return avalanche(hash);
};

const tool = (name, props, required) => ({
  name,
  description: `${name} tool`,
  input_schema: {
    type: "object",
    properties: props,
    required,
    additionalProperties: false,
  },
});

const systemBlocks = [
  ...(process.env.CLAUDE_PROBE_BILLING === "1"
    ? [
        {
          type: "text",
          text: billingHeader,
        },
      ]
    : []),
  {
    type: "text",
    text: process.env.CLAUDE_PROBE_AGENT_SYSTEM === "1"
      ? "You are a Claude agent, built on Anthropic's Claude Agent SDK."
      : "You are Claude Code, Anthropic's official CLI for Claude. You are an interactive CLI tool that helps users with software engineering tasks. For this compatibility probe, do not use tools. Reply with exactly OK.",
    ...(process.env.CLAUDE_PROBE_CACHE === "1" ? { cache_control: { type: "ephemeral" } } : {}),
  },
];

const payload = {
  model: "claude-opus-4-7",
  system: process.env.CLAUDE_PROBE_SYSTEM_STRING === "1"
    ? systemBlocks.map((block) => block.text).join("\n\n")
    : systemBlocks,
  messages: [
    {
      role: "user",
      content: [
        ...(process.env.CLAUDE_PROBE_REMINDERS === "1"
          ? [
              {
                type: "text",
                text: "<system-reminder>\nThe following skills are available for use with the Skill tool:\n\n- claude-api: Build, debug, and optimize Claude API / Anthropic SDK apps.\n</system-reminder>\n",
              },
              {
                type: "text",
                text: "<system-reminder>\nAs you answer the user's questions, you can use the following context:\n# currentDate\nToday's date is 2026/05/26.\n</system-reminder>\n\n",
              },
            ]
          : []),
        { type: "text", text: prompt, ...(process.env.CLAUDE_PROBE_CACHE === "1" ? { cache_control: { type: "ephemeral" } } : {}) },
      ],
    },
  ],
  tools: (process.env.CLAUDE_PROBE_LATEST === "1"
    ? [
        "Agent",
        "AskUserQuestion",
        "Bash",
        "CronCreate",
        "CronDelete",
        "CronList",
        "Edit",
        "EnterPlanMode",
        "EnterWorktree",
        "ExitPlanMode",
        "ExitWorktree",
        "Glob",
        "Grep",
        "NotebookEdit",
        "Read",
        "ScheduleWakeup",
        "Skill",
        "TaskCreate",
        "TaskGet",
        "TaskList",
        "TaskOutput",
        "TaskStop",
        "TaskUpdate",
        "WebFetch",
        "WebSearch",
        "Write",
      ].map((name) => tool(name, { input: { type: "string" } }, []))
    : [
        tool("Bash", { command: { type: "string" }, description: { type: "string" } }, ["command"]),
        tool("Read", { file_path: { type: "string" }, offset: { type: "number" }, limit: { type: "number" } }, ["file_path"]),
        tool("Grep", { pattern: { type: "string" }, path: { type: "string" } }, ["pattern"]),
        tool("Glob", { pattern: { type: "string" }, path: { type: "string" } }, ["pattern"]),
        tool("Write", { file_path: { type: "string" }, content: { type: "string" } }, ["file_path", "content"]),
      ]),
  max_tokens: process.env.CLAUDE_PROBE_LATEST === "1" ? 64000 : process.env.CLAUDE_PROBE_THINKING === "1" ? 2048 : 64,
  ...(process.env.CLAUDE_PROBE_THINKING === "1"
    ? { thinking: { type: "enabled", budget_tokens: 1024 } }
    : {}),
  ...(process.env.CLAUDE_PROBE_LATEST === "1" ? {
    thinking: { type: "adaptive" },
    context_management: { edits: [{ type: "clear_thinking_20251015", keep: "all" }] },
    output_config: { return_text_over_tool_use: true },
  } : {}),
  stream: true,
  metadata: {
    user_id: `user_cheapai_account_selftest_session_${crypto.randomUUID().replace(/-/g, "")}`,
  },
};

const openAiPayload = {
  model: "claude-opus-4-7",
  messages: [{ role: "user", content: prompt }],
  max_tokens: 64,
  stream: true,
};

const headers = {
  ...(process.env.CLAUDE_PROBE_AUTH === "x-api-key"
    ? { "x-api-key": token }
    : { authorization: `Bearer ${token}` }),
  "content-type": "application/json",
  "anthropic-version": "2023-06-01",
  "anthropic-beta": process.env.CLAUDE_PROBE_BETA || (process.env.CLAUDE_PROBE_LATEST === "1"
    ? "claude-code-20250219,interleaved-thinking-2025-05-14,context-management-2025-06-27,prompt-caching-scope-2026-01-05,effort-2025-11-24"
    : "claude-code-20250219"),
  ...(process.env.CLAUDE_PROBE_LATEST === "1" ? { "x-claude-code-session-id": crypto.randomUUID() } : {}),
  "x-app": "cli",
  "X-Stainless-Lang": "js",
  "X-Stainless-Package-Version": process.env.CLAUDE_PROBE_LATEST === "1" ? "0.94.0" : "0.70.0",
  "X-Stainless-OS": "Windows",
  "X-Stainless-Arch": "x64",
  "X-Stainless-Runtime": "node",
  "X-Stainless-Runtime-Version": process.env.CLAUDE_PROBE_LATEST === "1" ? "v24.3.0" : "v22.21.1",
  "X-Stainless-Retry-Count": "0",
  "X-Stainless-Timeout": "600",
  "anthropic-dangerous-direct-browser-access": "true",
  "User-Agent": process.env.CLAUDE_PROBE_UA || `claude-cli/${version} (external, ${process.env.CLAUDE_PROBE_LATEST === "1" ? "sdk-cli" : "cli"})`,
  accept: "application/json",
};

(async () => {
  try {
    const useOpenAiPayload = process.env.CLAUDE_PROBE_OPENAI === "1";
    let body = JSON.stringify(useOpenAiPayload ? openAiPayload : payload);
    if (useNativeCch) {
      const cch = (xxhash64(body, 0x6e52736ac806831en) & 0xfffffn).toString(16).padStart(5, "0");
      body = body.replace("cch=00000", `cch=${cch}`);
      console.log("computed_cch", cch, "version", `${version}.${versionSuffix}`);
    }

    const response = await fetch(process.env.CLAUDE_PROBE_URL || "https://socheap.ai/v1/messages?beta=true", {
      method: "POST",
      headers,
      body,
      signal: controller.signal,
    });

    console.log("status", response.status, "headersMs", now(), "contentType", response.headers.get("content-type"));
    const reader = response.body?.getReader();
    if (!reader) {
      console.log("no_body");
      return;
    }

    const decoder = new TextDecoder();
    let text = "";
    let chunks = 0;
    while (true) {
      const { value, done } = await reader.read();
      if (done) {
        console.log("done", "chunks", chunks, "ms", now());
        break;
      }

      chunks += 1;
      const part = decoder.decode(value, { stream: true });
      text += part;
      console.log("chunk", chunks, "ms", now(), part.slice(0, 500).replace(/\s+/g, " "));

      if (text.includes("message_stop") || text.includes("[DONE]")) {
        console.log("saw_stop", "chunks", chunks, "ms", now());
        break;
      }

      if (chunks >= 20) {
        console.log("chunk_limit", now());
        break;
      }
    }
  } catch (error) {
    console.log("error", error.name, error.message, "ms", now());
  } finally {
    clearTimeout(timeout);
  }
})();
