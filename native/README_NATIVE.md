# Native DLL — Placement Instructions

Place your compiled `RepositoryNative.dll` (x64) in this folder.

The `.csproj` file in `RepositoryManager.GUI` contains an `<ItemGroup>` that copies
it to the build output directory automatically:

```xml
<None Include="..\native\RepositoryNative.dll" Condition="Exists(...)">
  <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
</None>
```

## Expected exports (C-compatible, `extern "C"` in your C++ source)

```cpp
extern "C" {
    __declspec(dllexport) bool  RM_Initialize();
    __declspec(dllexport) bool  RM_Register(const char* name, const char* content, int type);
    __declspec(dllexport) const char* RM_Retrieve(const char* name);
    __declspec(dllexport) int   RM_GetType(const char* name);
    __declspec(dllexport) bool  RM_Deregister(const char* name);
    __declspec(dllexport) bool  RM_Contains(const char* name);
    __declspec(dllexport) void  RM_FreeString(const char* str);
}
```

`RM_Retrieve` must allocate its return string with `malloc` (or `new char[]`).
`RM_FreeString` must free that allocation. The managed layer never calls
`Marshal.FreeHGlobal` on it — only `RM_FreeString` is used.

## Switching from Mock to Native

Open `RepositoryManager.GUI/App.xaml.cs` and swap:

```csharp
// Remove:
var service = new MockRepositoryService();

// Add:
var service = new NativeRepositoryService();
```
