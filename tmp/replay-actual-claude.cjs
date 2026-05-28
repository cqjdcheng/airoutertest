const fs = require("fs");

const token = process.env.ANTHROPIC_AUTH_TOKEN;
if (!token) {
  console.error("ANTHROPIC_AUTH_TOKEN is required");
  process.exit(1);
}

const request = JSON.parse(fs.readFileSync("tmp/actual-claude-request.json", "utf8"));
if (process.env.CLAUDE_REPLAY_VARIANT === "prompt-only") {
  request.messages[0].content = request.messages[0].content.slice(-1);
}
if (process.env.CLAUDE_REPLAY_VARIANT === "short-system") {
  request.messages[0].content = request.messages[0].content.slice(-1);
  request.system = request.system.slice(0, 1);
}
if (process.env.CLAUDE_REPLAY_VARIANT === "short-tools") {
  request.messages[0].content = request.messages[0].content.slice(-1);
  request.system = request.system.slice(0, 1);
  request.tools = request.tools.map((tool) => ({
    name: tool.name,
    description: tool.description?.slice(0, 80) || `${tool.name} tool`,
    input_schema: { type: "object", properties: {}, required: [] },
  }));
}
const body = JSON.stringify(request);
const headers = JSON.parse(fs.readFileSync("tmp/actual-claude-headers.json", "utf8"));
delete headers.host;
delete headers.connection;
delete headers["accept-encoding"];
delete headers["content-length"];
headers.authorization = `Bearer ${token}`;

const controller = new AbortController();
const started = Date.now();
const timeout = setTimeout(() => controller.abort(), 60000);
const now = () => Date.now() - started;

(async () => {
  try {
    const response = await fetch("https://socheap.ai/v1/messages?beta=true", {
      method: "POST",
      headers,
      body,
      signal: controller.signal,
    });
    console.log("status", response.status, "headersMs", now(), "contentType", response.headers.get("content-type"));
    const reader = response.body?.getReader();
    if (!reader) return;
    const decoder = new TextDecoder();
    let text = "";
    let chunks = 0;
    while (true) {
      const { value, done } = await reader.read();
      if (done) {
        console.log("done", chunks, now());
        break;
      }
      chunks += 1;
      const part = decoder.decode(value, { stream: true });
      text += part;
      console.log("chunk", chunks, now(), part.slice(0, 400).replace(/\s+/g, " "));
      if (text.includes("message_stop") || text.includes("[DONE]")) {
        console.log("saw_stop", chunks, now());
        break;
      }
    }
  } catch (error) {
    console.log("error", error.name, error.message, now());
  } finally {
    clearTimeout(timeout);
  }
})();
