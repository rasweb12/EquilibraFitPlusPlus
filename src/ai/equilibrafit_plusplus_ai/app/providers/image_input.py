import base64
import binascii


def decode_image(image_base64: str) -> tuple[bytes, str] | None:
    """Accept bounded raw base64 JPEG/PNG/WebP inputs, without fetching URLs."""
    if len(image_base64) > 14 * 1024 * 1024:
        return None
    try:
        binary = base64.b64decode(image_base64, validate=True)
    except (ValueError, binascii.Error):
        return None
    if not binary or len(binary) > 10 * 1024 * 1024:
        return None
    if binary.startswith(b"\xff\xd8\xff"):
        return binary, "image/jpeg"
    if binary.startswith(b"\x89PNG\r\n\x1a\n"):
        return binary, "image/png"
    if binary[:4] == b"RIFF" and binary[8:12] == b"WEBP":
        return binary, "image/webp"
    return None
