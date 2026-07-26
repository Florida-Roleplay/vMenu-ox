-- Syncs the menu accent with the FSRP-wide accent from fsrp-hud.
-- fsrp-hud exposes: exports['fsrp-hud']:GetCurrentAccentColor()  (hex string, e.g. "#257EF2")
-- and fires the local event 'fsrp-hud:accentColorChanged' whenever it changes.
-- If fsrp-hud isn't running, this does nothing and the menu keeps its default accent.

local function hexToRgb(hex)
    if type(hex) ~= "string" then return nil end
    hex = hex:gsub("#", "")
    if #hex < 6 then return nil end
    return tonumber(hex:sub(1, 2), 16), tonumber(hex:sub(3, 4), 16), tonumber(hex:sub(5, 6), 16)
end

local function applyAccent(color)
    local r, g, b
    if type(color) == "string" then
        r, g, b = hexToRgb(color)
    elseif type(color) == "table" then
        if color.r and color.g and color.b then
            r, g, b = color.r, color.g, color.b
        elseif color.hex then
            r, g, b = hexToRgb(color.hex)
        end
    end
    if r then exports.vMenu:SetAccent(math.floor(r + 0.5), math.floor(g + 0.5), math.floor(b + 0.5)) end
end

-- Pull the current accent on start (retry a few times while fsrp-hud initialises).
CreateThread(function()
    for _ = 1, 10 do
        Wait(500)
        local ok, color = pcall(function() return exports["fsrp-hud"]:GetCurrentAccentColor() end)
        if ok and color then
            applyAccent(color)
            break
        end
    end
end)

-- Follow live changes.
AddEventHandler("fsrp-hud:accentColorChanged", function(color)
    applyAccent(color)
end)
