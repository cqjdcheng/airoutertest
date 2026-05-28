const https = require("https");
const http = require("http");

function patch(mod, protocol) {
  const originalRequest = mod.request;
  mod.request = function patchedRequest(options, callback) {
    const target =
      typeof options === "string"
        ? options
        : `${protocol}://${options.hostname || options.host || ""}${options.path || ""}`;

    if (!String(target).includes("socheap.ai")) {
      return originalRequest.call(this, options, callback);
    }

    const req = originalRequest.call(this, options, callback);
    const chunks = [];
    const originalWrite = req.write.bind(req);
    const originalEnd = req.end.bind(req);

    req.write = (chunk, ...args) => {
      if (chunk) chunks.push(Buffer.isBuffer(chunk) ? chunk : Buffer.from(String(chunk)));
      return originalWrite(chunk, ...args);
    };

    req.end = (chunk, ...args) => {
      if (chunk) chunks.push(Buffer.isBuffer(chunk) ? chunk : Buffer.from(String(chunk)));
      const headers = { ...(options.headers || {}) };
      for (const key of Object.keys(headers)) {
        if (["authorization", "x-api-key"].includes(key.toLowerCase())) {
          headers[key] = "<redacted>";
        }
      }

      console.error("CLAUDE_HTTP_URL", target);
      console.error("CLAUDE_HTTP_HEADERS", JSON.stringify(headers, null, 2));
      console.error("CLAUDE_HTTP_BODY", Buffer.concat(chunks).toString("utf8"));
      process.exit(66);
      return originalEnd(chunk, ...args);
    };

    return req;
  };
}

patch(https, "https");
patch(http, "http");
