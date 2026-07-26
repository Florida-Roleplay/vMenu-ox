using System;
using System.Collections.Generic;
using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using Newtonsoft.Json;

namespace MenuAPI
{
    public static class MenuNui
    {
        public static bool Enabled { get; set; } = true;
        public static string Side { get; set; } = null;

        public static int AccentR { get; private set; } = 64;
        public static int AccentG { get; private set; } = 89;
        public static int AccentB { get; private set; } = 214;

        private static string _last = null;
        private static bool _visible = false;
        private static readonly Dictionary<MenuItem, string> _icons = new Dictionary<MenuItem, string>();

        public static void SetIcon(MenuItem item, string key)
        {
            if (item == null) return;
            if (string.IsNullOrEmpty(key)) _icons.Remove(item);
            else _icons[item] = key;
        }

        public static void SetAccent(int r, int g, int b)
        {
            AccentR = r; AccentG = g; AccentB = b;
            SendNuiMessage(JsonConvert.SerializeObject(new
            {
                action = "setAccent",
                accent = new { r = AccentR, g = AccentG, b = AccentB }
            }));
            _last = null;
        }

        public static void Hide()
        {
            if (!_visible) return;
            _visible = false;
            _last = null;
            SendNuiMessage(JsonConvert.SerializeObject(new { action = "setVisible", visible = false }));
        }

        public static void Send(Menu menu)
        {
            if (menu == null) return;
            object state = Serialize(menu);
            string json = JsonConvert.SerializeObject(new { action = "setMenu", state });
            if (json == _last) return;
            _last = json;
            _visible = true;
            SendNuiMessage(json);
        }

        private static object Serialize(Menu menu)
        {
            List<MenuItem> all = menu.GetMenuItems();
            int size = all.Count;
            int max = menu.MaxItemsOnScreen;
            int offset = menu.ViewIndexOffset;
            if (offset < 0) offset = 0;
            if (offset > Math.Max(0, size - 1)) offset = Math.Max(0, size - 1);

            bool catIcons = GetConvar("vmenu_nui_icons_categories", "true") != "false";
            bool itemIcons = GetConvar("vmenu_nui_icons_items", "true") != "false";

            var items = new List<object>();
            int end = Math.Min(size, offset + max);
            for (int i = offset; i < end; i++)
            {
                items.Add(SerializeItem(all[i], catIcons, itemIcons));
            }

            int selectedRel = menu.CurrentIndex - offset;
            if (selectedRel < 0) selectedRel = 0;

            string desc = "";
            MenuItem cur = menu.GetCurrentMenuItem();
            if (cur != null && cur.Description != null) desc = cur.Description;

            string counter = (menu.CounterPreText ?? "") + (size > 0 ? (menu.CurrentIndex + 1) : 0) + " / " + size;

            return new
            {
                visible = true,
                title = menu.MenuSubtitle ?? "",
                counter,
                autoCounter = false,
                selected = selectedRel,
                accent = new { r = AccentR, g = AccentG, b = AccentB },
                side = ResolveSide(),
                showDescription = true,
                description = desc,
                stats = PanelStats(menu),
                items
            };
        }

        private static readonly string[] WEAPON_LABELS = { "Damage", "Fire Rate", "Accuracy", "Range" };
        private static readonly string[] VEHICLE_LABELS = { "Top Speed", "Acceleration", "Braking", "Traction" };

        private static List<object> PanelStats(Menu menu)
        {
            if (menu.ShowWeaponStatsPanel)
                return StatRows(WEAPON_LABELS, menu.WeaponStats, menu.WeaponComponentStats);
            if (menu.ShowVehicleStatsPanel)
                return StatRows(VEHICLE_LABELS, menu.VehicleStats, menu.VehicleUpgradeStats);
            return null;
        }

        private static List<object> StatRows(string[] labels, float[] baseV, float[] extra)
        {
            var rows = new List<object>();
            for (int i = 0; i < labels.Length; i++)
            {
                float v = baseV[i];
                if (extra != null && i < extra.Length && extra[i] > v) v = extra[i];
                rows.Add(new { label = labels[i], percent = v * 100.0 });
            }
            return rows;
        }

        private static string ResolveSide()
        {
            string cv = GetConvar("vmenu_nui_side", "").ToLower();
            if (cv == "left" || cv == "center" || cv == "right") return cv;
            if (!string.IsNullOrEmpty(Side)) return Side;
            return MenuController.MenuAlignment == MenuController.MenuAlignmentOption.Left ? "left" : "right";
        }

        private static List<object> _hairPalette, _makeupPalette;

        private static List<object> Palette(bool hair)
        {
            var cached = hair ? _hairPalette : _makeupPalette;
            if (cached != null) return cached;
            var sw = new List<object>();
            bool anyColor = false;
            for (int i = 0; i < 64; i++)
            {
                int r = 0, g = 0, b = 0;
                if (hair) GetHairRgbColor(i, ref r, ref g, ref b);
                else GetMakeupRgbColor(i, ref r, ref g, ref b);
                if (r != 0 || g != 0 || b != 0) anyColor = true;
                sw.Add(new { r, g, b });
            }
            if (!anyColor) return sw; // palette not loaded yet — don't cache zeros
            if (hair) _hairPalette = sw; else _makeupPalette = sw;
            return sw;
        }

        private static string StripArrows(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            var sb = new System.Text.StringBuilder(s.Length);
            foreach (char c in s)
                if (c != '→' && c != '←' && c != '▶' && c != '◀') sb.Append(c);
            return sb.ToString().Trim();
        }

        private static Dictionary<string, object> SerializeItem(MenuItem item, bool catIcons, bool itemIcons)
        {
            string text0 = item.Text ?? "";
            if (!item.Enabled && text0.StartsWith("~h~"))
                return new Dictionary<string, object> { ["type"] = "separator", ["label"] = text0.Replace("~h~", "").Trim() };

            bool isCat = MenuController.MenuButtons.ContainsKey(item);
            var it = new Dictionary<string, object>
            {
                ["type"] = "item",
                ["label"] = item.Text ?? "",
                ["enabled"] = item.Enabled,
                ["description"] = item.Description ?? "",
                ["leftBadge"] = LeftBadge(item, isCat, catIcons, itemIcons),
                ["rightBadge"] = Badge(item.RightIcon),
                ["submenu"] = isCat,
            };

            string rl = StripArrows(item.Label);
            if (!string.IsNullOrEmpty(rl)) it["rightLabel"] = rl;

            if (item is MenuCheckboxItem cb)
            {
                it["type"] = "checkbox";
                it["checked"] = cb.Checked;
                it["checkStyle"] = cb.Style == MenuCheckboxItem.CheckboxStyle.Tick ? "tick" : "cross";
            }
            else if (item is MenuListItem li)
            {
                if (li.ShowColorPanel)
                {
                    it["colours"] = new
                    {
                        title = item.Text ?? "Color",
                        swatches = Palette(li.ColorPanelColorType == MenuListItem.ColorPanelType.Hair),
                        index = li.ListIndex,
                    };
                }
                else
                {
                    it["type"] = "list";
                    it["values"] = li.ListItems ?? new List<string>();
                    it["index"] = li.ListIndex;
                }
            }
            else if (item is MenuSliderItem sl)
            {
                it["type"] = "slider";
                int range = sl.Max - sl.Min;
                it["sliderPercent"] = range > 0 ? (double)(sl.Position - sl.Min) / range * 100.0 : 0.0;
            }
            else if (item is MenuDynamicListItem dl)
            {
                it["type"] = "list";
                it["values"] = new List<string> { dl.CurrentItem ?? "" };
                it["index"] = 0;
            }

            return it;
        }

        // ordered: longer / more specific keywords first so they win over generic substrings
        private static readonly string[][] KEYWORDS =
        {
            // specific player/online options
            new[]{"stay in vehicle","seatbelt"}, new[]{"seatbelt","seatbelt"},
            new[]{"force stop","close"}, new[]{"private message","chat"}, new[]{"message","chat"},
            new[]{"clean player","sponge"}, new[]{"wet player","droplet"}, new[]{"dry player","sun"},
            new[]{"clear blood","blood"}, new[]{"set blood","blood"}, new[]{"blood","blood"},
            new[]{"commit suicide","skull"}, new[]{"suicide","skull"},
            new[]{"no ragdoll","stand"}, new[]{"ragdoll","stand"},
            new[]{"fast swim","swim"}, new[]{"swim","swim"},
            new[]{"walking style","walk"}, new[]{"walk style","walk"}, new[]{"walking","walk"},
            new[]{"no reload","magazine"}, new[]{"reload","magazine"}, new[]{"parachute","parachute"},
            // player state
            new[]{"god mode","shield"}, new[]{"godmode","shield"}, new[]{"invincib","shield"}, new[]{"armor","shield"}, new[]{"armour","shield"},
            new[]{"invisible","eye"}, new[]{"invis","eye"}, new[]{"spectate","eye"}, new[]{"noclip","eye"}, new[]{"ignore","eye"},
            new[]{"stamina","run"}, new[]{"fast run","run"}, new[]{"super jump","run"}, new[]{"sprint","run"},
            new[]{"never wanted","star"}, new[]{"set wanted","star"}, new[]{"wanted","star"},
            new[]{"freeze","clock"},
            new[]{"health","heart"}, new[]{"heal","heart"}, new[]{"revive","heart"},
            new[]{"money","cash"}, new[]{"cash","cash"}, new[]{"wallet","cash"},
            // online players
            new[]{"teleport","pin"}, new[]{"waypoint","pin"}, new[]{"summon","pin"}, new[]{"gps","pin"}, new[]{"marker","pin"}, new[]{"coord","pin"}, new[]{"blip","pin"},
            new[]{"identifier","id"}, new[]{"print","id"},
            new[]{"kill","skull"}, new[]{"kick","warning"}, new[]{"ban ","warning"},
            // weapons
            new[]{"loadout","gun"}, new[]{"ammo","magazine"}, new[]{"rifle","gun"}, new[]{"pistol","gun"}, new[]{"handgun","gun"}, new[]{"shotgun","gun"}, new[]{"melee","gun"}, new[]{"grenade","gun"}, new[]{"launcher","gun"}, new[]{"weapon","gun"}, new[]{"gun","gun"},
            // ped / appearance
            new[]{"saved ped","folder"}, new[]{"saved","folder"}, new[]{"collection","person"},
            new[]{"customization","person"}, new[]{"appearance","person"}, new[]{"outfit","person"}, new[]{"animal","person"}, new[]{"male ped","person"}, new[]{"female ped","person"}, new[]{"illuminated","bulb"},
            // clothing / props
            new[]{"mask","mask"},
            new[]{"upper body","shirt"}, new[]{"shirt","shirt"}, new[]{"jacket","shirt"}, new[]{"overlay","shirt"}, new[]{"scarf","shirt"}, new[]{"chain","shirt"},
            new[]{"lower body","pants"}, new[]{"pants","pants"}, new[]{"legs","pants"},
            new[]{"shoe","shoe"}, new[]{"feet","shoe"},
            new[]{"hat","hat"}, new[]{"helmet","hat"},
            new[]{"glasses","glasses"}, new[]{"watch","watch"}, new[]{"bracelet","watch"}, new[]{"bag","parachute"},
            new[]{"badge","id"}, new[]{"logo","id"},
            new[]{"hair","person"}, new[]{"beard","person"}, new[]{"eyebrow","eye"},
            new[]{"makeup","brush"}, new[]{"blush","brush"}, new[]{"lipstick","brush"},
            new[]{"eye colo","eye"}, new[]{"blemish","person"}, new[]{"ageing","person"}, new[]{"complexion","person"}, new[]{"moles","person"}, new[]{"freckle","person"}, new[]{"sun damage","person"},
            new[]{"ped","person"},
            // vehicle / world
            new[]{"livery","brush"}, new[]{"colour","brush"}, new[]{"color","brush"}, new[]{"paint","brush"}, new[]{"tint","brush"}, new[]{"wash","sponge"}, new[]{"clean","sponge"},
            new[]{"wheel","wheel"}, new[]{"tire","wheel"}, new[]{"tyre","wheel"},
            new[]{"neon","bulb"}, new[]{"xenon","bulb"}, new[]{"headlight","bulb"}, new[]{"blackout","bulb"}, new[]{"light","bulb"},
            new[]{"plate","id"}, new[]{"license","id"}, new[]{"hud","id"},
            new[]{"weather","cloud"}, new[]{"snow","cloud"}, new[]{"cloud","cloud"},
            new[]{"time","clock"}, new[]{"clock","clock"},
            new[]{"fuel","fuel"}, new[]{"engine","fuel"}, new[]{"petrol","fuel"},
            new[]{"top speed","speed"}, new[]{"boost","speed"}, new[]{"nitro","speed"}, new[]{"turbo","speed"}, new[]{"speed","speed"},
            new[]{"repair","wrench"}, new[]{"fix","wrench"}, new[]{"mods","wrench"}, new[]{"modification","wrench"}, new[]{"upgrade","wrench"}, new[]{"tuning","wrench"}, new[]{"handling","wrench"}, new[]{"extras","wrench"},
            new[]{"drive","car"}, new[]{"autopilot","car"}, new[]{"auto pilot","car"}, new[]{"spawn vehicle","car"}, new[]{"personal vehicle","car"}, new[]{"vehicle","car"},
            new[]{"scenario","walk"},
            // generic
            new[]{"save","save"}, new[]{"bookmark","save"}, new[]{"clone","save"},
            new[]{"delete","refresh"}, new[]{"reset","refresh"}, new[]{"respawn","refresh"}, new[]{"restore","refresh"}, new[]{"randomize","refresh"}, new[]{"replace","refresh"},
            new[]{"unlock","unlock"}, new[]{"door","unlock"}, new[]{"lock","lock"},
            new[]{"map","map"}, new[]{"radar","map"}, new[]{"voice","chat"},
            new[]{"stop","close"},
            new[]{"setting","tune"}, new[]{"option","tune"}, new[]{"config","tune"}, new[]{"toggle","tune"}, new[]{"enable","tune"},
        };

        private static string KeywordIcon(string text)
        {
            if (string.IsNullOrEmpty(text)) return null;
            string t = text.ToLower();
            foreach (var k in KEYWORDS)
                if (t.Contains(k[0])) return k[1];
            return null;
        }

        private static string LeftBadge(MenuItem item, bool isCat, bool catIcons, bool itemIcons)
        {
            string semantic = Badge(item.LeftIcon);
            if (semantic != null) return semantic;
            if (isCat ? !catIcons : !itemIcons) return null;
            if (_icons.TryGetValue(item, out var ic)) return ic;
            string kw = KeywordIcon(item.Text);
            if (kw != null) return kw;
            return isCat ? "map" : "dot";
        }

        private static string Badge(MenuItem.Icon icon)
        {
            switch (icon)
            {
                case MenuItem.Icon.LOCK:
                case MenuItem.Icon.LOCK_ARENA:
                    return "lock";
                case MenuItem.Icon.STAR:
                case MenuItem.Icon.MISSION_STAR:
                    return "star";
                case MenuItem.Icon.WARNING:
                    return "warning";
                case MenuItem.Icon.TICK:
                    return "success";
                default:
                    return null;
            }
        }
    }
}
