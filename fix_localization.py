"""
fix_localization.py
-------------------
Знаходить хардкодний Text="..." у всіх .axaml файлах проєкту,
пропонує ключ для DynamicResource, замінює в AXAML і додає до en.yaml.

Запуск із кореня солюшена:
    python fix_localization.py

Параметри (змінюй прямо тут):
"""

import re
import os
import sys

# Папка де лежить сам скрипт — використовуємо як корінь проєкту
_SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))

# ─── Налаштування ────────────────────────────────────────────────────────────

# Папка, де шукати .axaml (рекурсивно) — корінь солюшена
SEARCH_ROOT = _SCRIPT_DIR

# Шлях до файлу локалізації
YAML_PATH = os.path.join(_SCRIPT_DIR, "PixelArtEditor", "Localization", "en.yaml")

# Елементи, Text= яких НЕ чіпаємо (наприклад MenuItem — у тебе вже є ключі)
SKIP_PARENT_TAGS = {"MenuItem"}

# Якщо значення вже є DynamicResource / Binding / x:Static — пропускаємо
SKIP_VALUE_PREFIXES = ("{", "avares://")

# Чи писати зміни на диск (False = dry-run, тільки показати)
APPLY_CHANGES = True

# ─────────────────────────────────────────────────────────────────────────────

# Регексп: Text="щось" де "щось" не починається з { або пробілу
# Беремо тільки TextBlock, Label, Button Content, але НЕ MenuItem
HARDCODED_TEXT_RE = re.compile(
    r'(?P<attr>(?:Text|Content|Header))\s*=\s*"(?P<val>[^"{][^"]*)"'
)

# Регексп для імені найближчого батьківського тегу (для генерації ключа)
TAG_NAME_RE = re.compile(r"<\s*([A-Za-z][A-Za-z0-9.:]*)")


def load_yaml_simple(path: str) -> dict[str, str]:
    """Мінімальний парсер: читає key: value рядки (без вкладеності)."""
    result: dict[str, str] = {}
    if not os.path.exists(path):
        return result
    with open(path, encoding="utf-8") as f:
        for line in f:
            line = line.rstrip("\n")
            if ": " in line and not line.startswith(" "):
                k, _, v = line.partition(": ")
                # прибираємо yaml-лапки якщо є
                v = v.strip().strip('"').strip("'")
                # multi-line scalar (>- тощо) — просто зберігаємо як є
                result[k.strip()] = v
    return result


def append_yaml(path: str, key: str, value: str):
    """Дописує новий ключ в кінець yaml-файлу."""
    with open(path, "a", encoding="utf-8") as f:
        # якщо значення містить \n — використовуємо block scalar
        if "\n" in value:
            safe = value.replace("\n", "\n  ")
            f.write(f"{key}: >-\n  {safe}\n")
        else:
            f.write(f"{key}: {value}\n")


def to_pascal(s: str) -> str:
    """'hello world' → 'HelloWorld'"""
    return "".join(w.capitalize() for w in re.split(r"[\s_\-]+", s) if w)


def make_key(tag: str, value: str, existing: set[str]) -> str:
    """
    Генерує унікальний ключ виду <Tag><Value>.
    Приклад: TextBlock + "Size" → "TextBlockSize"
    """
    # скорочуємо тег до останнього компонента (ui|InstantToggleButton → InstantToggleButton)
    tag_short = tag.split("|")[-1].split(".")[-1]
    # перші 3 слова значення
    words = re.split(r"\s+", value.strip())[:4]
    val_part = to_pascal(" ".join(words))
    # прибираємо не-ASCII символи
    val_part = re.sub(r"[^\w]", "", val_part)

    base = tag_short + val_part
    key = base
    i = 2
    while key in existing:
        key = f"{base}{i}"
        i += 1
    return key


def find_tag_before(text: str, match_start: int) -> str:
    """Знаходить ім'я останнього відкритого тегу перед позицією."""
    snippet = text[:match_start]
    tags = TAG_NAME_RE.findall(snippet)
    # фільтруємо закриваючі
    open_tags = [t for t in tags if not t.startswith("/")]
    return open_tags[-1] if open_tags else "Unknown"


def process_file(
    axaml_path: str,
    yaml_entries: dict[str, str],
    new_entries: dict[str, str],
    apply: bool,
) -> int:
    with open(axaml_path, encoding="utf-8") as f:
        original = f.read()

    result = original
    replacements = 0
    offset = 0  # зміщення після замін

    for m in HARDCODED_TEXT_RE.finditer(original):
        val = m.group("val")
        attr = m.group("attr")

        # пропускаємо пусті рядки та значення що вже є ресурсами
        if not val.strip():
            continue
        if any(val.startswith(p) for p in SKIP_VALUE_PREFIXES):
            continue
        # пропускаємо чисто числові значення (наприклад Width="40")
        if val.strip().lstrip("-").replace(".", "").isdigit():
            continue

        tag = find_tag_before(original, m.start())
        if tag in SKIP_PARENT_TAGS:
            continue

        all_keys = set(yaml_entries) | set(new_entries)
        key = make_key(tag, val, all_keys)

        new_attr_val = f'{attr}="{{DynamicResource {key}}}"'
        old_attr_val = m.group(0)

        # замінюємо в результуючому рядку з урахуванням зміщення
        start = m.start() + offset
        end = m.end() + offset
        result = result[:start] + new_attr_val + result[end:]
        offset += len(new_attr_val) - len(old_attr_val)

        new_entries[key] = val
        replacements += 1

        print(f"  [{axaml_path}] {attr}=\"{val}\"  →  {{DynamicResource {key}}}")

    if replacements and apply:
        with open(axaml_path, "w", encoding="utf-8") as f:
            f.write(result)

    return replacements


def main():
    yaml_entries = load_yaml_simple(YAML_PATH)
    print(f"Завантажено {len(yaml_entries)} ключів з {YAML_PATH}\n")

    new_entries: dict[str, str] = {}
    total = 0

    for root, dirs, files in os.walk(SEARCH_ROOT):
        # пропускаємо bin/obj
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
            print(f"\n✓ Збережено в {YAML_PATH}")
        else:
            print("\n[dry-run] Зміни НЕ збережено (APPLY_CHANGES = False)")


if __name__ == "__main__":
    main()
