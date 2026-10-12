from pathlib import Path
from PIL import Image

input_folder = Path(input("Введіть шлях до папки з PNG: ").strip())
output_folder = input_folder.parent / f"{input_folder.name}_converted"

output_folder.mkdir(exist_ok=True)

old_color = (239, 239, 239)  # efefef
new_color = (27, 27, 27)     # 1b1b1b

for png_file in input_folder.glob("*.png"):
    img = Image.open(png_file).convert("RGBA")
    pixels = img.load()

    for y in range(img.height):
        for x in range(img.width):
            r, g, b, a = pixels[x, y]

            if (r, g, b) == old_color:
                pixels[x, y] = (*new_color, a)

    img.save(output_folder / png_file.name)

print(f"Готово. Результат збережено в: {output_folder}")
input("Натисніть Enter для виходу...")