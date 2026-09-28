"""
Draws the cartoon weapon sprites of the arena kit (thick outline, flat colors, light band on top).

    pip install pymupdf
    python Tools/weapon_sprites.py

Writes the PNGs to Assets/Kit/Art/Weapons and Assets/Kit/Art/Weapons/Projectiles, plus
weapons.json with the grip (sprite pivot) and muzzle position of every weapon in pixels.
Coordinates below are in pixels of the final sprite at 100 pixels per unit (like the bazooka),
y pointing down. Everything is rendered at twice that resolution.
"""

import json
import os

import fitz  # PyMuPDF

ROOT = os.path.join(os.path.dirname(__file__), "..", "Assets", "Kit", "Art", "Weapons")
SCALE = 2          # render resolution (pixels per design pixel)
OUTLINE = 9        # outline width in design pixels
PADDING = 8

OUT = (0.21, 0.20, 0.25)
METAL = (0.64, 0.68, 0.74)
METAL_DARK = (0.43, 0.46, 0.53)
DARK = (0.29, 0.29, 0.34)
WOOD = (0.76, 0.47, 0.26)
WOOD_DARK = (0.58, 0.33, 0.18)
ORANGE = (0.98, 0.56, 0.20)
YELLOW = (1.00, 0.80, 0.22)
GREEN = (0.38, 0.76, 0.36)
GREEN_DARK = (0.23, 0.52, 0.26)
RED = (0.91, 0.30, 0.25)
BLUE = (0.33, 0.62, 0.96)
CYAN = (0.35, 0.90, 0.96)
PURPLE = (0.60, 0.42, 0.86)
WHITE = (0.95, 0.95, 0.97)


def lighter(color, amount=0.35):
    return tuple(c + (1 - c) * amount for c in color)


def darker(color, amount=0.25):
    return tuple(c * (1 - amount) for c in color)


# --- Parts -------------------------------------------------------------------

def rrect(x0, y0, x1, y1, fill, radius=6, shine=True):
    return {"type": "rect", "rect": (x0, y0, x1, y1), "fill": fill, "radius": radius, "shine": shine}


def poly(points, fill):
    return {"type": "poly", "points": points, "fill": fill}


def circle(cx, cy, r, fill, shine=True):
    return {"type": "circle", "center": (cx, cy), "r": r, "fill": fill, "shine": shine}


def line(x0, y0, x1, y1, color=OUT, width=3):
    return {"type": "line", "from": (x0, y0), "to": (x1, y1), "color": color, "width": width}


def bounds(parts):
    xs, ys = [], []
    for part in parts:
        if part["type"] == "rect":
            x0, y0, x1, y1 = part["rect"]
            xs += [x0, x1]
            ys += [y0, y1]
        elif part["type"] == "poly":
            xs += [p[0] for p in part["points"]]
            ys += [p[1] for p in part["points"]]
        elif part["type"] == "circle":
            (cx, cy), r = part["center"], part["r"]
            xs += [cx - r, cx + r]
            ys += [cy - r, cy + r]
    return min(xs), min(ys), max(xs), max(ys)


def draw_part(shape, part, offset, outline_pass):
    ox, oy = offset
    s = SCALE
    if part["type"] == "line":
        if outline_pass:
            return
        shape.draw_line(fitz.Point((part["from"][0] + ox) * s, (part["from"][1] + oy) * s),
                        fitz.Point((part["to"][0] + ox) * s, (part["to"][1] + oy) * s))
        shape.finish(color=part["color"], width=part["width"] * s, lineCap=1)
        return

    if part["type"] == "rect":
        x0, y0, x1, y1 = part["rect"]
        rect = fitz.Rect((x0 + ox) * s, (y0 + oy) * s, (x1 + ox) * s, (y1 + oy) * s)
        radius = min(part["radius"] / max(1, min(x1 - x0, y1 - y0)), 0.5)
        shape.draw_rect(rect, radius=radius if radius > 0 else None)
    elif part["type"] == "poly":
        shape.draw_polyline([fitz.Point((x + ox) * s, (y + oy) * s) for x, y in part["points"]])
    elif part["type"] == "circle":
        (cx, cy), r = part["center"], part["r"]
        shape.draw_circle(fitz.Point((cx + ox) * s, (cy + oy) * s), r * s)

    if outline_pass:
        shape.finish(color=OUT, fill=OUT, width=OUTLINE * 2 * s, closePath=True, lineJoin=1)
    else:
        shape.finish(color=darker(part["fill"], 0.45), fill=part["fill"], width=2.5 * s, closePath=True, lineJoin=1)


def draw_shine(shape, part, offset):
    """A lighter band on the upper third, like the Kenney tiles."""
    if not part.get("shine"):
        return
    ox, oy = offset
    s = SCALE
    if part["type"] == "rect":
        x0, y0, x1, y1 = part["rect"]
        height = y1 - y0
        if height < 10 or x1 - x0 < 10:
            return
        inset = min(5, height * 0.18)
        band = fitz.Rect((x0 + inset + ox) * s, (y0 + inset + oy) * s, (x1 - inset + ox) * s, (y0 + inset + height * 0.28 + oy) * s)
        shape.draw_rect(band, radius=0.5)
        shape.finish(color=None, fill=lighter(part["fill"]), width=0)
    elif part["type"] == "circle":
        (cx, cy), r = part["center"], part["r"]
        if r < 8:
            return
        shape.draw_circle(fitz.Point((cx - r * 0.35 + ox) * s, (cy - r * 0.35 + oy) * s), r * 0.28 * s)
        shape.finish(color=None, fill=lighter(part["fill"], 0.55), width=0)


def render(name, parts, folder, grip=None, muzzle=None):
    x0, y0, x1, y1 = bounds(parts)
    margin = OUTLINE + PADDING
    offset = (margin - x0, margin - y0)
    width = x1 - x0 + margin * 2
    height = y1 - y0 + margin * 2

    doc = fitz.open()
    page = doc.new_page(width=width * SCALE, height=height * SCALE)
    shape = page.new_shape()
    for part in parts:
        draw_part(shape, part, offset, outline_pass=True)
    for part in parts:
        draw_part(shape, part, offset, outline_pass=False)
        draw_shine(shape, part, offset)
    shape.commit()

    os.makedirs(folder, exist_ok=True)
    page.get_pixmap(dpi=72, alpha=True).save(os.path.join(folder, name + ".png"))

    # Positions in pixels of the rendered image (origin top left).
    info = {"name": name, "size": [width * SCALE, height * SCALE]}
    if grip:
        info["grip"] = [(grip[0] + offset[0]) * SCALE, (grip[1] + offset[1]) * SCALE]
    if muzzle:
        info["muzzle"] = [(muzzle[0] + offset[0]) * SCALE, (muzzle[1] + offset[1]) * SCALE]
    return info


# --- Weapons -------------------------------------------------------------------
# grip = where the front hand holds the weapon, muzzle = where projectiles come out.

WEAPONS = {
    "Pistol": dict(
        parts=[
            poly([(30, 34), (62, 34), (56, 86), (24, 86)], DARK),
            rrect(116, 15, 131, 31, DARK, 3, shine=False),
            rrect(8, 6, 122, 38, METAL, 9),
            rrect(40, 27, 104, 33, ORANGE, 2, shine=False),
            line(70, 10, 70, 24, darker(METAL, 0.3), 3),
            line(80, 10, 80, 24, darker(METAL, 0.3), 3),
        ],
        grip=(44, 44), muzzle=(131, 23)),

    "Shotgun": dict(
        parts=[
            poly([(0, 30), (92, 22), (98, 60), (8, 74)], WOOD),
            poly([(108, 48), (134, 48), (128, 80), (102, 80)], WOOD_DARK),
            rrect(160, 19, 336, 33, METAL, 5),
            rrect(160, 33, 336, 47, METAL_DARK, 5, shine=False),
            rrect(80, 18, 172, 54, DARK, 8),
            rrect(196, 42, 256, 60, WOOD, 7),
        ],
        grip=(120, 52), muzzle=(336, 33)),

    "GrenadeLauncher": dict(
        parts=[
            poly([(0, 38), (46, 32), (46, 70), (8, 86)], DARK),
            poly([(70, 64), (96, 64), (90, 108), (64, 108)], DARK),
            rrect(40, 28, 150, 70, GREEN, 12),
            rrect(120, 20, 262, 62, GREEN, 16),
            rrect(250, 14, 270, 68, DARK, 6, shine=False),
            circle(112, 64, 32, GREEN_DARK),
            circle(100, 56, 7, DARK, shine=False),
            circle(124, 56, 7, DARK, shine=False),
            circle(112, 76, 7, DARK, shine=False),
        ],
        grip=(82, 70), muzzle=(270, 41)),

    "Minigun": dict(
        parts=[
            poly([(18, 40), (64, 34), (64, 72), (26, 92)], DARK),
            rrect(70, 76, 132, 112, GREEN_DARK, 6),
            rrect(140, 18, 326, 34, METAL, 6),
            rrect(140, 36, 326, 52, METAL_DARK, 6, shine=False),
            rrect(140, 54, 326, 70, METAL, 6),
            rrect(198, 12, 214, 76, DARK, 4, shine=False),
            rrect(298, 12, 314, 76, DARK, 4, shine=False),
            rrect(56, 12, 152, 80, YELLOW, 18),
            rrect(80, 0, 128, 14, DARK, 5, shine=False),
        ],
        grip=(100, 80), muzzle=(326, 44)),

    # --- Extra sprites for the students' own weapons ---

    "Revolver": dict(
        parts=[
            poly([(22, 32), (52, 32), (46, 88), (12, 82)], WOOD),
            rrect(62, 13, 140, 29, METAL, 5),
            rrect(22, 8, 70, 38, METAL_DARK, 8),
            rrect(40, 4, 82, 42, METAL, 11),
            line(52, 12, 52, 34, darker(METAL, 0.35), 3),
            line(64, 12, 64, 34, darker(METAL, 0.35), 3),
        ],
        grip=(36, 42), muzzle=(140, 21)),

    "Blaster": dict(
        parts=[
            poly([(40, 48), (72, 48), (64, 100), (32, 100)], DARK),
            poly([(122, 18), (166, 24), (166, 44), (122, 50)], WHITE),
            rrect(16, 12, 134, 54, PURPLE, 20),
            rrect(30, 40, 120, 46, CYAN, 3, shine=False),
            circle(46, 26, 6, CYAN, shine=False),
            circle(64, 26, 6, CYAN, shine=False),
        ],
        grip=(54, 54), muzzle=(166, 34)),

    "LaserRifle": dict(
        parts=[
            poly([(0, 28), (72, 22), (72, 60), (8, 72)], WHITE),
            poly([(120, 50), (148, 50), (142, 90), (114, 90)], DARK),
            rrect(206, 26, 312, 42, DARK, 6, shine=False),
            rrect(58, 16, 222, 56, WHITE, 14),
            rrect(76, 42, 206, 48, CYAN, 3, shine=False),
            rrect(110, 0, 182, 18, DARK, 7),
            circle(312, 34, 11, CYAN),
        ],
        grip=(132, 56), muzzle=(322, 34)),

    "SMG": dict(
        parts=[
            rrect(0, 20, 36, 34, METAL_DARK, 5, shine=False),
            poly([(40, 44), (66, 44), (60, 92), (34, 92)], DARK),
            poly([(84, 44), (108, 44), (102, 106), (78, 106)], YELLOW),
            rrect(144, 21, 190, 37, METAL, 5),
            rrect(28, 12, 152, 48, DARK, 10),
            rrect(44, 36, 136, 42, YELLOW, 2, shine=False),
        ],
        grip=(52, 48), muzzle=(190, 29)),

    "SniperRifle": dict(
        parts=[
            poly([(0, 26), (112, 22), (112, 60), (14, 74)], GREEN_DARK),
            poly([(150, 50), (176, 50), (170, 88), (144, 88)], GREEN_DARK),
            rrect(222, 30, 402, 42, DARK, 5, shine=False),
            rrect(394, 22, 420, 50, DARK, 5, shine=False),
            rrect(98, 20, 232, 54, GREEN, 12),
            rrect(128, 0, 232, 18, DARK, 8),
            circle(232, 9, 8, CYAN, shine=False),
            circle(60, 40, 8, GREEN, shine=False),
            circle(170, 34, 7, GREEN_DARK, shine=False),
        ],
        grip=(162, 54), muzzle=(420, 36)),

    "WaterGun": dict(
        parts=[
            poly([(50, 60), (82, 60), (74, 112), (44, 112)], darker(ORANGE, 0.15)),
            rrect(150, 40, 200, 56, YELLOW, 6),
            rrect(18, 32, 162, 66, ORANGE, 17),
            rrect(40, 0, 124, 38, BLUE, 19),
        ],
        grip=(64, 66), muzzle=(200, 48)),

    "Flamethrower": dict(
        parts=[
            poly([(120, 58), (148, 58), (142, 100), (114, 100)], DARK),
            rrect(186, 24, 290, 48, METAL, 8),
            rrect(280, 18, 300, 54, DARK, 5, shine=False),
            rrect(50, 28, 204, 62, METAL_DARK, 12),
            rrect(0, 36, 72, 112, RED, 30),
            circle(294, 64, 7, ORANGE, shine=False),
        ],
        grip=(132, 62), muzzle=(300, 36)),

    "HandCannon": dict(
        parts=[
            poly([(70, 72), (98, 72), (92, 108), (64, 108)], WOOD),
            poly([(40, 12), (222, 20), (222, 72), (40, 82)], DARK),
            rrect(62, 12, 82, 82, YELLOW, 5, shine=False),
            rrect(150, 17, 170, 76, YELLOW, 5, shine=False),
            rrect(206, 16, 226, 76, YELLOW, 5, shine=False),
            circle(40, 47, 34, DARK),
        ],
        grip=(82, 76), muzzle=(226, 46)),
}

PROJECTILES = {
    "LaserBolt": [rrect(0, 0, 60, 16, CYAN, 8, shine=False), rrect(10, 5, 50, 11, WHITE, 3, shine=False)],
    "WaterDrop": [circle(14, 20, 13, BLUE), poly([(3, 16), (14, 0), (25, 16)], BLUE)],
    "CannonBall": [circle(20, 20, 20, DARK)],
}


def main():
    weapons = [render(name, weapon["parts"], ROOT, weapon["grip"], weapon["muzzle"]) for name, weapon in WEAPONS.items()]
    for name, parts in PROJECTILES.items():
        render(name, parts, os.path.join(ROOT, "Projectiles"))
    with open(os.path.join(ROOT, "weapons.json"), "w", encoding="utf-8") as file:
        json.dump({"weapons": weapons}, file, indent=2)
    print("wrote", len(weapons) + len(PROJECTILES), "sprites to", os.path.abspath(ROOT))


if __name__ == "__main__":
    main()
