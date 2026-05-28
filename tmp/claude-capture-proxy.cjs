const fs = require("fs");
const http = require("http");
const https = require("https");

const port = 8787;

const redactHeaders = (headers) => {
  const clone = { ...headers };
  for (const key of Object.keys(clone)) {
    if (/authorization|api-key|token|cookie/i.test(key)) {
      clone[key] = "***";
    }
  }
  return clone;
};

const server = http.createServer((request, response) => {
  const chunks = [];
  request.on("data", (chunk) => chunks.push(chunk));
  request.on("end", () => {
    const body = Buffer.concat(chunks);

    if (request.method === "POST" && request.url.startsWith("/v1/messages")) {
      fs.writeFileSync("tmp/actual-claude-request.json", body);
      fs.writeFileSync(
        "tmp/actual-claude-headers.json",
        JSON.stringify(redactHeaders(request.headers), null, 2),
      );
      console.log("captured", request.method, request.url, body.length);
    } else {
      console.log("proxy", request.method, request.url);
    }

    const upstream = https.request(
      {
        hostname: "socheap.ai",
        port: 443,
        method: request.method,
        path: request.url,
        headers: { ...request.headers, host: "socheap.ai" },
      },
      (upstreamResponse) => {
        console.log("upstream", request.method, request.url, upstreamResponse.statusCode);
        response.writeHead(upstreamResponse.statusCode || 502, upstreamResponse.headers);
        upstreamResponse.pipe(response);
      },
    );

    upstream.on("error", (error) => {
      console.log("upstream_error", error.message);
      response.writeHead(502, { "content-type": "application/json" });
      response.end(JSON.stringify({ error: "proxy_upstream_error" }));
    });

    upstream.write(body);
    upstream.end();
  });
});

server.listen(port, "127.0.0.1", () => {
  console.log(`capture proxy listening on http://127.0.0.1:${port}`);
});
