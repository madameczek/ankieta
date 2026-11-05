#!/usr/bin/env bash
# Builds both images and, when a registry is given, tags and pushes them (D37).
#   deploy/build.sh                      -> local images polls-api:dev, polls-web:dev
#   deploy/build.sh <registry> <tag>     -> <registry>/polls-api:<tag>, <registry>/polls-web:<tag>, pushed
# Run from the repository root.
set -euo pipefail

registry="${1:-}"
tag="${2:-dev}"
root="$(cd "$(dirname "$0")/.." && pwd)"

name() { if [[ -n "$registry" ]]; then echo "$registry/$1:$tag"; else echo "$1:$tag"; fi; }

api="$(name polls-api)"
web="$(name polls-web)"

docker build -f "$root/deploy/polls-api.Dockerfile" -t "$api" "$root/src/polls"
docker build -f "$root/deploy/polls-web.Dockerfile" --build-context deploy="$root/deploy" -t "$web" "$root/src/polls-angular"

if [[ -n "$registry" ]]; then
  docker push "$api"
  docker push "$web"
fi

echo "Built: $api $web"
