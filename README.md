# fsrp-vmenu-v2

FSRP's vMenu — the [vMenu](https://github.com/TomGrobbe/vMenu) admin/player menu, **re-skinned to
render through our custom FSRP NativeUI (NUI)** and made **extensible in Lua**. All of vMenu's
features are intact; the rendering, look, and authoring workflow are what changed.

Based on [vMenu](https://github.com/TomGrobbe/vMenu) by Tom Grobbe and the
[vMenu-ox](https://github.com/DukeOfCheese/vMenu-ox) fork (ox_lib integration).

---

## Install

1. Drop `build/vMenu/` into your server's `resources/` (rename to `vMenu`).
2. `ensure ox_lib` then `ensure vMenu` in `server.cfg`.
3. Open the menu as usual. Configure via `config/`, `permissions.cfg`, and convars (below).

`build/vMenu/` is the ready-to-run resource. To rebuild from source see **Building** below.

---

## What we changed

### 1. Rendering — MenuAPI → FSRP NativeUI (NUI)
vMenu draws through **MenuAPI**; we vendored MenuAPI's source (`MenuAPI/`, patched — replaces the
`MenuAPI.FiveM` NuGet package) and added a bridge (`MenuAPI/MenuNui.cs`) that **disables the native
`DrawRect`/`DrawSprite` rendering and serialises each menu to our NUI** (`SendNuiMessage`). Input
stays native (keyboard nav). The NUI is a Vite + React + TypeScript + Tailwind + Satoshi app,
rendered on vMenu's `ui_page` (merged with vMenu's existing `storage.html`). See `NUI-RESKIN.md`.

- **FSRP store theme** — blue gradient selection (`#4059d6 → #2b42a3`), glass surfaces, Satoshi font,
  configurable accent colour (drives selection, stat bars, checkboxes, counter).
- **Accent synced to fsrp-hud** — `addons/accent_sync.lua` pulls the server-wide accent from
  `exports['fsrp-hud']:GetCurrentAccentColor()` on start and follows the `fsrp-hud:accentColorChanged`
  event live. Falls back to the default accent if fsrp-hud isn't running.
- **GTA text colour codes** (`~r~`, `~g~`, `~b~`, `~h~`, `~s~`, `~n~`, …) parsed to real colours.
- **Menu alignment** left / centre / right — follows vMenu's "Right Align Menu" setting, plus a
  convar override (`setr vmenu_nui_side "center"`).
- Banner is a separate rounded card; no drop-shadows / neon glow; slight rounding.

### 2. Icons
- **50+ icons** with **keyword auto-mapping** — every option gets a fitting icon by its name
  (Armor→shield, Heal→heart, Teleport→pin, Weather→cloud, Repair→wrench, etc.).
- **Weapon category icons** use the real GTA HUD weapon silhouettes (pistol, rifle, shotgun, SMG,
  throwable, melee, heavy, sniper).
- **Configurable** (convars): `setr vmenu_nui_icons_categories "false"` and
  `setr vmenu_nui_icons_items "false"` toggle decorative icons (functional icons like the disabled
  lock always show).

### 3. Ped customization
- **Colour swatches** — hair, highlight, beard, eyebrows, makeup, blush, lipstick, chest hair render
  as our colour-strip using the real GTA palette (`GetHairRgbColor` / `GetMakeupRgbColor`).
- **Texture indicator** — clothing/props show a bold **`Texture X/Y`** at the bottom, updating live.

### 4. Weapon / vehicle stats
Weapon and vehicle stat panels (damage / fire rate / accuracy / range · top speed / accel / braking /
traction) render as our stat bars.

### 5. Addon vehicles / weapons with categories + ACE
`config/addon_vehicles.json` and `config/addon_weapons.json` — group addon vehicles/weapons into
**custom categories**, each with an optional **ACE permission** (resolved server-side). Locked
categories show a lock. Grant with e.g. `add_ace group.admin vMenu.Addons.Emergency allow`.
See `ADDON-CATEGORIES-PLAN.md`.

### 6. Lua extensibility (no C# rebuild) — **new**
Build menu categories/options in **Lua**. Any `.lua` in `addons/` (client) and `server/addons/`
(server) is auto-loaded. Full C# menu-building API is exposed as exports; a wrapper
(`lua/menu_api.lua`) gives a clean Lua API with callbacks. See **`LUA-ADDONS.md`**.

```lua
local menu = vMenu.CreateCategory("Server Extras", "star", "addons")   -- "main" or "addons"
menu:AddButton("Repair Vehicle", "wrench", function() ... end, { description = "…" })
menu:AddCheckbox("God Mode", false, "shield", function(on) SetEntityInvincible(PlayerPedId(), on) end)
menu:AddList("Time", { "Morning", "Noon", "Night" }, 1, "clock", function(i, v) ... end)
local sub = menu:AddSubmenu("Tuning", "wheel"); sub:AddSlider("Power", 0, 10, 5, "speed", print)
```
Example test command: `/vmenu-setaccent` (`addons/setaccent.lua`) opens an ox_lib colour picker and
sets the menu accent live via `exports.vMenu:SetAccent(r, g, b)`.

### 7. Live ACE refresh
Opening the menu re-requests the player's permissions from the server (ACE is evaluated live
server-side) and **rebuilds the menu if they changed** — so granting/revoking a permission applies
without the player rejoining or the resource restarting. Lua-added categories are re-attached.

### 8. Fixes
- Stripped vMenu's `→→→` submenu-arrow labels (our chevron replaces them).
- Navigation **skips spacers/dividers** (selection no longer lands on a divider).
- Spacers render as clean separator dividers.
- Description bar hides when there's no help text.

---

## Config quick reference

| Setting | Where | Effect |
|---|---|---|
| `vmenu_nui_side` | convar | `left` / `center` / `right` menu alignment |
| `vmenu_nui_icons_categories` | convar | `false` hides category-row icons |
| `vmenu_nui_icons_items` | convar | `false` hides per-option icons |
| `config/addon_vehicles.json` | file | addon vehicle categories + optional `ace` |
| `config/addon_weapons.json` | file | addon weapon categories + optional `ace` |
| `addons/*.lua` | folder | your Lua menu addons (client) |
| `server/addons/*.lua` | folder | your Lua addon server logic |

---

## Building

Requires the **.NET SDK** (8) and **Python 3** (for the NUI merge). From the repo root:

```
dotnet build vMenu/vMenuClient.csproj -c Release       # -> build/vMenu/vMenuClient.net.dll (+ MenuAPI.dll)
dotnet build vMenuServer/vMenuServer.csproj -c Release  # -> build/vMenu/vMenuServer.net.dll
python tools/build_nui.py                               # -> build/vMenu/nui/index.html (+ images)
```

The NUI source lives in the FSRP NativeUI project; `tools/build_nui.py` merges the built menu UI with
vMenu's `storage.html`. Lua files (`lua/`, `addons/`, `server/addons/`) are loaded directly.

---

## Credits
- vMenu — Tom Grobbe. vMenu-ox — Gravxd & DukeOfCheese. MenuAPI — Tom Grobbe.
- FSRP NativeUI NUI, Lua bridge, addon system — Florida-Roleplay.
