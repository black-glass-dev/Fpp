"""Reject oversized or unaudited candidates even when an APK signature is valid."""
def verify_payload(payload, apk_bytes, budget_receipt_present):
    if not budget_receipt_present:
        raise ValueError('Missing completed Android build-budget audit')
    if not 0 < apk_bytes < 500_000_000:
        raise ValueError('APK must be smaller than500MB, not another larger candidate')
    if payload.get('textures', 0) <= 0:
        raise ValueError('No compressed world textures recorded')
    before = payload.get('textureRgbaEquivalentBytesBefore', 0)
    after = payload.get('textureBytesAfter', 0)
    if not 0 < after < before:
        raise ValueError('Missing/inconsistent ASTC byte audit (RGBA equivalent, not phone RAM)')
    return True
