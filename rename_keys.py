"""
rename_keys.py
--------------
Замінює старі ключі DynamicResource на нові у всіх .axaml файлах проєкту.
Поклади поруч з .sln і запусти: python rename_keys.py
"""

import os
import re

_SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))

# ─── Таблиця замін: старий ключ → новий ключ ─────────────────────────────────
RENAMES = {
    "TextBlockDpixY":      "ImgPropDpiXY",
    "TextBlockColorMode":  "ImgPropColorMode",
    "TextBlockBitDepth":   "ImgPropBitDepth",
    "TextBlockAlphaFormat":"ImgPropAlphaFormat",
    "TextBlockColorSpaces":"ImgPropColorSpaces",
    "TextBlockBigEndian":  "ImgPropBigEndian",
    "TextBlockV100":       "AppVersion",
    "TextBlockBasic":      "CrWinBasic",
    "TextBlockAdvanced":   "CrWinAdvanced",
    "TextBlockAdvanced2":  "CrWinAdvanced",
    "ButtonConfirm":       "ExportConfirm",
    "TextBlockSize":       "ImgPropSize",
}
# ─────────────────────────────────────────────────────────────────────────────

def process_file(path: str) -> int:
    with open(path, encoding="utf-8") as f:
        content = f.read()

    new_content = content
    count = 0
    for old, new in RENAMES.items():
        pattern = re.compile(re.escape(old))
        occurrences = len(pattern.findall(new_content))
        if occurrences:
            new_content = pattern.sub(new, new_content)
            count += occurrences
            print(f"  {path}: {old} → {new} ({occurrences}x)")

    if count:
        with open(path, "w", encoding="utf-8") as f:
            f.write(new_content)

    return count

def main():
    total = 0
    for root, dirs, files in os.walk(_SCRIPT_DIR):
        dirs[:] = [d for d in dirs if d not in ("bin", "obj", ".git")]
        for fname in files:
            if fname.endswith(".axaml"):
                total += process_file(os.path.join(root, fname))

    print(f"\nГотово. Всього замін: {total}")

if __name__ == "__main__":
    main()
