--[[
    Server-side tagging of vMenu Saved Vehicles for ox_inventory vehicle storage.

    vMenu (client) triggers this after spawning a saved vehicle. We set the `svStorageId` statebag on the
    entity SERVER-SIDE so it works with sv_stateBagStrictMode enabled (client writes to entity statebags are
    restricted/blocked under strict mode; server writes are always allowed).

    ox_inventory's fsrp bridge reads this statebag to key the vehicle's trunk/glovebox storage to the saved
    vehicle identity (see ox_inventory/modules/bridge/fsrp/server.lua getOwnedVehicleId).
]]

RegisterNetEvent('vMenu:ox:tagVehicleStorage', function(netId, saveName)
    local src = source
    if type(saveName) ~= 'string' or saveName == '' then return end

    netId = tonumber(netId)
    if not netId then return end

    CreateThread(function()
        local entity
        local tries = 0

        -- The entity may not have replicated to the server yet right after the client created it.
        repeat
            entity = NetworkGetEntityFromNetworkId(netId)
            if entity and entity ~= 0 and DoesEntityExist(entity) then break end
            tries += 1
            Wait(50)
        until tries > 40 -- ~2s

        if not entity or entity == 0 or not DoesEntityExist(entity) then return end
        if GetEntityType(entity) ~= 2 then return end -- must be a vehicle

        -- Light anti-spoof guard: the requester must be near the vehicle they're tagging.
        local ped = GetPlayerPed(src)
        if ped and ped ~= 0 then
            local pcoords = GetEntityCoords(ped)
            local vcoords = GetEntityCoords(entity)
            if #(pcoords - vcoords) > 20.0 then return end
        end

        Entity(entity).state:set('svStorageId', saveName, true)
    end)
end)
