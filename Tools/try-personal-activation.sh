#!/usr/bin/env bash
# A real, bounded email/password-only attempt, not a license bypass.
# Never print Unity's activation log or credentials.
set -euo pipefail
if [[ -n "${UNITY_LICENSE:-}" || -n "${UNITY_SERIAL:-}" ]]; then exit 0; fi
if [[ -z "${UNITY_EMAIL:-}" || -z "${UNITY_PASSWORD:-}" ]]; then
  echo '::error::UNITY_EMAIL or UNITY_PASSWORD is absent. Check exact Actions secret names.'
  exit 1
fi
work="${RUNNER_TEMP}/fpp-personal-activation"
mkdir -p "$work/licenses"
trap 'rm -rf "$work"' EXIT
docker pull unityci/editor:ubuntu-2022.3.62f1-android-3 > "$work/pull.log" 2>&1 || { echo "::error::Unity Android container could not be downloaded."; exit 1; }
set +e
timeout 240 docker run --rm --entrypoint bash \
  -e UNITY_EMAIL -e UNITY_PASSWORD \
  -v "$work/licenses:/root/.local/share/unity3d/Unity" \
  unityci/editor:ubuntu-2022.3.62f1-android-3 \
  -c 'mkdir -p /tmp/BlankProject/Assets; unity-editor -batchmode -nographics -quit -projectPath /tmp/BlankProject -username "$UNITY_EMAIL" -password "$UNITY_PASSWORD" -logFile /tmp/activation.log' > "$work/docker.log" 2>&1
status=$?
set -e
license=$(find "$work/licenses" -type f -name '*.ulf' -print -quit)
if [[ -z "$license" ]]; then
  echo "::error::Email/password-only Personal activation produced no license (process exit $status). Unity may require Hub sign-in/interactive activation or a supported license. No credentials or private activation logs have been published."
  exit 1
fi
# Mask each line before writing an environment variable for the builder's standard license import.
while IFS= read -r line; do printf '::add-mask::%s\n' "$line"; done < "$license"
{
  echo 'PERSONAL_LICENSE<<FPP_LICENSE_EOF'
  cat "$license"
  printf '\nFPP_LICENSE_EOF\n'
} >> "$GITHUB_ENV"
echo 'Personal activation produced a license file; the build will validate it.'
