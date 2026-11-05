#!/usr/bin/env bash
# Builds the local images and deploys them to Docker Desktop's Kubernetes (D54).
# The kubectl context is fixed to docker-desktop, whatever the current context is.
#   deploy/docker-desktop/deploy.sh             build + apply + restart
#   deploy/docker-desktop/deploy.sh --no-build  apply + restart only
# Run from anywhere; needs deploy/docker-desktop/secrets.env (template: secrets.env.example).
set -euo pipefail

context=docker-desktop
namespace=polls-pkp
here="$(cd "$(dirname "$0")" && pwd)"
kc() { kubectl --context "$context" "$@"; }

if [[ ! -f "$here/secrets.env" ]]; then
  echo "Missing $here/secrets.env – copy secrets.env.example and fill it in." >&2
  exit 1
fi

if [[ "${1:-}" != "--no-build" ]]; then
  "$here/../build.sh"
fi

kc apply -k "$here"
# Every build reuses the :dev tag, so apply alone would keep the old image running
kc -n "$namespace" rollout restart deploy/polls-api deploy/polls-web
kc -n "$namespace" rollout status deploy/polls-api --timeout=180s
kc -n "$namespace" rollout status deploy/polls-web --timeout=60s

echo "Ready: http://localhost:30090"
