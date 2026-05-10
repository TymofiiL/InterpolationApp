# Розв'язання задач інтерполяції

Курсова робота з дисципліни **«Основи програмування»**  
КПІ ім. Ігоря Сікорського · Кафедра ІПІ · 2026

Програмний продукт реалізує три методи поліноміальної інтерполяції з графічним
інтерфейсом на базі **.NET 7 MAUI** (Windows та macOS).

---

## Реалізовані методи

| Метод | Складність (обчислення) | Особливості |
|---|---|---|
| Класичний метод Лагранжа | O(n²) | Будує явний аналітичний вираз |
| Барицентрична форма Лагранжа | O(n²) ініціалізація + **O(n)** оцінка | Найшвидший при повторних обчисленнях |
| Схема Ейткена | O(n²) | Рекурентний, без явної побудови полінома |

---

## Вимоги

### Спільні для всіх платформ

| Компонент | Версія |
|---|---|
| .NET SDK | **7.0** або новіший |
| MAUI Workload | встановлений |

### Windows

| Компонент | Версія |
|---|---|
| Windows | 10 версія 1809+ (build 17763+) |
| Visual Studio 2022 | 17.6+ з робочим навантаженням **.NET MAUI** (альтернативно) |

### macOS

| Компонент | Версія |
|---|---|
| macOS | 13.3 (Ventura) або новіший |
| Xcode | 14+ з командними інструментами (`xcode-select --install`) |

---

## Встановлення MAUI Workload

На будь-якій платформі виконайте одну команду:

```bash
dotnet workload install maui
```

На macOS може знадобитись `sudo`:

```bash
sudo dotnet workload install maui
```

---

## Збірка та запуск

Проєкт автоматично визначає версію встановленого .NET SDK і додає відповідні
цільові платформи:

| Встановлений SDK | Доступні цільові платформи |
|---|---|
| .NET 7 | `net7.0-maccatalyst`, `net7.0-windows10.0.19041.0` |
| .NET 8+ | обидві платформи net7 **та** net8 |

Оскільки проєкт може містити кілька Mac-цілей, прапорець `-f` є обов'язковим.

### Клонування репозиторію

```bash
git clone https://github.com/<your-username>/InterpolationApp.git
cd InterpolationApp
dotnet restore
```

### Windows

```bash
# .NET 7 SDK
dotnet run -f net7.0-windows10.0.19041.0

# .NET 8 SDK
dotnet run -f net8.0-windows10.0.19041.0
```

Або відкрити рішення у **Visual Studio 2022** і натиснути **F5**.

### macOS

```bash
# .NET 7 SDK
dotnet build -f net7.0-maccatalyst
open bin/Debug/net7.0-maccatalyst/InterpolationApp.app

# .NET 8 SDK
dotnet build -f net8.0-maccatalyst
open bin/Debug/net8.0-maccatalyst/InterpolationApp.app
```

> На macOS застосунок підписується ad-hoc (без облікового запису Apple Developer),
> тому під час першого запуску може з'явитись діалог системи безпеки.
> Щоб дозволити запуск: **Системні параметри → Конфіденційність і безпека → Все одно відкрити**.

---

## Структура проєкту

```
InterpolationApp/
├── Models/               # InterpolationData, NodeItem, PlotSeries,
│                         # ValidationResult, ComputeResultItem
├── Interpolators/        # InterpolatorBase, LagrangeInterpolator,
│                         # BarycentricInterpolator, AitkenInterpolator
├── Services/             # DataManager, ComplexityAnalyzer,
│                         # InterpolationStateService, SvgExporter
├── ViewModels/           # ComputeViewModel, GraphViewModel, ComplexityViewModel
├── Views/                # ComputePage, GraphPage, ComplexityPage (XAML + CS)
├── Controls/             # ChartDrawable, ComplexityDrawable (IDrawable)
├── Converters/           # InvertBool, StringNotEmpty, IntNotZero
├── Messages/             # WeakReferenceMessenger message records
├── Platforms/
│   ├── MacCatalyst/      # AppDelegate, Program (точка входу macOS)
│   └── Windows/          # App.xaml (точка входу Windows)
└── Resources/            # App icon, splash, fonts, images
```

---

## Функціонал

### Вкладка «Обчислення»
- Введення вузлів (xᵢ, yᵢ) у таблиці — додавання, редагування, видалення рядків
- Завантаження вузлів із файлу TXT (роздільники: пробіл, кома, крапка з комою, табуляція)
- Вибір одного або кількох методів
- Обчислення P(x) у заданій точці з виведенням результату та часу виконання
- Відображення аналітичного виразу полінома (для методу Лагранжа)

### Вкладка «Графік»
- Побудова графіків поліномів усіх вибраних методів на заданому інтервалі [a, b]
- Відображення вузлових точок на графіку
- Масштабування (кнопки +/−) та скидання виду
- Збереження графіка у формат **SVG** (діалог вибору місця збереження)

### Вкладка «Складність»
- Автоматичний аналіз часу виконання для n = 5, 10, …, 100 вузлів
- Графік залежності часу (мкс) від кількості вузлів
- Таблиця числових результатів
- Збереження графіка складності у формат **SVG** (діалог вибору місця збереження)
- Можливість скасування аналізу

---

## Формат вхідного файлу (TXT)

```
# Коментарі починаються з символу '#'
# Формат: x_i <роздільник> y_i

0    1.0
1    0.841
2    0.909
3    0.141
4   -0.757
```

Підтримувані роздільники: пробіл, табуляція, крапка з комою, кома.  
Десятковий роздільник: крапка або кома.

---

## Ліцензія

MIT — вільне використання для навчальних цілей.
