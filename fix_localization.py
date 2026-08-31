"""
fix_localization.py
--------------------
Знаходить хардкодний текст (Text, Content, Header, Tag, PlaceholderText,
ToolTip.Tip) у всіх .axaml файлах проєкту, генерує ключ, замінює на
DynamicResource і дописує в en.yaml.

Запуск із кореня солюшена:
    python fix_localization.py
"""

import re
import os

_SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))

SEARCH_ROOT = _SCRIPT_DIR
YAML_PATH = os.path.join(_SCRIPT_DIR, "PixelArtEditor", "Localization", "en.yaml")

# Тут вказуй теги, чиї атрибути НЕ треба чіпати (напр. MenuItem вже має ключі)
SKIP_PARENT_TAGS = {"MenuItem"}

# Атрибути, які локалізуємо
TARGET_ATTRS = ["Text", "Content", "Header", "PlaceholderText", "ToolTip.Tip"]

SKIP_VALUE_PREFIXES = ("{", "avares://")

APPLY_CHANGES = True

_attrs_pattern = "|".join(re.escape(a) for a in TARGET_ATTRS)
HARDCODED_TEXT_RE = re.compile(
    rf'(?P<attr>{_attrs_pattern})\s*=\s*"(?P<val>[^"{{][^"]*)"'
)

TAG_NAME_RE = re.compile(r"<\s*([A-Za-z][A-Za-z0-9.:]*)")


def load_yaml_simple(path):
    result = {}
    if not os.path.exists(path):
        return result
    with open(path, encoding="utf-8") as f:
        for line in f:
            line = line.rstrip("\n")
            if ": " in line and not line.startswith(" "):
                k, _, v = line.partition(": ")
                v = v.strip().strip('"').strip("'")
                result[k.strip()] = v
    return result


def append_yaml(path, key, value):
    with open(path, "a", encoding="utf-8") as f:
        if "\n" in value:
            safe = value.replace("\n", "\n  ")
            f.write(f"{key}: >-\n  {safe}\n")
        else:
            f.write(f"{key}: {value}\n")


def to_pascal(s):
    return "".join(w.capitalize() for w in re.split(r"[\s_\-]+", s) if w)


def make_key(tag, value, existing):
    tag_short = tag.split("|")[-1].split(".")[-1]
    words = re.split(r"\s+", value.strip())[:4]
    val_part = to_pascal(" ".join(words))
    val_part = re.sub(r"[^\w]", "", val_part)

    base = tag_short + val_part
    key = base
    i = 2
    while key in existing:
        key = f"{base}{i}"
        i += 1
    return key


def find_tag_before(text, match_start):
    snippet = text[:match_start]
    tags = TAG_NAME_RE.findall(snippet)
    open_tags = [t for t in tags if not t.startswith("/")]
    return open_tags[-1] if open_tags else "Unknown"


def process_file(axaml_path, yaml_entries, new_entries, apply):
    with open(axaml_path, encoding="utf-8") as f:
        original = f.read()

    result = original
    replacements = 0
    offset = 0

    for m in HARDCODED_TEXT_RE.finditer(original):
        val = m.group("val")
        attr = m.group("attr")

        if not val.strip():
            continue
        if any(val.startswith(p) for p in SKIP_VALUE_PREFIXES):
            continue
        if val.strip().lstrip("-").replace(".", "").isdigit():
            continue

        tag = find_tag_before(original, m.start())
        if tag in SKIP_PARENT_TAGS:
            continue

        all_keys = set(yaml_entries) | set(new_entries)
        key = make_key(tag, val, all_keys)

        new_attr_val = f'{attr}="{{DynamicResource {key}}}"'
        old_attr_val = m.group(0)

        start = m.start() + offset
        end = m.end() + offset
        result = result[:start] + new_attr_val + result[end:]
        offset += len(new_attr_val) - len(old_attr_val)

        new_entries[key] = val
        replacements += 1

        print(f"  [{axaml_path}] {attr}=\"{val}\"  ->  {{DynamicResource {key}}}")

    if replacements and apply:
        with open(axaml_path, "w", encoding="utf-8") as f:
            f.write(result)

    return replacements


def main():
    yaml_entries = load_yaml_simple(YAML_PATH)
    print(f"Завантажено {len(yaml_entries)} ключів з {YAML_PATH}\n")

    new_entries = {}
    total = 0

    for root, dirs, files in os.walk(SEARCH_ROOT):
        dirs[:] = [d for d in dirs if d not in ("bin", "obj", ".git")]
        for fname in files:
            if not fname.endswith(".axaml"):
                continue
            path = os.path.join(root, fname)
            count = process_file(path, yaml_entries, new_entries, APPLY_CHANGES)
            total += count

    print(f"\n{'='*60}")
    print(f"Всього замін: {total}")
    print(f"Нових ключів: {len(new_entries)}")

    if new_entries:
        print(f"\nДодаємо до {YAML_PATH}:")
        for k, v in new_entries.items():
            print(f"  {k}: {v}")
        if APPLY_CHANGES:
            os.makedirs(os.path.dirname(YAML_PATH) or ".", exist_ok=True)
            for k, v in new_entries.items():
                append_yaml(YAML_PATH, k, v)
            print(f"\nЗбережено в {YAML_PATH}")
        else:
            print("\n[dry-run] Зміни НЕ збережено (APPLY_CHANGES = False)")


if __name__ == "__main__":
    main()
