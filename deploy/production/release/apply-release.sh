#!/usr/bin/env bash
set -Eeuo pipefail

COMPOSE_DIR="${CHEAPAI_COMPOSE_DIR:-/www/wwwroot/realllm.cn}"
RELEASE_DIR="${1:-}"

if [[ -z "$RELEASE_DIR" ]]; then
  echo "Usage: $0 /www/wwwroot/realllm.cn/releases/<release-tag>" >&2
  exit 64
fi

if [[ ! -d "$COMPOSE_DIR" ]]; then
  echo "Compose directory not found: $COMPOSE_DIR" >&2
  exit 65
fi

if [[ ! -f "$RELEASE_DIR/SHA256SUMS" ]]; then
  echo "Checksum file not found: $RELEASE_DIR/SHA256SUMS" >&2
  exit 66
fi

release_tag="$(basename "$RELEASE_DIR")"
services=(api jobs public-web admin-web)
repositories=(realllmcn-api realllmcn-jobs realllmcn-public-web realllmcn-admin-web)

echo "--- verify release package ---"
(
  cd "$RELEASE_DIR"
  sha256sum -c SHA256SUMS
)

cd "$COMPOSE_DIR"

echo "--- tag rollback images ---"
for repository in "${repositories[@]}"; do
  if docker image inspect "${repository}:latest" >/dev/null 2>&1; then
    docker tag "${repository}:latest" "${repository}:rollback-before-${release_tag}"
    echo "${repository}:rollback-before-${release_tag}"
  fi
done

echo "--- load images ---"
for tar_file in "$RELEASE_DIR"/*.tar; do
  echo "LOAD=$tar_file"
  docker load -i "$tar_file"
done

echo "--- recreate app services ---"
docker compose --env-file .env up -d --no-deps --force-recreate "${services[@]}"

if docker ps --format '{{.Names}}' | grep -qx 'cheapai-proxy'; then
  echo "--- reload proxy nginx ---"
  docker exec cheapai-proxy nginx -s reload
fi

echo "--- local health checks ---"
curl -fsS http://127.0.0.1:18080/api/health >/dev/null
curl -fsSI http://127.0.0.1:13000/ >/dev/null
curl -fsSI http://127.0.0.1:18081/ >/dev/null

echo "--- public health checks ---"
curl -fsSI https://www.realllm.cn/ >/dev/null
curl -fsSI https://www.realllm.cn/sites >/dev/null
curl -fsS https://www.realllm.cn/api/health >/dev/null
curl -fsSI https://admin.realllm.cn/ >/dev/null

echo "--- compose ps ---"
docker compose --env-file .env ps

echo "Release applied: $release_tag"
