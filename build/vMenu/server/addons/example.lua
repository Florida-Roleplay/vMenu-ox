-- Example server-side addon (pairs with addons/example.lua). Standard FiveM Lua — no special API.
RegisterNetEvent("vmenu_addons:giveCash")
AddEventHandler("vmenu_addons:giveCash", function(amount)
    local src = source
    -- Gate with ACE if you want (grant with: add_ace group.admin vMenu.Addons.Cash allow):
    -- if not IsPlayerAceAllowed(src, "vMenu.Addons.Cash") then return end

    -- Give the money using your framework here, e.g. ESX/QBCore. Placeholder:
    print(("[addon] player %s requested $%s"):format(GetPlayerName(src) or src, amount))
end)
