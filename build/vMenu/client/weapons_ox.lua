--[[
    ox_inventory weapon integration (client side).

    These exports are called from the vMenu C# weapon spawner in place of the native GiveWeaponToPed /
    RemoveWeaponFromPed / component / ammo natives. Mutations are server-authoritative (ox_inventory
    AddItem/RemoveItem/SetMetadata must run on the server), so each export forwards to a server event.
    Reads (does the player currently carry the weapon item) use ox_inventory's client Search export.
]]

---@param spawnName string
---@param tint integer
exports('oxGiveWeapon', function(spawnName, tint)
    TriggerServerEvent('vMenu:ox:giveWeapon', spawnName, tint or 0)
end)

--- Adds the weapon if the player doesn't carry it, otherwise removes it (server decides).
---@param spawnName string
---@param tint integer
exports('oxToggleWeapon', function(spawnName, tint)
    TriggerServerEvent('vMenu:ox:toggleWeapon', spawnName, tint or 0)
end)

---@param spawnName string
exports('oxRemoveWeapon', function(spawnName)
    TriggerServerEvent('vMenu:ox:removeWeapon', spawnName)
end)

exports('oxGiveAllWeapons', function()
    TriggerServerEvent('vMenu:ox:giveAllWeapons')
end)

exports('oxRemoveAllWeapons', function()
    TriggerServerEvent('vMenu:ox:removeAllWeapons')
end)

---@param spawnName string
exports('oxRefillAmmo', function(spawnName)
    TriggerServerEvent('vMenu:ox:refillAmmo', spawnName)
end)

---@param count integer
exports('oxSetAllAmmo', function(count)
    TriggerServerEvent('vMenu:ox:setAllAmmo', count)
end)

---@param spawnName string
---@param componentHash integer
exports('oxToggleComponent', function(spawnName, componentHash)
    TriggerServerEvent('vMenu:ox:toggleComponent', spawnName, componentHash)
end)

---@param spawnName string
---@param tint integer
exports('oxSetTint', function(spawnName, tint)
    TriggerServerEvent('vMenu:ox:setTint', spawnName, tint)
end)
