-- Example vMenu Lua addon. Copy/rename this file and edit it — no C# rebuild needed.
-- Any .lua file in this addons/ folder is auto-loaded (see fxmanifest.lua).

-- "addons" = nested under an "Addons" submenu on the main menu. Use "main" to show it directly.
local menu = vMenu.CreateCategory("Server Extras", "star", "addons")

menu:AddButton("Repair Vehicle", "wrench", function()
    local veh = GetVehiclePedIsIn(PlayerPedId(), false)
    if veh ~= 0 then
        SetVehicleFixed(veh)
        SetVehicleDeformationFixed(veh)
        SetVehicleUndriveable(veh, false)
    end
end, { description = "Fully repair the vehicle you're in." })

menu:AddButton("Give $1,000", "cash", function()
    TriggerServerEvent("vmenu_addons:giveCash", 1000)
end)

local godMode = false
menu:AddCheckbox("God Mode", godMode, "shield", function(on)
    godMode = on
    SetEntityInvincible(PlayerPedId(), on)
end)

menu:AddList("Time of Day", { "Morning", "Noon", "Evening", "Night" }, 1, "clock", function(index, value)
    print(("[addon] time selected: %s (#%d)"):format(value, index))
end)

local tuning = menu:AddSubmenu("Tuning", "wheel")
tuning:AddSlider("Engine Power", 0, 10, 5, "speed", function(v)
    print("[addon] engine power:", v)
end)
