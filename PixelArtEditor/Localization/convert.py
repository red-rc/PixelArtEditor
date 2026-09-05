from pathlib import Path

def find_separator(line: str):
    """
    Знаходить двокрапку, яка є роздільником між key та value.

    Двокрапка, перед якою стоїть '\\', вважається частиною key.
    Наприклад:

        ui\\:InstantToggleButtonPen: Pen

    має key:

        ui:InstantToggleButtonPen
    """

    escaped = False

    for i, char in enumerate(line):
        if char == "\\" and not escaped:
            escaped = True
            continue

        if char == ":" and not escaped:
            return i

        escaped = False

    return -1


def parse_localization_file(text: str):
    """
    Парсить ваш YAML-подібний файл локалізації.

    Підтримує:

        Key: Value
        Key: Value:
        ui\\:SomeKey: Value

    а також block scalar:

        Key: >-
          Some text

          More text
    """

    lines = text.splitlines()

    data = {}

    i = 0

    while i < len(lines):
        line = lines[i]

        # Порожні рядки пропускаємо
        if not line.strip():
            i += 1
            continue

        # Коментарі пропускаємо
        if line.lstrip().startswith("#"):
            i += 1
            continue

        separator = find_separator(line)

        if separator == -1:
            print(f"Warning: Could not parse line {i + 1}:")
            print(f"    {line}")
            i += 1
            continue

        key = line[:separator].strip()
        value = line[separator + 1:].strip()

        # Повертаємо escaped ":" у ключі
        key = key.replace("\\:", ":")

        # -----------------------------------------
        # Block scalar: >-
        # -----------------------------------------

        if value in (">-", ">", "|-", "|"):

            block_lines = []
            i += 1

            while i < len(lines):
                next_line = lines[i]

                # Якщо рядок має відступ — він належить значенню
                if next_line.startswith(" ") or next_line.startswith("\t"):
                    block_lines.append(next_line.strip())
                    i += 1
                else:
                    break

            # > і >- означають folded text.
            # Для вашого випадку зробимо порожні рядки
            # реальними переносами, а звичайні — пробілами.

            if value.startswith(">"):
                result = []

                for block_line in block_lines:
                    if block_line == "":
                        result.append("\n")
                    else:
                        if result and result[-1] != "\n":
                            result.append(" ")
                        result.append(block_line)

                value = "".join(result).strip()

            else:
                # Literal block scalar |
                value = "\n".join(block_lines)

            data[key] = value
            continue

        # -----------------------------------------
        # Звичайне значення
        # -----------------------------------------

        # Якщо значення обгорнуте в "..."
        if len(value) >= 2 and value[0] == '"' and value[-1] == '"':
            value = value[1:-1]

        elif len(value) >= 2 and value[0] == "'" and value[-1] == "'":
            value = value[1:-1]

        data[key] = value

        i += 1

    return data


def escape_csharp_string(value: str):
    """
    Екранує рядок для C#.
    """

    return (
        value
        .replace("\\", "\\\\")
        .replace('"', '\\"')
        .replace("\r\n", "\n")
        .replace("\r", "\n")
        .replace("\n", "\\n")
    )


def main():

    # Папка, де знаходиться convert.py
    script_dir = Path(__file__).resolve().parent

    # Шукаємо YAML
    yaml_file = script_dir / "en.yaml"

    if not yaml_file.exists():
        print("en.yaml file not found.")
        return

    print(f"Reading: {yaml_file.name}")

    # Читаємо файл
    with yaml_file.open("r", encoding="utf-8") as file:
        text = file.read()

    # Парсимо
    data = parse_localization_file(text)

    # Формуємо C#
    lines = [
        "var dict = new Dictionary<string, string>",
        "{"
    ]

    for key, value in data.items():

        key = escape_csharp_string(key)
        value = escape_csharp_string(value)

        lines.append(
            f'    ["{key}"] = "{value}",'
        )

    lines.append("};")

    result = "\n".join(lines)

    # Назва вихідного файлу
    output_file = script_dir / "localization_dictionary.txt"

    with output_file.open("w", encoding="utf-8") as file:
        file.write(result)

    print()
    print("Done!")
    print(f"Input : {yaml_file.name}")
    print(f"Output: {output_file.name}")
    print(f"Entries: {len(data)}")


if __name__ == "__main__":
    main()