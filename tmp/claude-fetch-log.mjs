const originalFetch = globalThis.fetch;

globalThis.fetch = async (input, init = {}) => {
  const url = typeof input === "string" ? input : input?.url;
  if (url && String(url).includes("socheap.ai")) {
    const headers = new Headers(init.headers ?? input?.headers ?? {});
    for (const name of [...headers.keys()]) {
      if (["authorization", "x-api-key"].includes(name.toLowerCase())) {
        headers.set(name, "<redacted>");
      }
    }

    let body = init.body;
    if (body && typeof body !== "string") {
      try {
        body = await new Response(body).text();
      } catch {
        body = "<non-string-body>";
      }
    }

    console.error("CLAUDE_FETCH_URL", url);
    console.error("CLAUDE_FETCH_HEADERS", JSON.stringify(Object.fromEntries(headers.entries()), null, 2));
    console.error("CLAUDE_FETCH_BODY", body);
    throw new Error("stop after logging Claude Code request");
  }

  return originalFetch(input, init);
};
