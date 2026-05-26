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
checksum_file="$RELEASE_DIR/SHA256SUMS"

curl_with_retry() {
  local description="$1"
  shift
  local attempts="${CHEAPAI_HEALTH_CHECK_ATTEMPTS:-12}"
  local delay_seconds="${CHEAPAI_HEALTH_CHECK_DELAY_SECONDS:-5}"
  local attempt=1

  while (( attempt <= attempts )); do
    if curl "$@" >/dev/null 2>&1; then
      echo "OK: $description"
      return 0
    fi

    if (( attempt == attempts )); then
      echo "FAILED: $description" >&2
      curl "$@" >/dev/null
      return 1
    fi

    sleep "$delay_seconds"
    attempt=$((attempt + 1))
  done
}

echo "--- verify release package ---"
(
  cd "$RELEASE_DIR"
  if grep -q $'\r' "$checksum_file"; then
    tmp_checksum="$(mktemp)"
    tr -d '\015' < "$checksum_file" > "$tmp_checksum"
    cat "$tmp_checksum" > "$checksum_file"
    rm -f "$tmp_checksum"
  fi
  sha256sum -c "$checksum_file"
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
curl_with_retry "local api health" -fsS --max-time 10 http://127.0.0.1:18080/api/health
curl_with_retry "local public web" -fsSI --max-time 10 http://127.0.0.1:13000/
curl_with_retry "local admin web" -fsSI --max-time 10 http://127.0.0.1:18081/

echo "--- public health checks ---"
curl_with_retry "public home" -fsSI --max-time 10 https://www.realllm.cn/
curl_with_retry "public sites" -fsSI --max-time 10 https://www.realllm.cn/sites
curl_with_retry "public api health" -fsS --max-time 10 https://www.realllm.cn/api/health
curl_with_retry "admin home" -fsSI --max-time 10 https://admin.realllm.cn/

echo "--- compose ps ---"
docker compose --env-file .env ps

echo "Release applied: $release_tag"
