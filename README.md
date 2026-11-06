# FPP — Mobile FPS foundation

Unity mobile military FPS project. Original gameplay/art only; no Call of Duty assets or branding. **Current stage: cottage test-map generator and mobile movement code; Unity/device verification pending. Not a finished FPS.**

## Toolchain
- Unity **2022.3.62f1 LTS**, Android Build Support, SDK/NDK and OpenJDK (install via Unity Hub).
- Android 8+ (API 26), ARM64 device, OpenGL ES 3.
- Built-in renderer, landscape, initial 30 FPS cap, no realtime shadows/MSAA. These are starting defaults, not measured performance guarantees.

Open this folder in Unity. Use **FPP → Create cottage test map** for the imported cottage environment with movement; **FPP → Build Android APK** writes `build/Android/Fpp.apk`. Build generates the cottage scene only if missing, preserving later manual scene edits. The older **Create setup scene** command remains an empty-scene diagnostic. See [asset inventory](Docs/ASSETS.md) for provenance and the pending larger asset pack. Unity generates missing project settings and metadata on first import; retain generated `.meta` files when adding production assets.

## CI and activation
`.github/workflows/android.yml` uses GameCI to build the same APK. It runs on relevant branch pushes or manually in Actions. Workflows must first be pushed to GitHub. Manual dispatch may require the workflow to exist on the default branch first (GitHub restriction).

Repository **Settings → Secrets and variables → Actions**:
- `UNITY_LICENSE`: valid Unity license file content, where your license/Unity activation method supports it.
- `UNITY_EMAIL`, `UNITY_PASSWORD`: Unity account credentials used by GameCI activation.
- For eligible paid serial activation: also `UNITY_SERIAL` instead of `UNITY_LICENSE`.

Email/password alone does **not** guarantee headless Personal activation. Unity's current license terms and activation support apply. If activation fails, inspect the Actions logs and use a supported license/runner setup. Never commit license files or credentials or paste them into chat. GitHub secrets cannot be read back by an agent; listing secret names also needs repository permission.

Successful builds upload an APK artifact (14 days). Manual dispatch with `publish_release=true` publishes a GitHub **prerelease** attached to the exact built commit, only from this session's branch. No APK or activated editor has yet been verified.

## Updates / patch policy
For now each update is a **full APK**, not a binary delta or downloadable content patch. Preserve package ID `dev.blackglass.fpp`, increase version code, and use the **same signing certificate** for Android to install over an earlier version. CI uses run number as version code; reruns keep that number. Local builds default to 1. Set `ANDROID_VERSION_CODE` when needed.

**Important:** current builds use Unity's default debug signing for initial smoke tests. Ephemeral CI runners may produce different debug certificates, so seamless updates are NOT guaranteed. Before real distribution, add a dedicated persistent test/release keystore through encrypted secrets, wire up signing, and never commit it. Keep this signing key backed up. Switching keys later can require uninstalling the old app (losing its local data).

Remote content patches (e.g. Addressables), download hosting, checksums, resume/retry, storage limits and version compatibility will be designed after resource links and gameplay scope are supplied. No arbitrary remote code downloads.

## Next requirements
1. Device model, RAM, Android version, available storage, target FPS (30/60).
2. Offline campaign / bots / online multiplayer priority.
3. First level, movement, touch controls, weapons and visual style.
4. Asset download links and permission/licenses to use them.
5. Supported Unity activation and persistent APK signing setup.

Profile CPU/GPU frame times, memory, thermal behavior and download/build sizes on real devices. Plan object pooling, LODs, baked lighting, texture compression, limited draw calls and scalable quality after actual assets exist. No optimization target has been benchmarked yet.
