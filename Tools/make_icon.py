"""Genera el ícono de la app en Assets/UI/Logo/ con On y Off:
icon.png (escritorio, iOS y Android clásico) e icon_fg.png + icon_bg.png (Android adaptativo).
Uso: python Tools/make_icon.py   (desde la raíz del proyecto)
"""
from PIL import Image, ImageDraw

A = "Assets/"
PURPLE = (124, 58, 237, 255)
DARK = (46, 22, 102, 255)


def mascot(path):
    im = Image.open(A + path).convert("RGBA").crop((0, 0, 48, 48))
    return im.crop(im.getbbox())


def mascots(size):
    """On y Off lado a lado, centrados en un lienzo transparente de size x size."""
    on, off = mascot("Characters/Sprites/On/on_full.png"), mascot("Characters/Sprites/Off/off_full.png")
    out = Image.new("RGBA", (size, size))
    w, h = on.width + off.width, max(on.height, off.height)
    x, y = (size - w) // 2, (size - h) // 2 + 1
    out.paste(on, (x, y + h - on.height), on)
    out.paste(off, (x + on.width, y + h - off.height), off)
    return out


def background(size, radius=0):
    """Morado en escalones hacia abajo; con radius, esquinas redondeadas transparentes."""
    bg = Image.new("RGBA", (size, size))
    d = ImageDraw.Draw(bg)
    steps = 8
    for i in range(steps):
        t = i / (steps - 1)
        c = tuple(int(PURPLE[k] + (DARK[k] - PURPLE[k]) * t) for k in range(3)) + (255,)
        d.rectangle((0, i * size // steps, size - 1, (i + 1) * size // steps - 1), fill=c)
    if radius:
        mask = Image.new("L", (size, size), 0)
        ImageDraw.Draw(mask).rounded_rectangle((0, 0, size - 1, size - 1), radius=radius, fill=255)
        bg = Image.composite(bg, Image.new("RGBA", (size, size)), mask)
    return bg


def save(img, name, scale):
    img.resize((img.width * scale, img.height * scale), Image.NEAREST).save(A + f"UI/Logo/{name}.png")


def main():
    # escritorio / iOS / Android clásico: 64 px de arte x16 = 1024
    icon = background(64, radius=12)
    m = mascots(64)
    icon.paste(m, (0, 0), m)
    save(icon, "icon", 16)

    # Android adaptativo: el sistema recorta la forma y solo el 66 % central es seguro,
    # así que las mascotas (~64 px) van en un lienzo de 96: 96 x11 = 1056
    save(background(96), "icon_bg", 11)
    save(mascots(96), "icon_fg", 11)

    assert Image.open(A + "UI/Logo/icon.png").size == (1024, 1024)
    assert Image.open(A + "UI/Logo/icon_fg.png").getbbox() is not None
    print("icon ok")


if __name__ == "__main__":
    main()
