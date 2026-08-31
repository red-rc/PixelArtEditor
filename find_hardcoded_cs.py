"""
find_hardcoded_cs.py
---------------------
Звіт (без автозаміни) по рядкових літералах у .cs файлах, які виглядають
як текст для користувача (помилки, повідомлення тощо) і НЕ є частиною
LocalizationService.Get(...) виклику.

Запуск із кореня солюшена:
    python find_hardcoded_cs.py

Виводить список: файл:рядок -> "текст". Заміну робиш вручну, бо C#-рядки
часто мають інтерполяцію / конкатенацію, і сліпа автозаміна ризикована.
"""

import re
import os

_SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
SEARCH_ROOT = _SCRIPT_DIR

# Мінімальна довжина рядка-кандидата (символів), щоб відсіяти службові рядки
MIN_LEN = 4

# Рядки, що починаються з великої літери і містять пробіл АБО
# закінчуються на . ! ? — типові UI-повідомлення
STRING_LITERAL_RE = re.compile(r'"([^"\\]*(?:\\.[^"\\]*)*)"')

# Патерни, які треба ігнорувати повністю (шляхи, ключі ресурсів, формати тощо)
IGNORE_PATTERNS = [
    re.compile(r'^[a-z0-9_\-./\\]+$'),          # шляхи, ключі в camelCase/шлях
    re.compile(r'^[A-Za-z]+\.(axaml|json|yaml|png|ico|ttf|dds|svg)$', re.I),
    re.compile(r'^avares://'),
    re.compile(r'^https?://'),
    re.compile(r'^\w+$'),                        # одне слово без пробілів (найчастіше ідентифікатор)
    re.compile(r'^[{}\[\]()<>#%\-_/\\., ]*$'),    # тільки спецсимволи
]

# Рядки всередині виклику LocalizationService.Get("...") — вважаємо вже обробленими
LOCALIZED_CALL_RE = re.compile(r'LocalizationService\.Get\s*\(\s*"([^"]*)"\s*\)')

# Файли/директорії, які пропускаємо повністю (скрипти генерації, автогенерований код)
SKIP_DIR_NAMES = {"bin", "obj", ".git"}
SKIP_FILE_SUFFIXES = (".g.cs", ".Designer.cs", ".axaml.cs")  # axaml.cs теж часто лише InitializeComponent
SKIP_FILE_NAMES = {"LocalizationService.cs"}


def is_probably_user_text(s: str) -> bool:
    if len(s) < MIN_LEN:
        return False
    for pat in IGNORE_PATTERNS:
        if pat.match(s):
            return False
    # виглядає як текст: є пробіл, або закінчується розділовим знаком, або починається з великої літери
    has_space = " " in s
    ends_punct = s.endswith((".", "!", "?", ":"))
    starts_upper = s[:1].isupper()
    return has_space or ends_punct or starts_upper


def scan_file(path):
    with open(path, encoding="utf-8") as f:
        lines = f.readlines()

    already_localized_strings = set()
    for line in lines:
        for m in LOCALIZED_CALL_RE.finditer(line):
            already_localized_strings.add(m.group(1))

    results = []
    for lineno, line in enumerate(lines, start=1):
        stripped = line.strip()
        if stripped.startswith("//"):
            continue
        if "LocalizationService.Get" in line:
            continue  # вже локалізовано

        for m in STRING_LITERAL_RE.finditer(line):
            val = m.group(1)
            if val in already_localized_strings:
                continue
            if is_probably_user_text(val):
                results.append((lineno, val))

    return results


def main():
    total = 0
    for root, dirs, files in os.walk(SEARCH_ROOT):
        dirs[:] = [d for d in dirs if d not in SKIP_DIR_NAMES]
        for fname in files:
            if not fname.endswith(".cs"):
                continue
            if fname.endswith(SKIP_FILE_SUFFIXES):
                continue
            if fname in SKIP_FILE_NAMES:
                continue

            path = os.path.join(root, fname)
            hits = scan_file(path)
            if not hits:
                continue

            rel = os.path.relpath(path, SEARCH_ROOT)
            for lineno, val in hits:
                print(f"{rel}:{lineno}: \"{val}\"")
                total += 1

    print(f"\n{'='*60}")
    print(f"Знайдено кандидатів: {total}")
    print("Це лише звіт. Заміни вручну на LocalizationService.Get(\"KeyName\")")
    print("і додай відповідний ключ у en.yaml + SetDefaults().")


if __name__ == "__main__":
    main()
