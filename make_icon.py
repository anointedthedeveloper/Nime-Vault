"""
Generates Resources/Icons/app.ico from the gemini-svg.svg design.
Uses only Pillow — no external SVG renderer needed.
Produces sizes: 16, 32, 48, 64, 128, 256.
"""

import os, math
from PIL import Image, ImageDraw

os.makedirs("Resources/Icons", exist_ok=True)

def draw_logo(size: int) -> Image.Image:
    img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    s = size

    # ── Background squircle (approximated with rounded rectangle) ──────────
    rx = max(1, round(s * 0.225))          # corner radius ≈ 115/512 * s
    bg_color = (13, 14, 21, 255)           # #0D0E15
    d.rounded_rectangle([0, 0, s - 1, s - 1], radius=rx, fill=bg_color)

    # ── Outer border ring ───────────────────────────────────────────────────
    bw = max(1, round(s * 0.016))          # stroke-width ≈ 8/512
    border_color = (124, 58, 237, 64)      # #7C3AED at 25% opacity
    d.rounded_rectangle([bw//2, bw//2, s-1-bw//2, s-1-bw//2],
                        radius=max(1, rx - bw),
                        outline=border_color, width=bw)

    cx, cy = s / 2, s / 2
    r_track = s * 0.3125                   # 160/512

    # ── Vault lock outer circle (faint) ─────────────────────────────────────
    tw = max(1, round(s * 0.031))          # 16/512
    tc = (124, 58, 237, 51)               # #7C3AED at 20%
    bbox = [cx - r_track, cy - r_track, cx + r_track, cy + r_track]
    d.arc(bbox, start=0, end=360, fill=None)
    # Draw as ellipse outline
    d.ellipse(bbox, outline=tc, width=tw)

    # ── Pink arc (top-right quarter) ────────────────────────────────────────
    if size >= 32:
        aw = max(2, round(s * 0.035))      # 18/512
        arc_color = (236, 72, 153, 255)    # #EC4899
        # Arc from -90° (top) to about -30° (upper right)
        d.arc(bbox, start=-90, end=-30, fill=arc_color, width=aw)

    # ── Geometric N stroke ──────────────────────────────────────────────────
    # Original path: M152 352 V160 L288 352 V160  (in 512 coordinate space)
    # Map to current size
    def px(v): return round(v / 512 * s)

    nw = max(2, round(s * 0.074))          # 38/512
    nc = (124, 58, 237, 255)              # #7C3AED

    p1 = (px(152), px(352))
    p2 = (px(152), px(160))
    p3 = (px(288), px(352))
    p4 = (px(288), px(160))

    if size >= 16:
        d.line([p1, p2], fill=nc, width=max(1, nw))
        d.line([p2, p3], fill=nc, width=max(1, nw))
        d.line([p3, p4], fill=nc, width=max(1, nw))

    # ── Central core circle ─────────────────────────────────────────────────
    if size >= 32:
        core_cx = px(288)
        core_cy = px(256)
        outer_r = px(28)
        inner_r = px(10)
        cw = max(2, round(s * 0.020))      # 10/512

        # Outer ring
        d.ellipse([core_cx - outer_r, core_cy - outer_r,
                   core_cx + outer_r, core_cy + outer_r],
                  fill=bg_color, outline=nc, width=cw)

        # Pink dot centre
        d.ellipse([core_cx - inner_r, core_cy - inner_r,
                   core_cx + inner_r, core_cy + inner_r],
                  fill=(236, 72, 153, 255))

    return img


sizes = [16, 32, 48, 64, 128, 256]
frames = [draw_logo(s) for s in sizes]

# Save .ico (multi-size)
frames[0].save(
    "Resources/Icons/app.ico",
    format="ICO",
    append_images=frames[1:],
    sizes=[(s, s) for s in sizes]
)

# Also save a 256×256 PNG for use as WPF logo
frames[-1].save("Resources/Icons/logo.png", format="PNG")

print("Generated: Resources/Icons/app.ico")
print("Generated: Resources/Icons/logo.png")
