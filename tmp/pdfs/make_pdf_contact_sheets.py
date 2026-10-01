from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

src = Path(r"D:\WorkSpace\JayTom\JayTom.Dws\tmp\pdfs\wxkc_bridge_qa_v3")
files = sorted(src.glob("page-*.png"), key=lambda p: int(p.stem.split("-")[-1]))
out = src / "contacts"
out.mkdir(exist_ok=True)
font = ImageFont.truetype(r"C:\Windows\Fonts\CascadiaMono.ttf", 22)

cols, rows = 4, 4
thumb_w, thumb_h = 420, 297
label_h, gutter = 34, 18
sheet_w = cols * thumb_w + (cols + 1) * gutter
sheet_h = rows * (thumb_h + label_h) + (rows + 1) * gutter

for batch in range(0, len(files), cols * rows):
    sheet = Image.new("RGB", (sheet_w, sheet_h), "#dbe2ec")
    draw = ImageDraw.Draw(sheet)
    for slot, path in enumerate(files[batch:batch + cols * rows]):
        page_no = batch + slot + 1
        row, col = divmod(slot, cols)
        x = gutter + col * thumb_w + col * gutter
        y = gutter + row * (thumb_h + label_h) + row * gutter
        with Image.open(path) as im:
            thumb = im.convert("RGB")
            thumb.thumbnail((thumb_w, thumb_h), Image.Resampling.LANCZOS)
            px = x + (thumb_w - thumb.width) // 2
            py = y + label_h + (thumb_h - thumb.height) // 2
            sheet.paste(thumb, (px, py))
            draw.rectangle((px, py, px + thumb.width - 1, py + thumb.height - 1), outline="#7f8da3", width=1)
        draw.text((x, y), f"PAGE {page_no:02d}", font=font, fill="#12233f")
    target = out / f"contact-{batch // (cols * rows) + 1}.png"
    sheet.save(target, optimize=True)
    print(target)
