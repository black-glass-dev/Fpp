# Unity Personal activation research — 2026-11-06

## Correction to earlier findings
The old `unity-builder@v4` workflow and our email/password-only editor command were insufficient to rule out automated Personal activation. Upstream now ships a different account-based licensing route. A failed editor command, or absence of a `.ulf` file, is not proof that email/password activation is impossible.

Verified upstream:
- GameCI CLI PR 246, merged 2026-09-05: https://github.com/game-ci/cli/pull/246
- Published builder v6.0.0: https://github.com/game-ci/unity-builder/releases/tag/v6.0.0
- Published CLI v0.1.72: https://github.com/game-ci/cli/releases/tag/v0.1.72
- Pinned activation implementation: https://github.com/game-ci/cli/blob/v0.1.72/dist/platforms/ubuntu/steps/activate.sh
- Pinned seat/verification diagnostics and strategy resolution: https://github.com/game-ci/cli/blob/v0.1.72/dist/platforms/ubuntu/steps/licensing_method.sh
- Unity describes Hub as the primary activation method: https://docs.unity3d.com/Manual/ManagingYourUnityLicense.html
- GameCI's older Hub/ULF-based instructions still exist, and should not be confused with the newer CLI route: https://github.com/game-ci/documentation/blob/main/docs/03-github/02-activation.mdx

## New workflow
`.github/workflows/android.yml` uses builder **v6.0.0** with CLI **v0.1.72**. With only `UNITY_EMAIL` and `UNITY_PASSWORD` supplied, the upstream auto strategy can choose Personal. No serial is invented and no paid subscription or license bypass is required by our configuration.

Newer compatible clients use `Unity.Licensing.Client --activate-all --include-personal --username ... --password ...`. These licensing-client flags are described by upstream as undocumented by Unity; compatibility and service behavior must be tested, not promised. Upstream probes client capabilities and supplies version-dependent fallbacks and seat-return handling. Its activation runs in the same build environment rather than trying to transfer entitlement state out of a disposable activation container.

## What the owner may need to do
Do not send passwords, verification codes, authenticator seeds or license files through chat.

If account verification is requested, sign into Unity yourself via its trusted site/Hub, verify the account/email and accept any required terms. Confirm that the account has a valid Personal entitlement and is eligible for it. A social sign-in is not automatically a Unity ID password login; check the account's supported sign-in method directly in Unity settings.

If seat availability is the error, review active machines/seats in Unity's account/Hub interface and release only machines no longer in use. Avoid concurrent jobs using the same entitlement.

If MFA/new-device verification cannot be handled by the hosted runner, keep MFA enabled and use a trusted Hub-activated compatible self-hosted runner, or Unity's supported licensing/support route. Do not weaken account security or give verification codes to an agent to make CI pass.

The `Safe Unity build diagnostics` workflow reads a completed run on GitHub's runner and publishes only predefined classifications and a bounded sanitized selection of errors, never the raw activation transcript or license data. Local outbound network rules prevent downloading the Actions log storage endpoint directly in this sandbox.

## Actual test result
Run https://github.com/black-glass-dev/Fpp/actions/runs/42206325110 selected Personal and reported `Activation complete.`. Sanitized diagnostics confirm that activation, not just an exit-code guess. Unity also reported a successful build and mentioned the APK output, but the overall builder wrapper exited 1 before artifact upload. A new workflow verifies a freshly produced APK and its signature independently, without treating a license cleanup error as proof that no APK was built. No APK is claimed from this run. The existing email/password secrets were sufficient to pass activation in this test.

## Published result
Run https://github.com/black-glass-dev/Fpp/actions/runs/42208662418 successfully verified APK ZIP integrity, ARM64 Unity/IL2CPP contents and Android signing, uploaded it and published https://github.com/black-glass-dev/Fpp/releases/tag/test-build-6 . Asset: `Fpp.apk`, 17,838,161 bytes, SHA256 `03d20c2985bcf021dd8bffd17cc941acfa576d4ffa53d8de609930988d6bd2ee` (GitHub release asset digest).

The builder wrapper still returned 1 even though a signed APK was produced; workflow `continue-on-error` is deliberately followed by mandatory independent APK and signing verification. No stale APK is restored by the Library cache. The first independent-verification run then failed only while writing a checksum into the root-owned Docker output directory; printing the checksum instead fixed publication. Do not equate the wrapper warning with a device test or a confirmed successful license return. Review seat/cleanup behavior before repeated CI runs; a Personal entitlement may need account-side cleanup if it remains held.

Activation now needs no additional password/license contribution from the owner for this tested route. Please test the released APK on the actual target phone and report device model, Android version, visual/controls defects and frame/thermal behavior. If future activation asks for verification, use the secure owner-assistance steps above rather than sharing codes or disabling MFA.
