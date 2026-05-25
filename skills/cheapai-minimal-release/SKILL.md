---
name: cheapai-minimal-release
description: Minimal-permission CheapAI production release workflow. Use when working in D:\project\CheapAi and the user asks to publish, deploy, release, update production, create a release package, avoid sharing server/root credentials, use a restricted deploy user, or improve the CheapAI release process.
---

# CheapAI Minimal Release

## Core Rules

- Always respond in Simplified Chinese.
- Never ask for or store the production root password by default.
- Prefer zero-server-credential mode: build and verify locally, then produce a release package for the user to upload and apply.
- If automation is required, use a restricted `cheapai-deploy` user, not root.
- Never write secrets to Git, logs, release manifests, screenshots, docs, or final responses.
- Production-changing actions still require explicit user confirmation.

## Local Release Package

Run local validation first. For full P2 acceptance, use:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\loop\loop.ps1
```

Build release images and package tar files:

```powershell
powershell -ExecutionPolicy Bypass -File .\deploy\production\release\prepare-release.ps1
```

The package is created under `artifacts/release/<tag>` and contains:

- `manifest.json`
- `SHA256SUMS`
- `realllmcn-api_<tag>.tar`
- `realllmcn-jobs_<tag>.tar`
- `realllmcn-public-web_<tag>.tar`
- `realllmcn-admin-web_<tag>.tar`

## Zero-Credential Publishing

When the user does not want to give server access:

1. Generate the release package locally.
2. Report the package path and SHA256 summary.
3. Give the exact upload/apply commands for the user to run manually.
4. Ask the user to paste verification output if they want help interpreting it.

Do not attempt SSH/SFTP.

## Minimal-Permission Publishing

Use only after the user confirms they have installed the restricted deploy user:

1. Upload files from `artifacts/release/<tag>` to `/www/wwwroot/realllm.cn/releases/<tag>`.
2. Run:

```bash
sudo /usr/local/sbin/cheapai-apply-release /www/wwwroot/realllm.cn/releases/<tag>
```

The server script validates checksums, tags rollback images, loads images, recreates only app services, reloads `cheapai-proxy`, and verifies health endpoints.

## Server Setup Reference

Use `deploy/production/release/install-minimal-deployer.sh` for one-time setup. It creates:

- user: `cheapai-deploy`
- writable release directory: `/www/wwwroot/realllm.cn/releases`
- sudo whitelist for `/usr/local/sbin/cheapai-apply-release /www/wwwroot/realllm.cn/releases/*`
- `.env` permission hardening to `0600 root:root`

Read `docs/最小权限发布方案.md` when the user asks for the rationale, setup steps, rollback, or security boundary.
