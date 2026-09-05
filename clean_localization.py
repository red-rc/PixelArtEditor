"""
clean_localization.py
----------------------
Видаляє з en.yaml/uk.yaml ключі, які не використовуються в коді (.cs, .axaml),
та ключі-дублікати за значенням (лишає перший).
Запуск із кореня солюшена: python clean_localization.py
"""

import os
import re

_SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
YAML_FILES = [
    os.path.join(_SCRIPT_DIR, "PixelArtEditor", "Localization", "en.yaml"),
    os.path.join(_SCRIPT_DIR, "PixelArtEditor", "Localization", "uk.yaml"),
]
SCAN_EXTENSIONS = (".cs", ".axaml")
SKIP_DIRS = {"bin", "obj", ".git"}
DRY_RUN = False


def load_yaml_simple(path):
    keys_order = []
    data = {}
    if not os.path.exists(path):
        return keys_order, data
    with open(path, encoding="utf-8") as f:
        for line in f:
            stripped = line.rstrip("\n")
            if ": " in stripped and not stripped.startswith(" "):
                k, _, v = stripped.partition(": ")
                k = k.strip()
                v = v.strip()
                keys_order.append(k)
                data[k] = v
    return keys_order, data


def write_yaml_simple(path, keys_order, data):
    with open(path, "w", encoding="utf-8") as f:
        for k in keys_order:
            f.write(f"{k}: {data[k]}\n")


def collect_all_source_text():
    text_chunks = []
    for root, dirs, files in os.walk(_SCRIPT_DIR):
        dirs[:] = [d for d in dirs if d not in SKIP_DIRS]
        for fname in files:
            if fname.endswith(SCAN_EXTENSIONS):
                path = os.path.join(root, fname)
                try:
                    with open(path, encoding="utf-8") as f:
                        text_chunks.append(f.read())
                except Exception:
                    pass
    return "\n".join(text_chunks)


def find_used_keys(all_text, keys):
    used = set()
    for key in keys:
        # DynamicResource Key}, StaticResource Key}, Get("Key"), ["Key"], Resources[key]
        pattern = re.compile(
            r'(?:DynamicResource\s+' + re.escape(key) + r'\b'
            r'|StaticResource\s+' + re.escape(key) + r'\b'
            r'|["\']' + re.escape(key) + r'["\'])'
        )
        if pattern.search(all_text):
            used.add(key)
    return used


def main():
    all_text = collect_all_source_text()

    # 1. Використовуємо перший файл як референс для порядку/повноти ключів
    ref_keys, _ = load_yaml_simple(YAML_FILES[0])

    used_keys = find_used_keys(all_text, ref_keys)
    unused_keys = [k for k in ref_keys if k not in used_keys]

    print(f"Невикористаних ключів: {len(unused_keys)}")
    for k in unused_keys:
        print(f"  - {k}")

    # 2. Дублікати за значенням (по референсному файлу), лишаємо перший
    seen_values = {}
    duplicate_keys = []
    for k in ref_keys:
        if k in unused_keys:
            continue
        v = dict(zip(ref_keys, [None] * len(ref_keys)))  # placeholder, replaced below

    ref_keys2, ref_data = load_yaml_simple(YAML_FILES[0])
    seen_values = {}
    duplicate_keys = []
    for k in ref_keys2:
        if k in unused_keys:
            continue
        v = ref_data[k]
        if v in seen_values:
            duplicate_keys.append(k)
        else:
            seen_values[v] = k

    print(f"\nДублікатів за значенням: {len(duplicate_keys)}")
    for k in duplicate_keys:
        print(f"  - {k} -> '{ref_data[k]}' (дублює '{seen_values[ref_data[k]]}')")

    keys_to_remove = set(unused_keys) | set(duplicate_keys)

    if DRY_RUN:
        print("\n[dry-run] Зміни НЕ застосовано.")
        return

    for path in YAML_FILES:
        keys_order, data = load_yaml_simple(path)
        new_keys_order = [k for k in keys_order if k not in keys_to_remove]
        new_data = {k: data[k] for k in new_keys_order}
        write_yaml_simple(path, new_keys_order, new_data)
        print(f"\nОновлено {path}: {len(keys_order)} -> {len(new_keys_order)} ключів")


if __name__ == "__main__":
    main()