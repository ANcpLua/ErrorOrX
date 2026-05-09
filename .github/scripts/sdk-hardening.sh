#!/usr/bin/env bash
# SDK adoption + held-back drift audit.
# Bash 3.2-compatible (works on macOS dev boxes and GitHub Actions ubuntu).
#
# Inputs (env):
#   SDK_NAME                  e.g. "ANcpLua.NET.Sdk"
#   ALLOW_MICROSOFT_NET_SDK   newline-separated exact csproj paths exempt (Nuke, fixtures)
#   HELD_BACK                 newline-separated "path|reason" — known not-yet-migrated
#   BANNED_PACKAGES           newline-separated package IDs the SDK forbids
#   SDK_PIN_PACKAGES          newline-separated msbuild-sdk IDs to freshness-check
#
# Output:  human-readable markdown report → $GITHUB_STEP_SUMMARY (or stdout)
# Exit:    0 = clean / matches held-back; 1 = drift or new banned-pkg

set -euo pipefail

repo_root="${GITHUB_WORKSPACE:-$(pwd)}"
cd "$repo_root"

drift_csproj=()
resolved_held_back=()
banned_hits=()
hardcoded_version=()
stale_pins=()

# ── allowlist ─────────────────────────────────────────────────────────────
allow=()
while IFS= read -r line; do
  [ -z "$line" ] && continue
  allow+=("$line")
done <<< "${ALLOW_MICROSOFT_NET_SDK:-}"

is_allowlisted() {
  # Each allowlist entry is a bash case-glob. `*` matches across `/`, so
  # `*/TestAssets/*` covers every nested fixture without listing each.
  local path="$1" a
  for a in "${allow[@]+"${allow[@]}"}"; do
    case "$path" in
      $a) return 0 ;;
    esac
  done
  return 1
}

# ── held-back map (parallel arrays — bash 3.2 has no assoc arrays) ────────
held_back_paths=()
held_back_reasons=()
while IFS='|' read -r p r; do
  [ -z "$p" ] && continue
  held_back_paths+=("$p")
  held_back_reasons+=("$r")
done <<< "${HELD_BACK:-}"

held_back_reason_for() {
  local target="$1" i
  for ((i=0; i<${#held_back_paths[@]}; i++)); do
    if [ "${held_back_paths[$i]}" = "$target" ]; then
      printf '%s' "${held_back_reasons[$i]}"
      return 0
    fi
  done
  return 1
}

is_held_back() { held_back_reason_for "$1" >/dev/null 2>&1; }

# ── 1. find production csproj ─────────────────────────────────────────────
all_csproj=()
while IFS= read -r line; do
  all_csproj+=("$line")
done < <(find . -name "*.csproj" \
  -not -path "*/bin/*" -not -path "*/obj/*" \
  -not -path "*/artifacts/*" -not -path "*/node_modules/*" \
  -not -path "*/.git/*" \
  | sed 's|^\./||' | sort)

# ── 2. classify each csproj ───────────────────────────────────────────────
for csproj in "${all_csproj[@]+"${all_csproj[@]}"}"; do
  sdk_attr=$(grep -oE '<Project Sdk="[^"]+"' "$csproj" 2>/dev/null | head -1 | sed 's/<Project Sdk="//;s/"$//' || true)
  case "$sdk_attr" in
    ANcpLua.NET.Sdk*)
      if is_held_back "$csproj"; then
        resolved_held_back+=("$csproj")
      fi
      ;;
    Microsoft.NET.Sdk*)
      if is_allowlisted "$csproj" || is_held_back "$csproj"; then
        :
      else
        drift_csproj+=("$csproj")
      fi
      ;;
  esac

  # hardcoded <Version>X.Y.Z</Version> — but allow the VersionPrefix conditional pattern
  if grep -qE '<Version>[0-9]' "$csproj" 2>/dev/null; then
    if ! grep -q 'VersionPrefix' "$csproj" 2>/dev/null; then
      hardcoded_version+=("$csproj")
    fi
  fi
  # <PackageVersion>X.Y.Z</PackageVersion> property form (legacy override; differs from CPM item form)
  if grep -qE '<PackageVersion>[0-9].*</PackageVersion>' "$csproj" 2>/dev/null; then
    hardcoded_version+=("$csproj  (PackageVersion property)")
  fi
done

# ── 3. banned packages ────────────────────────────────────────────────────
banned=()
while IFS= read -r line; do
  [ -z "$line" ] && continue
  banned+=("$line")
done <<< "${BANNED_PACKAGES:-PolySharp
FluentAssertions
Microsoft.NET.Test.Sdk}"

for csproj in "${all_csproj[@]+"${all_csproj[@]}"}"; do
  for pkg in "${banned[@]+"${banned[@]}"}"; do
    if grep -qE "PackageReference[[:space:]]+Include=\"${pkg}\"" "$csproj" 2>/dev/null; then
      if is_held_back "$csproj"; then
        banned_hits+=("$csproj | $pkg | held-back")
      else
        banned_hits+=("$csproj | $pkg | DRIFT")
      fi
    fi
  done
done
if [ -f Directory.Packages.props ]; then
  for pkg in "${banned[@]+"${banned[@]}"}"; do
    if grep -qE "PackageVersion[[:space:]]+Include=\"${pkg}\"" Directory.Packages.props 2>/dev/null; then
      banned_hits+=("Directory.Packages.props | $pkg | CPM")
    fi
  done
fi

# ── 4. SDK pin freshness ──────────────────────────────────────────────────
if [ -f global.json ]; then
  pins=()
  while IFS= read -r line; do
    [ -z "$line" ] && continue
    pins+=("$line")
  done <<< "${SDK_PIN_PACKAGES:-}"
  for sdk_id in "${pins[@]+"${pins[@]}"}"; do
    pinned=$(jq -r --arg k "$sdk_id" '.["msbuild-sdks"][$k] // empty' global.json 2>/dev/null || true)
    [ -z "$pinned" ] && continue
    sdk_lower=$(printf '%s' "$sdk_id" | tr '[:upper:]' '[:lower:]')
    latest=$(curl -fsSL "https://api.nuget.org/v3-flatcontainer/${sdk_lower}/index.json" 2>/dev/null \
      | jq -r '.versions | map(select(test("-") | not)) | .[-1] // empty' 2>/dev/null || true)
    if [ -n "$latest" ] && [ "$pinned" != "$latest" ]; then
      stale_pins+=("$sdk_id | pinned=$pinned | latest=$latest")
    fi
  done
fi

# ── 5. render report ──────────────────────────────────────────────────────
out="${GITHUB_STEP_SUMMARY:-/dev/stdout}"
{
  echo "# SDK Hardening Audit"
  echo
  echo "_SDK: \`${SDK_NAME:-ANcpLua.NET.Sdk}\` — repo: \`${GITHUB_REPOSITORY:-$(basename "$repo_root")}\` — $(date -u +%FT%TZ)_"
  echo
  echo "## Drift"
  if [ ${#drift_csproj[@]} -eq 0 ]; then
    echo "_None — every csproj outside the allowlist is on the SDK._"
  else
    echo
    for p in "${drift_csproj[@]}"; do echo "- \`$p\` uses \`Microsoft.NET.Sdk\` and is neither allowlisted nor held-back"; done
  fi
  echo
  echo "## Held-back (tracked debt)"
  if [ ${#held_back_paths[@]} -eq 0 ]; then
    echo "_None._"
  else
    for ((i=0; i<${#held_back_paths[@]}; i++)); do echo "- \`${held_back_paths[$i]}\` — ${held_back_reasons[$i]}"; done
  fi
  echo
  echo "## Resolved held-back (drop from config)"
  if [ ${#resolved_held_back[@]} -eq 0 ]; then
    echo "_None._"
  else
    for p in "${resolved_held_back[@]}"; do echo "- \`$p\` is now on the SDK"; done
  fi
  echo
  echo "## Banned packages"
  if [ ${#banned_hits[@]} -eq 0 ]; then
    echo "_None._"
  else
    for h in "${banned_hits[@]}"; do echo "- $h"; done
  fi
  echo
  echo "## Hardcoded \`<Version>\` overrides"
  if [ ${#hardcoded_version[@]} -eq 0 ]; then
    echo "_None — every csproj defers to CI's \`-p:Version=\` global property._"
  else
    for h in "${hardcoded_version[@]}"; do echo "- \`$h\`"; done
  fi
  echo
  echo "## SDK pin freshness vs nuget.org"
  if [ ${#stale_pins[@]} -eq 0 ]; then
    echo "_All pins on latest stable._"
  else
    for s in "${stale_pins[@]}"; do echo "- $s"; done
  fi
} > "$out"

# ── 6. exit code ──────────────────────────────────────────────────────────
fail=0
if [ ${#drift_csproj[@]} -gt 0 ]; then
  echo "::error::${#drift_csproj[@]} csproj(s) drifted off the SDK"
  fail=1
fi
new_banned=()
for h in "${banned_hits[@]+"${banned_hits[@]}"}"; do
  case "$h" in *DRIFT) new_banned+=("$h");; esac
done
if [ ${#new_banned[@]} -gt 0 ]; then
  echo "::error::${#new_banned[@]} banned-package reference(s) outside held-back list"
  fail=1
fi
if [ ${#resolved_held_back[@]} -gt 0 ]; then
  echo "::warning::${#resolved_held_back[@]} held-back csproj(s) now on SDK — drop from HELD_BACK"
fi
if [ ${#stale_pins[@]} -gt 0 ]; then
  echo "::warning::${#stale_pins[@]} SDK pin(s) behind nuget.org latest"
fi
exit $fail
