-- Friendly Lua wrapper over the vMenu C# menu-building exports (see ExternalMenuApi.cs).
-- Lets you add categories/options from Lua with no C# rebuild. Loaded before addons/*.lua.

vMenu = vMenu or {}

local handlers = {} -- itemId -> { fn = function, items = { ... } }

-- C# calls this on any interaction; we route it to the stored Lua callback.
exports("vMenuExtDispatch", function(kind, itemId, value)
    local h = handlers[itemId]
    if not h or not h.fn then return end
    if kind == "select" then
        h.fn()
    elseif kind == "checkbox" then
        h.fn(value == true or value == 1)
    elseif kind == "list" then
        local idx = (value or 0) + 1 -- 1-based for Lua
        h.fn(idx, h.items and h.items[idx] or nil)
    elseif kind == "slider" then
        h.fn(value or 0)
    end
end)

local Menu = {}
Menu.__index = Menu

local function wrap(id)
    return setmetatable({ id = id }, Menu)
end

--- Create a top-level category.
--- @param title string
--- @param icon string|nil  icon key (e.g. "car", "gun", "star", "cash")
--- @param placement string|nil  "main" (default) or "addons"
function vMenu.CreateCategory(title, icon, placement)
    return wrap(exports.vMenu:CreateCategory(title, icon, placement or "main"))
end

function Menu:AddSubmenu(title, icon)
    return wrap(exports.vMenu:CreateSubmenu(self.id, title, icon))
end

-- opts (optional): { description = "...", rightLabel = "..." }
function Menu:AddButton(label, icon, onSelect, opts)
    opts = opts or {}
    local id = exports.vMenu:AddButton(self.id, label, icon, opts.description, opts.rightLabel)
    if onSelect then handlers[id] = { fn = onSelect } end
    return id
end

function Menu:AddCheckbox(label, checked, icon, onChange, opts)
    opts = opts or {}
    local id = exports.vMenu:AddCheckbox(self.id, label, checked and true or false, icon, opts.description)
    if onChange then handlers[id] = { fn = onChange } end
    return id
end

-- items: array of strings. index: 1-based starting selection. onChange(index, value).
function Menu:AddList(label, items, index, icon, onChange, opts)
    opts = opts or {}
    local id = exports.vMenu:AddList(self.id, label, json.encode(items or {}), (index or 1) - 1, icon, opts.description)
    if onChange then handlers[id] = { fn = onChange, items = items } end
    return id
end

-- onChange(value)
function Menu:AddSlider(label, min, max, value, icon, onChange, opts)
    opts = opts or {}
    local id = exports.vMenu:AddSlider(self.id, label, min or 0, max or 10, value or 0, icon, opts.description)
    if onChange then handlers[id] = { fn = onChange } end
    return id
end

-- Live updates (pass the id returned by an Add* call).
function vMenu.SetEnabled(itemId, enabled) exports.vMenu:SetItemEnabled(itemId, enabled and true or false) end
function vMenu.SetLabel(itemId, text) exports.vMenu:SetItemLabel(itemId, text) end
function vMenu.SetRightLabel(itemId, text) exports.vMenu:SetItemRightLabel(itemId, text) end
function vMenu.SetDescription(itemId, text) exports.vMenu:SetItemDescription(itemId, text) end
function vMenu.SetIcon(itemId, icon) exports.vMenu:SetItemIcon(itemId, icon) end
function vMenu.SetChecked(itemId, value) exports.vMenu:SetChecked(itemId, value and true or false) end
function vMenu.SetListItems(itemId, items, index) exports.vMenu:SetListItems(itemId, json.encode(items or {}), (index or 1) - 1) end
