using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CitizenFX.Core;
using MenuAPI;
using Newtonsoft.Json;

namespace vMenuClient
{
    // Lets Lua (client scripts in this resource, or the addons/ folder) build menu categories and
    // options without recompiling the C#. Lua drives these exports; interaction is dispatched back
    // to Lua via the "vMenuExtDispatch" export. See lua/menu_api.lua for the friendly wrapper.
    public class ExternalMenuApi : BaseScript
    {
        private readonly Dictionary<int, Menu> _menus = new Dictionary<int, Menu>();
        private readonly Dictionary<int, MenuItem> _items = new Dictionary<int, MenuItem>();
        private readonly Dictionary<MenuItem, int> _itemIds = new Dictionary<MenuItem, int>();
        private readonly List<Action> _pending = new List<Action>();
        private Menu _addonsMenu;
        private bool _ready;
        private int _nextId = 1;

        public ExternalMenuApi()
        {
            Exports.Add("CreateCategory", new Func<string, string, string, int>(CreateCategory));
            Exports.Add("CreateSubmenu", new Func<int, string, string, int>(CreateSubmenu));
            Exports.Add("AddButton", new Func<int, string, string, string, string, int>(AddButton));
            Exports.Add("AddCheckbox", new Func<int, string, bool, string, string, int>(AddCheckbox));
            Exports.Add("AddList", new Func<int, string, string, int, string, string, int>(AddList));
            Exports.Add("AddSlider", new Func<int, string, int, int, int, string, string, int>(AddSlider));
            Exports.Add("SetItemEnabled", new Action<int, bool>(SetItemEnabled));
            Exports.Add("SetItemLabel", new Action<int, string>(SetItemLabel));
            Exports.Add("SetItemRightLabel", new Action<int, string>(SetItemRightLabel));
            Exports.Add("SetItemDescription", new Action<int, string>(SetItemDescription));
            Exports.Add("SetItemIcon", new Action<int, string>(SetItemIcon));
            Exports.Add("SetChecked", new Action<int, bool>(SetChecked));
            Exports.Add("SetListItems", new Action<int, string, int>(SetListItems));
            Exports.Add("ClearMenu", new Action<int>(ClearMenu));
            Exports.Add("OpenMenu", new Action<int>(OpenMenuById));
            Exports.Add("SetAccent", new Action<int, int, int>((r, g, b) => MenuNui.SetAccent(r, g, b)));
            Exports.Add("RefreshPermissions", new Action(() =>
            {
                TriggerServerEvent("vMenu:RequestPermissions");
                TriggerServerEvent("vMenu:RequestAddonPerms");
            }));

            Tick += WaitForReady;
            // Re-attach Lua-added categories when the menu tree is rebuilt (e.g. live ACE refresh).
            MainMenu.OnMenusRebuilt += ReattachAll;
        }

        private async Task WaitForReady()
        {
            if (!_ready && MainMenu.Menu != null)
            {
                _ready = true;
                foreach (var a in _pending) { try { a(); } catch { } }
                Tick -= WaitForReady;
            }
            await Delay(_ready ? 60000 : 250);
        }

        // The main menu was rebuilt; our category menus were dropped from the pool. Re-run the
        // attach actions so they re-add themselves to the fresh main menu.
        private void ReattachAll()
        {
            _addonsMenu = null;
            foreach (var a in _pending) { try { a(); } catch { } }
        }

        private void Dispatch(string kind, int itemId, object value)
        {
            try { Exports["vMenu"].vMenuExtDispatch(kind, itemId, value); } catch { }
        }

        private void Attach(Action a)
        {
            _pending.Add(a);
            if (_ready) { try { a(); } catch { } }
        }

        private int Register(Menu menu)
        {
            int id = _nextId++;
            _menus[id] = menu;
            menu.OnItemSelect += (m, item, idx) => { if (_itemIds.TryGetValue(item, out var iid)) Dispatch("select", iid, null); };
            menu.OnCheckboxChange += (m, item, idx, chk) => { if (_itemIds.TryGetValue(item, out var iid)) Dispatch("checkbox", iid, chk); };
            menu.OnListIndexChange += (m, item, oldI, newI, idx) => { if (_itemIds.TryGetValue(item, out var iid)) Dispatch("list", iid, newI); };
            menu.OnSliderPositionChange += (m, item, oldP, newP, idx) => { if (_itemIds.TryGetValue(item, out var iid)) Dispatch("slider", iid, newP); };
            return id;
        }

        private int RegisterItem(MenuItem item)
        {
            int id = _nextId++;
            _items[id] = item;
            _itemIds[item] = id;
            return id;
        }

        private Menu AddonsMenu()
        {
            if (_addonsMenu == null)
            {
                _addonsMenu = new Menu(Game.Player.Name, "Addons");
                var btn = new MenuItem("Addons", "Server addon options.") { Label = "→→→" };
                MenuNui.SetIcon(btn, "star");
                MenuController.AddSubmenu(MainMenu.Menu, _addonsMenu);
                MainMenu.Menu.AddMenuItem(btn);
                MenuController.BindMenuItem(MainMenu.Menu, _addonsMenu, btn);
            }
            return _addonsMenu;
        }

        private int CreateCategory(string title, string icon, string placement)
        {
            var menu = new Menu(Game.Player.Name, string.IsNullOrEmpty(title) ? "Menu" : title);
            int id = Register(menu);
            var btn = new MenuItem(title ?? "Menu", "") { Label = "→→→" };
            if (!string.IsNullOrEmpty(icon)) MenuNui.SetIcon(btn, icon);
            Attach(() =>
            {
                var parent = placement == "addons" ? AddonsMenu() : MainMenu.Menu;
                MenuController.AddSubmenu(parent, menu);
                parent.AddMenuItem(btn);
                MenuController.BindMenuItem(parent, menu, btn);
            });
            return id;
        }

        private int CreateSubmenu(int parentId, string title, string icon)
        {
            if (!_menus.TryGetValue(parentId, out var parent)) return 0;
            var sub = new Menu(Game.Player.Name, title ?? "Submenu");
            int id = Register(sub);
            var btn = new MenuItem(title ?? "Submenu", "") { Label = "→→→" };
            if (!string.IsNullOrEmpty(icon)) MenuNui.SetIcon(btn, icon);
            RegisterItem(btn);
            MenuController.AddSubmenu(parent, sub);
            parent.AddMenuItem(btn);
            MenuController.BindMenuItem(parent, sub, btn);
            return id;
        }

        private int AddButton(int menuId, string label, string icon, string description, string rightLabel)
        {
            if (!_menus.TryGetValue(menuId, out var menu)) return 0;
            var item = new MenuItem(label ?? "", description ?? "");
            if (!string.IsNullOrEmpty(rightLabel)) item.Label = rightLabel;
            if (!string.IsNullOrEmpty(icon)) MenuNui.SetIcon(item, icon);
            int id = RegisterItem(item);
            menu.AddMenuItem(item);
            return id;
        }

        private int AddCheckbox(int menuId, string label, bool check, string icon, string description)
        {
            if (!_menus.TryGetValue(menuId, out var menu)) return 0;
            var item = new MenuCheckboxItem(label ?? "", description ?? "", check);
            if (!string.IsNullOrEmpty(icon)) MenuNui.SetIcon(item, icon);
            int id = RegisterItem(item);
            menu.AddMenuItem(item);
            return id;
        }

        private int AddList(int menuId, string label, string itemsJson, int index, string icon, string description)
        {
            if (!_menus.TryGetValue(menuId, out var menu)) return 0;
            var values = ParseList(itemsJson);
            var item = new MenuListItem(label ?? "", values, Clamp(index, values.Count), description ?? "");
            if (!string.IsNullOrEmpty(icon)) MenuNui.SetIcon(item, icon);
            int id = RegisterItem(item);
            menu.AddMenuItem(item);
            return id;
        }

        private int AddSlider(int menuId, string label, int min, int max, int value, string icon, string description)
        {
            if (!_menus.TryGetValue(menuId, out var menu)) return 0;
            var item = new MenuSliderItem(label ?? "", min, max, value) { Description = description ?? "" };
            if (!string.IsNullOrEmpty(icon)) MenuNui.SetIcon(item, icon);
            int id = RegisterItem(item);
            menu.AddMenuItem(item);
            return id;
        }

        private void SetItemEnabled(int itemId, bool enabled) { if (_items.TryGetValue(itemId, out var it)) it.Enabled = enabled; }
        private void SetItemLabel(int itemId, string text) { if (_items.TryGetValue(itemId, out var it)) it.Text = text; }
        private void SetItemRightLabel(int itemId, string text) { if (_items.TryGetValue(itemId, out var it)) it.Label = text; }
        private void SetItemDescription(int itemId, string text) { if (_items.TryGetValue(itemId, out var it)) it.Description = text; }
        private void SetItemIcon(int itemId, string icon) { if (_items.TryGetValue(itemId, out var it)) MenuNui.SetIcon(it, icon); }
        private void SetChecked(int itemId, bool value) { if (_items.TryGetValue(itemId, out var it) && it is MenuCheckboxItem cb) cb.Checked = value; }

        private void SetListItems(int itemId, string itemsJson, int index)
        {
            if (_items.TryGetValue(itemId, out var it) && it is MenuListItem li)
            {
                li.ListItems = ParseList(itemsJson);
                li.ListIndex = Clamp(index, li.ListItems.Count);
            }
        }

        private void ClearMenu(int menuId) { if (_menus.TryGetValue(menuId, out var menu)) menu.ClearMenuItems(); }
        private void OpenMenuById(int menuId) { if (_menus.TryGetValue(menuId, out var menu)) menu.OpenMenu(); }

        private static List<string> ParseList(string json)
        {
            try { return JsonConvert.DeserializeObject<List<string>>(json ?? "[]") ?? new List<string>(); }
            catch { return new List<string>(); }
        }

        private static int Clamp(int i, int count) => count <= 0 ? 0 : Math.Max(0, Math.Min(i, count - 1));
    }
}
