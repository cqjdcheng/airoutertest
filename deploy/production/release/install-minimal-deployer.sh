#!/usr/bin/env bash
set -Eeuo pipefail

DEPLOY_USER="${CHEAPAI_DEPLOY_USER:-cheapai-deploy}"
COMPOSE_DIR="${CHEAPAI_COMPOSE_DIR:-/www/wwwroot/realllm.cn}"
RELEASES_DIR="${CHEAPAI_RELEASES_DIR:-$COMPOSE_DIR/releases}"
APPLY_SOURCE="${1:-$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/apply-release.sh}"
APPLY_TARGET="/usr/local/sbin/cheapai-apply-release"

if [[ "$(id -u)" -ne 0 ]]; then
  echo "Run as root on the production server." >&2
  exit 77
fi

if [[ ! -f "$APPLY_SOURCE" ]]; then
  echo "Apply script not found: $APPLY_SOURCE" >&2
  exit 78
fi

install -m 0755 "$APPLY_SOURCE" "$APPLY_TARGET"

if ! id "$DEPLOY_USER" >/dev/null 2>&1; then
  useradd --create-home --shell /bin/bash "$DEPLOY_USER"
fi

mkdir -p "$RELEASES_DIR"
chown "$DEPLOY_USER:$DEPLOY_USER" "$RELEASES_DIR"
chmod 0750 "$RELEASES_DIR"

if [[ -f "$COMPOSE_DIR/.env" ]]; then
  chown root:root "$COMPOSE_DIR/.env"
  chmod 0600 "$COMPOSE_DIR/.env"
fi

cat >"/etc/sudoers.d/cheapai-deploy" <<EOF
Defaults:$DEPLOY_USER !requiretty
$DEPLOY_USER ALL=(root) NOPASSWD: $APPLY_TARGET $RELEASES_DIR/*
EOF
chmod 0440 "/etc/sudoers.d/cheapai-deploy"
visudo -cf "/etc/sudoers.d/cheapai-deploy" >/dev/null

echo "Minimal deploy user installed: $DEPLOY_USER"
echo "Upload release packages to: $RELEASES_DIR/<release-tag>"
echo "Apply with: sudo $APPLY_TARGET $RELEASES_DIR/<release-tag>"
