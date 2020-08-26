# О Electron2D

Electron2D - это простой, легкий, быстрый кросплатформенный 2D игровой движок с открытым исходным кодом написанный на C# (.NET Core 3.1) и работающий под ОС Linux, Windows, Mac OS.

## Установка

Используйте менеджер пакетов [nuget](https://www.nuget.org/) для установки Electron2D.

```bash
dotnet add package electron2d
```

## Быстрый старт

```csharp
using Electron2D;

namespace MyFirstGame
{
    class MyGame
    {
        private static Game _myGame;
    
        public static void Main()
        {
            _myGame = new Game("My First Game on Electron2D");
            _myGame.Play();
        }
    }
}
```

## У Electron2D есть

* **Менеджер сцен** ![100%](https://img.shields.io/badge/100%25-f00FF00)
  * Проигрывание сцены ![100%](https://img.shields.io/badge/100%25-f00FF00)
  * Переход между сценами ![100%](https://img.shields.io/badge/100%25-f00FF00)
  
* **Менеджер ресурсов** ![40%](https://img.shields.io/badge/40%25-FFA500)
  * Загрузка спрайтов ![100%](https://img.shields.io/badge/100%25-f00FF00)
  * Загрузка Sprite Sheets ![100%](https://img.shields.io/badge/100%25-f00FF00)
  * Загрузка True Type шрифтов ![0%](https://img.shields.io/badge/0%25-f00000)
  * Загрузка Bitmap шрифтов ![0%](https://img.shields.io/badge/0%25-f00000)
  * Загрузка аудио ![0%](https://img.shields.io/badge/0%25-f00000)
  
* **Отрисовка спрайтов** ![100%](https://img.shields.io/badge/100%25-f00FF00)
  * Отрисовка спрайта ![100%](https://img.shields.io/badge/100%25-f00FF00)
  * Отрисовка спрайта из Sprite Sheets ![100%](https://img.shields.io/badge/100%25-f00FF00)

* **Отрисовка шрифтов (True Type, Bitmap)** ![0%](https://img.shields.io/badge/0%25-f00000)
  * Отрисовка True Type шрифтов ![0%](https://img.shields.io/badge/0%25-f00000)
  * Отрисовка Bitmap шрифтов ![0%](https://img.shields.io/badge/0%25-f00000)
  
* **Поддержка устройств ввода** ![100%](https://img.shields.io/badge/100%25-f00FF00)
  * Клавиатура ![100%](https://img.shields.io/badge/100%25-f00FF00)
  * Мышь ![100%](https://img.shields.io/badge/100%25-f00FF00)

* **Покадровая анимация** ![0%](https://img.shields.io/badge/0%25-f00000)
  * Менеджер анимаций (Animator) ![0%](https://img.shields.io/badge/0%25-f00000)
  * Анимация ![0%](https://img.shields.io/badge/0%25-f00000)

* **Система частиц** ![0%](https://img.shields.io/badge/0%25-f00000)

* **Система сущностей** (Entity) ![100%](https://img.shields.io/badge/100%25-f00FF00)

![Electron2D ready%](https://img.shields.io/badge/Electron2D%20Ready-48,8%25-FFA500)

## Лицензия
[Apache License, version 2.0](https://www.apache.org/licenses/LICENSE-2.0)
