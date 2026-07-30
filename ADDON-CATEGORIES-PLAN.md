# Plan — addon vehicle/weapon categories + optional ACE
> **STATUS: implemented** — client+server compile clean; needs in-game ACE/spawn verification.

Goal: define addon vehicles/weapons in their **own config files** with a **custom category** and an
**optional ACE permission** per category, instead of being mixed into `addons.json` (where addon
vehicles just fall into GTA's auto-detected class and can't be permission-gated individually).

## 1. New config files (additive — `addons.json` keeps working)

`config/addon_vehicles.json`
```json
{
  "categories": {
    "Emergency": {
      "ace": "vMenu.Addons.Emergency",
      "vehicles": ["police3", "ambulance2", "firetruk2"]
    },
    "Civilian Addons": {
      "vehicles": ["adder2", "zentorno2"]
    }
  }
}
```

`config/addon_weapons.json`
```json
{
  "categories": {
    "Restricted": {
      "ace": "vMenu.Addons.RestrictedWeapons",
      "weapons": { "Custom Rifle": "WEAPON_CUSTOMRIFLE" }
    }
  }
}
```

- `ace` is **optional** — omit it and the category is available to everyone.
- Existing `addons.json` `vehicles`/`weapons` are untouched and still load into their normal places.

## 2. Where they show

- **Vehicle Spawner** → one extra submenu per category, added after the 23 GTA classes.
- **Weapon Options** → one extra submenu per category, alongside the existing weapon groups.
- Category buttons get an icon (car / gun) via `MenuNui.SetIcon`.

## 3. ACE gating (follows vMenu's existing security model)

vMenu spawns client-side and gates by ACE that the **server** authoritatively resolves and syncs to
the client (same as the existing per-class locks). Plan:

1. On resource start the **server** reads the two config files, and for each category with an `ace`,
   checks `IsPlayerAceAllowed(player, ace)` per player (on join / on menu request).
2. Server sends the player their **allowed category list** (event or statebag).
3. Client builds the category submenus; if a category isn't in the allowed list, it renders **locked**
   (LOCK icon + disabled), exactly like the current "disabled by server owner" classes.
4. Categories with no `ace` are always allowed.

This keeps permission decisions server-side (client can't unlock a category by editing files).

## 4. Files to touch

- `SharedClasses/ConfigManager.cs` — add loaders `GetAddonVehicleCategories()` / `GetAddonWeaponCategories()`.
- `vMenuServer/MainServer.cs` — resolve ACE per player, send allowed categories.
- `vMenu/menus/VehicleSpawner.cs` — build category submenus from config + allowed list.
- `vMenu/menus/WeaponOptions.cs` — same for weapons.
- Ship example `config/addon_vehicles.json` + `config/addon_weapons.json`.

## 5. Open questions before building

- **Category order** — categories before or after the stock GTA classes / weapon groups?
- **Duplicate handling** — if an addon vehicle is in both `addons.json` and a category, prefer the
  category (and skip the auto-class copy)?
- **Permission naming** — free-form `ace` strings (as above), or a fixed `vMenu.Addons.<Category>`
  convention auto-derived from the category name?
