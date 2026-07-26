using System;
using System.Collections.Generic;
using CitizenFX.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static CitizenFX.Core.Native.API;

namespace vMenuClient
{
    // Loads config/addon_vehicles.json and config/addon_weapons.json — addon vehicles/weapons grouped
    // into custom categories with an optional ACE permission per category (resolved server-side).
    public class AddonCategories : BaseScript
    {
        public class Category
        {
            public string Name;
            public string Ace;                 // null = available to everyone
            public List<string> Vehicles = new List<string>();
            public Dictionary<string, string> Weapons = new Dictionary<string, string>(); // friendly name -> WEAPON_HASH
        }

        public static List<Category> VehicleCategories { get; private set; } = new List<Category>();
        public static List<Category> WeaponCategories { get; private set; } = new List<Category>();
        private static readonly HashSet<string> AllowedAces = new HashSet<string>();

        public static bool IsAllowed(Category c) => string.IsNullOrEmpty(c.Ace) || AllowedAces.Contains(c.Ace);

        public AddonCategories()
        {
            VehicleCategories = ParseVehicles(LoadResourceFile(GetCurrentResourceName(), "config/addon_vehicles.json"));
            WeaponCategories = ParseWeapons(LoadResourceFile(GetCurrentResourceName(), "config/addon_weapons.json"));

            EventHandlers["vMenu:SetAddonPerms"] += new Action<string>(OnSetPerms);
            TriggerServerEvent("vMenu:RequestAddonPerms");
        }

        private void OnSetPerms(string json)
        {
            AllowedAces.Clear();
            var list = JsonConvert.DeserializeObject<List<string>>(json ?? "[]") ?? new List<string>();
            foreach (var a in list) AllowedAces.Add(a);
        }

        private static List<Category> ParseVehicles(string json)
        {
            var result = new List<Category>();
            if (string.IsNullOrEmpty(json)) return result;
            if (!(JObject.Parse(json)["categories"] is JObject root)) return result;
            foreach (var kv in root)
            {
                if (!(kv.Value is JObject obj)) continue;
                var c = new Category { Name = kv.Key, Ace = (string)obj["ace"] };
                if (obj["vehicles"] is JArray arr)
                    foreach (var v in arr) c.Vehicles.Add((string)v);
                result.Add(c);
            }
            return result;
        }

        private static List<Category> ParseWeapons(string json)
        {
            var result = new List<Category>();
            if (string.IsNullOrEmpty(json)) return result;
            if (!(JObject.Parse(json)["categories"] is JObject root)) return result;
            foreach (var kv in root)
            {
                if (!(kv.Value is JObject obj)) continue;
                var c = new Category { Name = kv.Key, Ace = (string)obj["ace"] };
                if (obj["weapons"] is JObject w)
                    foreach (var wk in w) c.Weapons[wk.Key] = (string)wk.Value;
                result.Add(c);
            }
            return result;
        }
    }
}
