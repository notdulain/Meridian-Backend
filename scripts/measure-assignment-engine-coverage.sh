#!/usr/bin/env bash

set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
report_dir="$repo_root/coveragereport/assignment-engine"
results_root="$repo_root/TestResults/assignment-engine"
assignment_results="$results_root/assignment"
delivery_results="$results_root/delivery"
assignment_reports="$assignment_results/**/coverage.cobertura.xml"
delivery_reports="$delivery_results/**/coverage.cobertura.xml"

cd "$repo_root"

rm -rf "$results_root" "$report_dir"

dotnet test src/AssignmentService/AssignmentService.Tests/AssignmentService.Tests.csproj \
  --collect:"XPlat Code Coverage" \
  --results-directory "$assignment_results"

dotnet test src/DeliveryService/DeliveryService.Tests/DeliveryService.Tests.csproj \
  --collect:"XPlat Code Coverage" \
  --results-directory "$delivery_results"

reportgenerator \
  -reports:"$assignment_reports;$delivery_reports" \
  -targetdir:"$report_dir" \
  -reporttypes:"TextSummary;Html"

echo
echo "Coverage report generated at: $report_dir"
echo
cat "$report_dir/Summary.txt"
