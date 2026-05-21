# Repository Manager

A professional WPF desktop application (.NET 8, x64) for managing a native C++ repository
via P/Invoke interop.

---

## Prerequisites

| Tool | Version |
|---|---|
| Windows 10 / 11 | x64 |
| .NET 8 SDK | 8.0.x |
| Visual Studio 2022 | 17.8+ (Community or higher) |

---

## Building

### Option A — Visual Studio (recommended)

1. Open `RepositoryManager.sln`.
2. Set the solution platform to **x64** (not AnyCPU).
3. Select the desired configuration: **Debug** or **Release**.
4. Press **F5** or click **Run**.

### Option B — Command Line

```powershell
cd RepositoryManager
dotnet build RepositoryManager.sln -c Release -r win-x64
```

The EXE will be at:

```
RepositoryManager.GUI\bin\Release\net8.0-windows\RepositoryManager.GUI.exe
```

---

## Running with the Mock (no native DLL required)

The app ships with an in-memory `MockRepositoryService` pre-seeded with three demo
items. This is the default. No DLL is needed.

```csharp
// App.xaml.cs — default (mock)
var service = new MockRepositoryService();
```

---

## Connecting the Real Native DLL

1. Place your compiled `RepositoryNative.dll` (x64) into the `native/` folder.
2. Open `RepositoryManager.GUI/App.xaml.cs` and replace:

   ```csharp
   var service = new MockRepositoryService();
   ```

   with:

   ```csharp
   var service = new NativeRepositoryService();
   ```

3. Rebuild and run.

The DLL's expected exports are documented in `native/README_NATIVE.md`.

---

## Project Structure

```
RepositoryManager/
├── RepositoryManager.sln
│
├── RepositoryManager.Models/        # Domain models (RepositoryItem, ItemType)
│   └── RepositoryItem.cs
│
├── RepositoryManager.Core/          # Interfaces (service contracts)
│   └── Interfaces/
│       └── IRepositoryService.cs
│
├── RepositoryManager.Interop/       # P/Invoke wrapper + native service impl
│   ├── NativeMethods.cs             ← all DllImport declarations
│   └── NativeRepositoryService.cs  ← IRepositoryService → native DLL
│
├── RepositoryManager.Services/      # Pure-managed implementations
│   ├── MockRepositoryService.cs    ← in-memory mock for testing
│   └── ValidationService.cs        ← JSON / XML validation
│
├── RepositoryManager.GUI/           # WPF application
│   ├── App.xaml / App.xaml.cs      ← composition root
│   ├── Themes/
│   │   ├── Dark.xaml               ← dark colour palette + control styles
│   │   └── Light.xaml              ← light colour palette + control styles
│   ├── Views/
│   │   ├── MainWindow.xaml         ← primary UI layout
│   │   └── MainWindow.xaml.cs      ← code-behind (drag-drop wiring only)
│   ├── ViewModels/
│   │   ├── ViewModelBase.cs        ← INotifyPropertyChanged + SetProperty
│   │   └── MainViewModel.cs        ← all UI state and commands
│   ├── Commands/
│   │   └── RelayCommand.cs         ← RelayCommand + AsyncRelayCommand
│   └── Converters/
│       └── Converters.cs           ← value converters for XAML bindings
│
└── native/                          # Drop RepositoryNative.dll here
    └── README_NATIVE.md
```

---

## Architecture — MVVM

```
View (XAML)
  │  binds to
  ▼
ViewModel  ──── commands ────►  IRepositoryService
                                  │
                          ┌───────┴───────┐
                    MockRepository   NativeRepository
                    Service          Service
                                         │  P/Invoke
                                     NativeMethods.cs
                                         │
                                  RepositoryNative.dll
```

- **No business logic in code-behind.** `MainWindow.xaml.cs` contains only
  drag-drop event routing — it immediately delegates to `MainViewModel`.
- **AsyncRelayCommand** keeps the UI thread unblocked during all I/O.
- **ObservableCollection** (`AllItems`, `FilteredItems`, `StatusLog`) drives
  live list updates with zero manual refresh calls.

---

## Features

| Feature | Notes |
|---|---|
| Upload JSON / XML | File dialog or drag-and-drop |
| Validation before upload | Uses `System.Text.Json` + `System.Xml` |
| Searchable item list | Real-time filter via `SearchText` binding |
| Content preview | Monospaced read-only text box |
| Delete with confirmation | `MessageBox` guard before deregister |
| Export to disk | `SaveFileDialog` |
| Statistics panel | Total / JSON / XML counts |
| Dark / Light theme | Toggled at runtime via ResourceDictionary swap |
| Status log | Timestamped, colour-coded (red = error, amber = warning) |
| Busy indicator | Spinner in toolbar; commands auto-disabled during ops |

---

## Interop Notes

All native string marshalling decisions are documented inline in
`RepositoryManager.Interop/NativeMethods.cs`.

Key points:
- Strings **into** the DLL: `[MarshalAs(UnmanagedType.LPStr)]` → UTF-8/ANSI `const char*`.
- Strings **out** of the DLL (`RM_Retrieve`): returned as `IntPtr`, marshalled with
  `Marshal.PtrToStringAnsi`, then freed via `RM_FreeString` in a `finally` block.
- The runtime **never** frees native allocations — only `RM_FreeString` does.
- `AllowUnsafeBlocks` is enabled on the Interop project only.

---

## Switching Themes Programmatically

`MainViewModel.IsDarkTheme` is bound to the toolbar toggle. Setting it swaps
the active `ResourceDictionary` at runtime:

```csharp
Application.Current.Resources.MergedDictionaries.Clear();
Application.Current.Resources.MergedDictionaries.Add(
    new ResourceDictionary { Source = new Uri("Themes/Dark.xaml", UriKind.Relative) });
```

---

## License

MIT — free to use and modify.
