# CheapAI Loop Tool

`loop.ps1` is the local verification runner for CheapAI development.

It performs the same checks after each development stage:

1. Stop CheapAI-related local preview processes.
2. Run backend build and tests.
3. Build the public Next.js app with normalized `D:/...` path casing.
4. Build the admin Umi app.
5. Start API, public web, and admin web previews.
6. Run smoke checks against core API endpoints.
7. Capture desktop and mobile screenshots with Playwright.
8. Write a report to `artifacts/loop/<timestamp>/report.md`.

Install tool dependencies first:

```powershell
pnpm install --dir "D:/project/CheapAi/tools"
```

Run from the repository root:

```powershell
pnpm --dir "D:/project/CheapAi/tools" exec powershell -ExecutionPolicy Bypass -File loop/loop.ps1 -Stage smoke
```

Available stages:

- `smoke`: validate the current runnable P0 chain.
- `p0`: P0 verification gate.
- `p1`: P1 verification gate.
- `p2`: P2 verification gate.
- `all`: full verification gate.

The script only stops processes whose command line or executable path points under `D:/project/CheapAi`.
