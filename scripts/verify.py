"""Run after dotnet build. Uses only the Python standard library."""

import os
import sqlite3
from contextlib import closing
from datetime import date
from pathlib import Path
import subprocess
import sys
from tempfile import TemporaryDirectory


ROOT = Path(__file__).resolve().parent.parent
DLL = ROOT / "EmployeeNotebook" / "bin" / "Debug" / "net8.0" / "EmployeeNotebook.dll"
DOTNET = sys.argv[1] if len(sys.argv) > 1 else "dotnet"


def run(args, lines=(), cwd=None):
    result = subprocess.run(
        [DOTNET, str(DLL), *args],
        input="\n".join(lines) + "\n" if lines else "",
        encoding="utf-8",
        capture_output=True,
        timeout=30,
        check=True,
        cwd=cwd,
        env={**os.environ, "EMPLOYEE_NOTEBOOK_ANSI": "0"},
    )
    return result.stdout.replace("\u00a0", " ").replace("\u202f", " ")


def expect(output, *messages):
    for message in messages:
        if message not in output:
            raise AssertionError(f"Не найдено: {message!r}\n{output}")


def main():
    if not DLL.is_file():
        raise SystemExit("Сначала запустите dotnet build из корня проекта.")

    for stage, expected in (
        ("4", "UPDATE и DELETE: проверки пройдены"),
        ("5", "В базу успешно загружено 12 сотрудников"),
        ("6-pay", "63750.00 руб."),
        ("6-validation", "Карточка отклонена"),
        ("7", "Поиск «иванов» + ИТ + «Работает»: найдено 2"),
    ):
        expect(run(["--demo", stage]), expected)
        print(f"Проверка этапа {stage}: пройдена")

    with TemporaryDirectory(prefix="employee-notebook-e2e-") as temporary:
        db = Path(temporary) / "employees.db"
        options = ["--db", str(db)]
        first = run(options, ["0"])
        expect(first, "Отображено записей: 12 из 12", "ТН-0001")
        if not db.is_file():
            raise AssertionError("SQLite не создал файл базы")
        print("Сценарий 1: первый запуск, в SQLite 12 сотрудников")

        filtered = run(options, ["4", "Иванов", "ИТ", "Все", "0"])
        expect(filtered, "Поиск: Иванов | Отдел: ИТ", "Отображено записей: 2 из 12")
        filtered_screen = filtered.split("Отображено записей: 2 из 12", 1)[1]
        expect(filtered_screen, "Иванов Алексей Петрович", "Иванова Мария Олеговна")
        if "Кузнецов" in filtered_screen:
            raise AssertionError("Фильтр оставил запись с другим ФИО")
        print("Сценарий 2: поиск ФИО и фильтрация по отделу")

        # Имитируем схему из версии до появления образования.
        with closing(sqlite3.connect(db)) as connection, connection:
            connection.execute("ALTER TABLE Employees DROP COLUMN Education")
        migrated = run(options, ["1", "1", "", "0"])
        expect(migrated, "Иванов Алексей Петрович", "Не указано", "Возраст, полных лет",
               "Стаж в организации, полных лет")

        edited = run(options, ["1", "1", "1", *([""] * 12), "1,5", "Высшее", "1", "", "0"])
        expect(edited, "Итого к выплате (Оклад × Ставка): 85 000,00 руб.",
               "Изменения сохранены", "Итого к выплате (Оклад × Ставка): 127 500,00 руб.", "Высшее")
        print("Сценарий 3: изменение ставки с 1,0 на 1,5 и пересчёт выплаты")

        write = run(options, ["2", "ТН-0999", "000-000-999 00", "10.02.1990",
                              "01.09.2020", "Петрова Тестовая Анна", "1", "Специалист",
                              "demo@example.invalid", "SQLite", "Проверка сохранения", "1",
                              "85000", "0,75", "Среднее профессиональное", "1", "", "0"])
        expect(write, "Карточка добавлена: ID 13, ТН-0999", "Выход из записной книжки.")
        reopened = run(options, ["0"])
        expect(reopened, "Отображено записей: 13 из 13", "ТН-0999", "Петрова Тестовая Анна")
        print("Сценарий 4: новый процесс открыл из той же SQLite сохранённую карточку")

        today = date.today()
        age = today.year - 1990 - int((today.month, today.day) < (2, 10))
        tenure = today.year - 2020 - int((today.month, today.day) < (9, 1))
        run(options, ["1", "13", "2", "4", "ТН-0999", "Все", "Все", "6", "0"], cwd=temporary)
        reports = Path(temporary) / "Отчёты"
        card = (reports / "Карточка.html").read_text(encoding="utf-8")
        report = (reports / "Отчёт.html").read_text(encoding="utf-8")
        expect(card, "Среднее профессиональное", f"Возраст, полных лет</th><td>{age}",
               f"Стаж в организации, полных лет</th><td>{tenure}")
        expect(report, "Записей в отчёте: 1", "Петрова Тестовая Анна", "Сводка по отделам")
        if "Иванов" in report:
            raise AssertionError("В отчёт попала карточка вне фильтра")
        print("Проверены обновление старой базы, образование, возраст, стаж и выгрузка HTML")

        delete = run(options, ["3", "13", "1", "0"])
        expect(delete, "Карточка удалена", "Отображено записей: 12 из 12")
        if "ТН-0999" in run(options, ["0"]):
            raise AssertionError("Удалённая карточка появилась снова")
        print("Дополнительно: удаление сохраняется после перезапуска")


if __name__ == "__main__":
    sys.stdout.reconfigure(encoding="utf-8")
    main()
