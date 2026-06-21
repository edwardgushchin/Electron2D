# Electron2D Empty Project

Минимальный проект Electron2D для `0.1.0 Preview` clean baseline.

```powershell
dotnet restore
dotnet run
```

Текущий шаблон проверяет manifest проекта, наличие пустой main scene и минимальную модель C# script class.

`Scripts/MainScene.cs` показывает базовый сценарий `0.1.0 Preview`: пользовательский script наследуется от `Electron2D.Node`, получает `_EnterTree()`/`_Ready()` и обращается к сервисам через `GetTree()` и `RenderingServer`.
