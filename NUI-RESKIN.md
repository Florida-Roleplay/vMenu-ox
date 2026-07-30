# vMenu — NativeUI NUI reskin

vMenu's menus are rendered through the FSRP **NativeUI NUI** instead of MenuAPI's native
`DrawRect`/`DrawSprite`. All of vMenu's feature logic is untouched — only the *rendering* changed.

## How it works

- `MenuAPI/` — vendored MenuAPI **source** (was the `MenuAPI.FiveM` NuGet package), patched:
  - `MenuNui.cs` — serializes the open menu to the NUI's `setMenu`/`setVisible`/`setAccent`
    message format and calls `SendNuiMessage`. Windows items to `MaxItemsOnScreen`.
  - `Menu.cs` `Draw()` — after running button handlers, short-circuits to `MenuNui.Send(this)`
    and returns, skipping all native drawing. Input handling is untouched (keyboard nav still native).
  - `MenuController.cs` `ProcessMenus()` — hides the NUI when all menus close.
- `vMenu/vMenuClient.csproj` — references the vendored `..\MenuAPI\MenuAPI.csproj` (project ref)
  instead of the NuGet package.
- `build/vMenu/nui/index.html` — the NUI page: our menu renderer **merged** with vMenu's original
  `storage.html` (import/export UI). Storage CSS is scoped under `#vmenu-storage`; storage's message
  listener is guarded to ignore our `action` messages so it doesn't pop up every frame.
- `build/vMenu/images/` — `banner.png` + `logo.png` (swap to rebrand).

## Build

Requires the .NET SDK (8) + Python 3.

```
# 1. compile client + server (produces build/vMenu/*.dll incl. patched MenuAPI.dll)
dotnet build vMenu/vMenuClient.csproj -c Release
dotnet build vMenuServer/vMenuServer.csproj -c Release

# 2. (re)generate the NUI page from the current menu build + storage.html
python tools/build_nui.py
```

`tools/build_nui.py` reads the menu UI from `../fsrp-nativeui-nui/nui/index.html` and the images
from `../fsrp-nativeui-nui/images/`. Point those at wherever the FSRP NativeUI build lives.

## Deploy

Copy `build/vMenu/` into your server's `resources/` (rename to `vMenu`), `ensure ox_lib`, then
`ensure vMenu`. The whole `build/vMenu/` folder is the resource.

## Notes / TODO

- Menu side defaults to right (`MenuNui.Side`); accent defaults to FSRP blue
  (`MenuNui.SetAccent(r,g,b)`). Wire these to vMenu config / an export if desired.
- Icon → badge mapping covers LOCK / STAR / WARNING / TICK; other MenuAPI icons render no badge.
- Input is still native keyboard nav (arrows / enter / backspace) — unchanged from vMenu.
- Verified via headless render; still needs an in-game smoke test.
