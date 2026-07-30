RegisterNetEvent("vmenu_addons:giveCash")
AddEventHandler("vmenu_addons:giveCash", function(amount)
    local src = source

    print(("[addon] player %s requested $%s"):format(GetPlayerName(src) or src, amount))
end)
