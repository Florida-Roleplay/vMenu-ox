# vMenu Lua Addons

Build menu categories and options in **Lua** — no C# rebuild. vMenu is a mixed C#+Lua resource; the
C# handles rendering (MenuAPI → NUI) and exposes a menu-building API that Lua drives.

## Where addons live

- **Client menus:** any `.lua` in `addons/` (auto-loaded). Wrapper: `lua/menu_api.lua`.
- **Server logic:** any `.lua` in `server/addons/` (standard FiveM Lua — normal events/exports).

Copy `addons/example.lua` + `server/addons/example.lua` and edit them.

## API

```lua
-- title, icon, placement ("main" = on the main menu, "addons" = under an Addons submenu)
local menu = vMenu.CreateCategory("Server Extras", "star", "addons")

menu:AddButton(label, icon, onSelect, opts)                 -- opts = { description=, rightLabel= }
menu:AddCheckbox(label, checked, icon, onChange, opts)      -- onChange(bool)
menu:AddList(label, items, index1based, icon, onChange, o)  -- onChange(index1based, value)
menu:AddSlider(label, min, max, value, icon, onChange, o)   -- onChange(number)
local sub = menu:AddSubmenu(title, icon)                    -- returns another menu; nest freely
```

Every `Add*` returns an `itemId` you can update live:

```lua
local id = menu:AddButton("Status", "info")
vMenu.SetLabel(id, "Online")          vMenu.SetRightLabel(id, "24")
vMenu.SetEnabled(id, false)           vMenu.SetDescription(id, "…")
vMenu.SetIcon(id, "cash")             vMenu.SetChecked(id, true)
vMenu.SetListItems(id, { "A", "B" }, 1)
```

`icon` is any icon key the menu knows — e.g. `car gun star cash shield heart run pin eye clock cloud
wrench wheel bulb parachute magazine mask shirt pants shoe hat glasses watch chat skull folder person`
(or `nil` for none; unknown names fall back to a keyword guess).

## Example

```lua
local menu = vMenu.CreateCategory("Server Extras", "star", "addons")

menu:AddButton("Repair Vehicle", "wrench", function()
    local v = GetVehiclePedIsIn(PlayerPedId(), false)
    if v ~= 0 then SetVehicleFixed(v) end
end, { description = "Repair the vehicle you're in." })

menu:AddCheckbox("God Mode", false, "shield", function(on)
    SetEntityInvincible(PlayerPedId(), on)
end)

menu:AddButton("Give $1,000", "cash", function()
    TriggerServerEvent("vmenu_addons:giveCash", 1000)
end)
```

```lua
-- server/addons/mine.lua
RegisterNetEvent("vmenu_addons:giveCash", function(amount)
    local src = source
    -- if not IsPlayerAceAllowed(src, "vMenu.Addons.Cash") then return end
    -- give money via your framework
end)
```

## Notes

- Callbacks are dispatched from C# to Lua via the `vMenuExtDispatch` export (handled by the wrapper).
- Categories created before the main menu is ready are queued and attached automatically.
- This is additive — built-in vMenu menus are unaffected; you can grow your own sections over time.
