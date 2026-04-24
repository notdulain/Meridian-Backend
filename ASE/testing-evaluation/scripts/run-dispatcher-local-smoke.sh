#!/usr/bin/env bash

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BACKEND_ROOT="$(cd "${SCRIPT_DIR}/../../.." && pwd)"
ENV_FILE="${1:-${SCRIPT_DIR}/../env/local-k6.env}"
OUTPUT_FILE="${2:-${SCRIPT_DIR}/../evidence/dispatcher-local-smoke.txt}"
SUMMARY_FILE="${3:-${SCRIPT_DIR}/../evidence/dispatcher-local-smoke-summary.json}"

if ! command -v k6 >/dev/null 2>&1; then
  echo "k6 is not installed. Install it first, for example: brew install k6"
  exit 1
fi

if [[ ! -f "${ENV_FILE}" ]]; then
  echo "Missing env file: ${ENV_FILE}"
  echo "Create it from ASE/testing-evaluation/env/local-k6.env.example"
  exit 1
fi

mkdir -p "$(dirname "${OUTPUT_FILE}")"

set -a
source "${ENV_FILE}"
set +a

export K6_DISPATCHER_PROFILE="${K6_DISPATCHER_PROFILE:-demo}"
export BASE_URL="${BASE_URL:-http://localhost:5050}"
export K6_ASSIGNMENT_DURATION="${K6_ASSIGNMENT_DURATION:-5s}"

cd "${BACKEND_ROOT}"
k6 run \
  --summary-export "${SUMMARY_FILE}" \
  load-tests/dispatcher-session.js 2>&1 | tee "${OUTPUT_FILE}"
