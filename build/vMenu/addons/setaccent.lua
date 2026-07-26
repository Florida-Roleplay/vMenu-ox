-- Test: /vmenu-setaccent opens an ox_lib color picker and applies the chosen colour as the menu accent.

local function hexToRgb(hex)
    if type(hex) ~= "string" then return nil end
    hex = hex:gsub("#", "")
    if #hex < 6 then return nil end
    return tonumber(hex:sub(1, 2), 16), tonumber(hex:sub(3, 4), 16), tonumber(hex:sub(5, 6), 16)
end

RegisterCommand("vmenu-setaccent", function()
    local input = lib.inputDialog("Menu Accent", {
        { type = "color", label = "Accent Colour", default = "#4059d6", format = "hex" },
    })
    if not input or not input[1] then return end

    local r, g, b = hexToRgb(input[1])
    if not r then return end

    exports.vMenu:SetAccent(r, g, b)
    lib.notify({ title = "Menu Accent", description = ("Set to %d, %d, %d"):format(r, g, b), type = "success" })
end, false)
