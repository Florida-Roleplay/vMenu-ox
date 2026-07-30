--[[
    ox_inventory weapon integration (server side).

    The vMenu weapon spawner no longer touches the ped directly (which used the default weapon wheel).
    Instead every action is routed here and applied to the player's ox_inventory as items:
      - weapons        -> item named after the weapon (e.g. WEAPON_PISTOL)
      - ammo           -> native GTA ammo via the weapon's loaded metadata.ammo (no ammo items in inventory)
      - attachments    -> ox component items written into the weapon's metadata.components
      - tint           -> the weapon's metadata.tint

    All GTA-hash <-> ox-item mapping is read from ox_inventory/data/weapons.lua at runtime, so it stays
    in sync with whatever weapons/components/ammo the inventory actually defines.
]]

local DEFAULT_AMMO = GetConvarInt('vmenu:ox_default_ammo', 250)

local oxWeapons = {}          -- [WEAPON_NAME] = { ammoname = 'ammo-9', ... }
local componentHashToItem = {} -- [componentHash] = 'at_suppressor_light'

local ready = false

local function buildMaps()
    local file = LoadResourceFile('ox_inventory', 'data/weapons.lua')

    if not file then
        lib.print.error('[vMenu] ox_inventory/data/weapons.lua not found - weapon integration disabled')
        return
    end

    local chunk, err = load(file, '@ox_inventory/data/weapons.lua')

    if not chunk then
        lib.print.error(('[vMenu] failed to compile ox weapons data: %s'):format(err))
        return
    end

    local ok, data = pcall(chunk)

    if not ok or type(data) ~= 'table' then
        lib.print.error('[vMenu] failed to evaluate ox weapons data')
        return
    end

    oxWeapons = data.Weapons or {}

    local weaponCount = 0
    for _ in pairs(oxWeapons) do weaponCount += 1 end

    local hashCount = 0
    for name, comp in pairs(data.Components or {}) do
        local hashes = comp.client and comp.client.component
        if hashes then
            for i = 1, #hashes do
                componentHashToItem[hashes[i]] = name
                hashCount += 1
            end
        end
    end

    ready = true
    lib.print.info(('[vMenu] ox weapon integration ready (%d weapons, %d component hashes mapped)')
        :format(weaponCount, hashCount))
end

CreateThread(function()
    -- ox_inventory data is available as soon as the resource file exists; small wait for resource start ordering.
    while GetResourceState('ox_inventory') ~= 'started' do Wait(250) end
    buildMaps()
end)

---@param src number
---@param aces string[]
local function isAllowed(src, aces)
    -- Mirror vMenu's own posture: when vMenu permissions aren't enabled (permissions.cfg not exec'd),
    -- vMenu grants everything client-side, so the server must not block either. When permissions ARE
    -- enabled, enforce the matching vMenu ACEs.
    if GetConvar('vmenu_use_permissions', 'false') ~= 'true' then return true end

    if IsPlayerAceAllowed(src, 'vMenu.Everything') then return true end
    if IsPlayerAceAllowed(src, 'vMenu.WeaponOptions.All') then return true end

    for i = 1, #aces do
        if IsPlayerAceAllowed(src, aces[i]) then return true end
    end

    return false
end

---@param spawnName string
---@return string? itemName
local function weaponItemName(spawnName)
    if type(spawnName) ~= 'string' then return end
    local itemName = spawnName:upper()
    if oxWeapons[itemName] then return itemName end
    return nil
end

local function notify(src, description, ntype)
    -- ox_lib:notify is registered net-safe on the client; vMenu:CustomNotify is not accepted over the net.
    TriggerClientEvent('ox_lib:notify', src, { type = ntype or 'inform', description = description })
end

-- Anti-spam: cap how fast a player can conjure weapons. This is what stops the "spawn weapon -> drop
-- on the ground -> respawn -> drop" flood loop. Tunable via convars; backed by fsrp-core's RateLimit
-- (falls back to allowing the action if fsrp-core isn't running so weapons never hard-break).
local SPAWN_MAX = GetConvarInt('vmenu:ox_spawn_max', 6)
local SPAWN_WINDOW = GetConvarInt('vmenu:ox_spawn_window', 8000)
local SPAWN_INTERVAL = GetConvarInt('vmenu:ox_spawn_interval', 500)

---@param src number
---@return boolean allowed
local function spawnAllowed(src)
    local ok, allowed = pcall(function()
        return exports['fsrp-core']:RateLimit(src, 'weapon_spawn', SPAWN_MAX, SPAWN_WINDOW, SPAWN_INTERVAL)
    end)
    if not ok then return true end -- fsrp-core down: don't block weapon spawning outright
    if not allowed then
        notify(src, 'You are spawning weapons too fast — slow down.', 'error')
    end
    return allowed
end

--#region Weapons

RegisterNetEvent('vMenu:ox:giveWeapon', function(spawnName, tint)
    local src = source
    if not ready or not isAllowed(src, { 'vMenu.WeaponOptions.Spawn' }) then return end

    local itemName = weaponItemName(spawnName)
    if not itemName then return notify(src, 'That weapon is not available in the inventory.', 'error') end

    if exports.ox_inventory:GetItemCount(src, itemName) > 0 then
        return notify(src, 'You already have that weapon.', 'error')
    end

    if not spawnAllowed(src) then return end

    local metadata = { ammo = DEFAULT_AMMO, components = {} }
    if type(tint) == 'number' and tint > 0 then metadata.tint = tint end

    -- Native ammo, no inventory bullets: the weapon is given loaded (metadata.ammo) and reloads with the
    -- game's native reserve. No ammo items are added.
    local ok, reason = exports.ox_inventory:AddItem(src, itemName, 1, metadata)
    if ok then
        notify(src, 'Weapon added to your inventory.', 'success')
    else
        lib.print.warn(('[vMenu] AddItem(%s) failed: %s'):format(itemName, reason or 'unknown'))
        notify(src, ('Could not add weapon (%s).'):format(reason or 'unknown'), 'error')
    end
end)

RegisterNetEvent('vMenu:ox:toggleWeapon', function(spawnName, tint)
    local src = source
    if not ready or not isAllowed(src, { 'vMenu.WeaponOptions.Spawn' }) then return end

    local itemName = weaponItemName(spawnName)
    if not itemName then return notify(src, 'That weapon is not available in the inventory.', 'error') end

    if exports.ox_inventory:GetItemCount(src, itemName) > 0 then
        exports.ox_inventory:RemoveItem(src, itemName, 1)
        return notify(src, 'Weapon removed.', 'inform')
    end

    if not spawnAllowed(src) then return end

    local metadata = { ammo = DEFAULT_AMMO, components = {} }
    if type(tint) == 'number' and tint > 0 then metadata.tint = tint end

    local ok, reason = exports.ox_inventory:AddItem(src, itemName, 1, metadata)
    if ok then
        notify(src, 'Weapon added to your inventory.', 'success')
    else
        lib.print.warn(('[vMenu] AddItem(%s) failed: %s'):format(itemName, reason or 'unknown'))
        notify(src, ('Could not add weapon (%s).'):format(reason or 'unknown'), 'error')
    end
end)

RegisterNetEvent('vMenu:ox:removeWeapon', function(spawnName)
    local src = source
    if not ready or not isAllowed(src, { 'vMenu.WeaponOptions.Spawn' }) then return end

    local itemName = weaponItemName(spawnName)
    if not itemName then return end

    if exports.ox_inventory:RemoveItem(src, itemName, 1) then
        notify(src, 'Weapon removed.', 'success')
    else
        notify(src, 'You do not have that weapon.', 'error')
    end
end)

RegisterNetEvent('vMenu:ox:giveAllWeapons', function()
    local src = source
    if not ready or not isAllowed(src, { 'vMenu.WeaponOptions.GetAll' }) then return end
    if not spawnAllowed(src) then return end

    local count = 0
    for itemName, weapon in pairs(oxWeapons) do
        if exports.ox_inventory:GetItemCount(src, itemName) == 0 then
            local metadata = { ammo = DEFAULT_AMMO, components = {} }
            if exports.ox_inventory:AddItem(src, itemName, 1, metadata) then
                count += 1
            end
        end
    end

    notify(src, ('Added %d weapons to your inventory.'):format(count), 'success')
end)

RegisterNetEvent('vMenu:ox:removeAllWeapons', function()
    local src = source
    if not ready or not isAllowed(src, { 'vMenu.WeaponOptions.RemoveAll' }) then return end

    local items = exports.ox_inventory:GetInventoryItems(src)
    if not items then return end

    -- Snapshot the weapon slots first; RemoveItem mutates the live items table.
    local toRemove = {}
    for _, slot in pairs(items) do
        if slot and oxWeapons[slot.name] then
            toRemove[#toRemove + 1] = { name = slot.name, count = slot.count, slot = slot.slot }
        end
    end

    for i = 1, #toRemove do
        exports.ox_inventory:RemoveItem(src, toRemove[i].name, toRemove[i].count, nil, toRemove[i].slot)
    end

    notify(src, 'Removed all weapons.', 'success')
end)

--#endregion

--#region Ammo

local function setLoadedAmmo(src, itemName, count)
    local slot = exports.ox_inventory:GetSlotWithItem(src, itemName)
    if not slot then return false end

    local metadata = slot.metadata or {}
    metadata.ammo = count
    exports.ox_inventory:SetMetadata(src, slot.slot, metadata)
    return true
end

RegisterNetEvent('vMenu:ox:refillAmmo', function(spawnName)
    local src = source
    if not ready or not isAllowed(src, { 'vMenu.WeaponOptions.Spawn', 'vMenu.WeaponOptions.SetAllAmmo' }) then return end

    local itemName = weaponItemName(spawnName)
    if not itemName then return end

    if not setLoadedAmmo(src, itemName, DEFAULT_AMMO) then
        return notify(src, 'Get the weapon first before refilling ammo.', 'error')
    end

    -- Native ammo only: refilling sets the weapon's loaded reserve; re-equip to apply in-hand.
    notify(src, 'Ammo refilled (re-equip to apply).', 'success')
end)

RegisterNetEvent('vMenu:ox:setAllAmmo', function(count)
    local src = source
    if not ready or not isAllowed(src, { 'vMenu.WeaponOptions.SetAllAmmo' }) then return end
    count = tonumber(count)
    if not count then return end

    local items = exports.ox_inventory:GetInventoryItems(src)
    if not items then return end

    for _, slot in pairs(items) do
        if slot and oxWeapons[slot.name] then
            local metadata = slot.metadata or {}
            metadata.ammo = count
            exports.ox_inventory:SetMetadata(src, slot.slot, metadata)
        end
    end

    notify(src, ('Set ammo to %d on all weapons.'):format(count), 'success')
end)

--#endregion

--#region Attachments

RegisterNetEvent('vMenu:ox:toggleComponent', function(spawnName, componentHash)
    local src = source
    if not ready or not isAllowed(src, { 'vMenu.WeaponOptions.Spawn' }) then return end

    local itemName = weaponItemName(spawnName)
    if not itemName then return end

    componentHash = tonumber(componentHash)
    local componentItem = componentHash and componentHashToItem[componentHash]

    if not componentItem then
        return notify(src, 'That attachment is not supported by the inventory.', 'error')
    end

    local slot = exports.ox_inventory:GetSlotWithItem(src, itemName)
    if not slot then return notify(src, 'Get the weapon first.', 'error') end

    local metadata = slot.metadata or {}
    local components = metadata.components or {}

    local foundIndex
    for i = 1, #components do
        if components[i] == componentItem then foundIndex = i break end
    end

    if foundIndex then
        table.remove(components, foundIndex)
        notify(src, 'Attachment removed.', 'inform')
    else
        components[#components + 1] = componentItem
        notify(src, 'Attachment equipped.', 'success')
    end

    metadata.components = components
    exports.ox_inventory:SetMetadata(src, slot.slot, metadata)
end)

RegisterNetEvent('vMenu:ox:setTint', function(spawnName, tint)
    local src = source
    if not ready or not isAllowed(src, { 'vMenu.WeaponOptions.Spawn' }) then return end

    local itemName = weaponItemName(spawnName)
    if not itemName then return end
    tint = tonumber(tint)
    if not tint then return end

    local slot = exports.ox_inventory:GetSlotWithItem(src, itemName)
    if not slot then return notify(src, 'Get the weapon first.', 'error') end

    local metadata = slot.metadata or {}
    metadata.tint = tint
    exports.ox_inventory:SetMetadata(src, slot.slot, metadata)
    notify(src, 'Weapon tint updated.', 'success')
end)

--#endregion
