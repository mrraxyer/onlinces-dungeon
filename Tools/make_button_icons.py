"""Genera Assets/UI/Sprites/buttons.png (íconos de botones: Xbox, PlayStation y teclado)
y buttons.json con el rect de cada ícono (lo usa el editor para crear el sprite asset de TMP).
Uso: python Tools/make_button_icons.py   (desde la raíz del proyecto)
"""
import json
import re

from PIL import Image, ImageDraw

OUT = "Assets/UI/Sprites/buttons"
H = 16  # alto de cada ícono (px de arte)
PAD = 2
WHITE = (255, 255, 255, 255)
INK = (24, 20, 37, 255)
PAD_BODY = (52, 50, 66, 255)
PAD_EDGE = (22, 20, 30, 255)
KEY_FACE = (232, 230, 245, 255)
KEY_SIDE = (150, 146, 170, 255)


def font_text(text, color):
    fnt = open("Assets/Fonts/pixel.fnt", encoding="utf8").read()
    sheet = Image.open("Assets/Fonts/pixel.png").convert("RGBA")
    chars = {int(m[0]): tuple(map(int, m[1:])) for m in
             re.findall(r"char id=(\d+) x=(\d+) y=(\d+) width=(\d+) height=(\d+) xoffset=(-?\d+) yoffset=(-?\d+) xadvance=(\d+)", fnt)}
    w = sum(chars[ord(c)][6] + 1 for c in text)
    out = Image.new("RGBA", (w, 12))
    x = 0
    for c in text:
        cx, cy, cw, ch, xo, yo, adv = chars[ord(c)]
        g = sheet.crop((cx, cy, cx + cw, cy + ch))
        out.paste(Image.new("RGBA", g.size, color), (x + xo, yo), g.split()[3].point(lambda a: 255 if a > 127 else 0))
        x += adv + 1
    return out.crop(out.getbbox())


def center(img, part, dy=0):
    img.alpha_composite(part, ((img.width - part.width) // 2, (img.height - part.height) // 2 + dy))


def darker(c, k=0.6):
    return tuple(int(v * k) for v in c[:3]) + (255,)


def round_button(fill):
    img = Image.new("RGBA", (H, H))
    d = ImageDraw.Draw(img)
    d.ellipse((0, 0, H - 1, H - 1), fill=darker(fill, 0.45))
    d.ellipse((1, 1, H - 2, H - 3), fill=fill)
    return img, d


def xbox(letter, color):
    img, _ = round_button(color)
    center(img, font_text(letter, WHITE), -1)
    return img


def ps(symbol, color):
    img, d = round_button(PAD_BODY)
    c = 7.5
    if symbol == "cross":
        d.line((4, 3, 11, 10), fill=color, width=2)
        d.line((11, 3, 4, 10), fill=color, width=2)
    elif symbol == "circle":
        d.ellipse((3, 2, 12, 11), outline=color, width=2)
    elif symbol == "square":
        d.rectangle((4, 3, 11, 10), outline=color, width=2)
    elif symbol == "triangle":
        d.polygon([(c, 2), (12.5, 11), (2.5, 11)], outline=color, width=2)
    return img


def pill(label, w=22):
    img = Image.new("RGBA", (w, H))
    d = ImageDraw.Draw(img)
    d.rounded_rectangle((0, 1, w - 1, H - 1), 5, fill=PAD_EDGE)
    d.rounded_rectangle((1, 2, w - 2, H - 3), 4, fill=PAD_BODY)
    center(img, font_text(label, WHITE), -1)
    return img


def lines_icon(w=16):
    img = Image.new("RGBA", (w, H))
    d = ImageDraw.Draw(img)
    d.rounded_rectangle((0, 1, w - 1, H - 1), 5, fill=PAD_EDGE)
    d.rounded_rectangle((1, 2, w - 2, H - 3), 4, fill=PAD_BODY)
    for y in (5, 7, 9):
        d.line((4, y, w - 5, y), fill=WHITE)
    return img


def stick(letter):
    img, d = round_button(PAD_BODY)
    d.ellipse((3, 2, 12, 11), outline=(120, 116, 140, 255))
    center(img, font_text(letter, WHITE), -1)
    return img


def dpad(direction):
    img = Image.new("RGBA", (H, H))
    d = ImageDraw.Draw(img)
    d.rectangle((5, 0, 10, 15), fill=PAD_EDGE)
    d.rectangle((0, 5, 15, 10), fill=PAD_EDGE)
    d.rectangle((6, 1, 9, 14), fill=PAD_BODY)
    d.rectangle((1, 6, 14, 9), fill=PAD_BODY)
    hot = (250, 216, 74, 255)
    arm = {"right": (11, 6, 14, 9), "left": (1, 6, 4, 9), "up": (6, 1, 9, 4), "down": (6, 11, 9, 14)}[direction]
    d.rectangle(arm, fill=hot)
    return img


def key(label, w=None):
    text = font_text(label, INK) if label else None
    w = w or max(H, text.width + 8)
    img = Image.new("RGBA", (w, H))
    d = ImageDraw.Draw(img)
    d.rounded_rectangle((0, 0, w - 1, H - 1), 3, fill=INK)
    d.rounded_rectangle((1, 1, w - 2, H - 2), 2, fill=KEY_SIDE)
    d.rounded_rectangle((1, 1, w - 2, H - 4), 2, fill=KEY_FACE)
    if text:
        img.alpha_composite(text, ((w - text.width) // 2, (H - 3 - text.height) // 2))
    return img


def space_key():
    img = key("", 28)
    d = ImageDraw.Draw(img)
    d.line((8, 5, 8, 8), fill=INK)
    d.line((8, 8, 19, 8), fill=INK)
    d.line((19, 5, 19, 8), fill=INK)
    return img


def mouse(button):
    img = Image.new("RGBA", (12, H))
    d = ImageDraw.Draw(img)
    d.rounded_rectangle((0, 0, 11, 15), 5, fill=INK)
    d.rounded_rectangle((1, 1, 10, 14), 4, fill=KEY_FACE)
    d.line((1, 6, 10, 6), fill=INK)
    d.line((5, 1, 5, 6), fill=INK)
    hot = (250, 216, 74, 255)
    if button == "left":
        d.rectangle((2, 2, 4, 5), fill=hot)
    elif button == "right":
        d.rectangle((6, 2, 9, 5), fill=hot)
    return img


ICONS = {
    # Xbox
    "xb_a": xbox("A", (88, 178, 64, 255)),
    "xb_b": xbox("B", (214, 64, 64, 255)),
    "xb_x": xbox("X", (60, 120, 222, 255)),
    "xb_y": xbox("Y", (228, 186, 40, 255)),
    "xb_lb": pill("LB"), "xb_rb": pill("RB"), "xb_lt": pill("LT"), "xb_rt": pill("RT"),
    "xb_menu": lines_icon(),
    # PlayStation
    "ps_cross": ps("cross", (124, 170, 240, 255)),
    "ps_circle": ps("circle", (240, 100, 112, 255)),
    "ps_square": ps("square", (226, 140, 220, 255)),
    "ps_triangle": ps("triangle", (70, 210, 180, 255)),
    "ps_l1": pill("L1"), "ps_r1": pill("R1"), "ps_l2": pill("L2"), "ps_r2": pill("R2"),
    "ps_options": lines_icon(),
    # comunes de mando
    "ls": stick("L"), "rs": stick("R"),
    "dpad_right": dpad("right"),
    # teclado y ratón
    "key_e": key("E"), "key_q": key("Q"), "key_2": key("2"), "key_esc": key("ESC"),
    "key_shift": key("SHIFT"), "key_enter": key("ENTER"), "key_wasd": key("WASD"),
    "key_space": space_key(),
    "mouse_left": mouse("left"), "mouse": mouse(None),
}

if __name__ == "__main__":
    width = sum(i.width + PAD for i in ICONS.values()) + PAD
    atlas = Image.new("RGBA", (width, H + PAD * 2))
    rects, x = {}, PAD
    for name, img in ICONS.items():
        atlas.alpha_composite(img, (x, PAD))
        # y desde abajo (como Unity)
        rects[name] = {"x": x, "y": PAD, "w": img.width, "h": img.height}
        x += img.width + PAD
    atlas.save(OUT + ".png")
    json.dump({"height": atlas.height, "icons": rects}, open(OUT + ".json", "w"), indent=1)
    print(f"{len(rects)} íconos, atlas {atlas.size}")
