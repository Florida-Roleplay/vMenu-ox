# vMenu ⇄ ox_inventory weapon integration (FSRP)

The vMenu weapon spawner no longer gives weapons to the ped with native calls (which used the default
GTA weapon wheel and desynced with ox_inventory). Every weapon action now flows into **ox_inventory**
as inventory items, and player inventories are keyed to the active **fsrp-characters** character.

## What changed

### ox_inventory (`Z:\resources\[Scripts]\ox_inventory`) — already live, no rebuild needed
- `modules/mysql/server.lua` — added a `fsrp` framework branch: `playerTable = citizen`, `playerColumn = id`.
  The trunk/glovebox `SHOW COLUMNS` is now `pcall`-guarded so a missing vehicle table can't break init.
- `modules/bridge/fsrp/server.lua` + `client.lua` — **new** custom framework bridge. On
  `fsrp-characters:characterSelected` it loads the character's inventory (owner = `tostring(citizen.id)`);
  on a character switch it saves+unloads the previous one first. Player-drop saving is handled by ox core.
- Player inventory JSON persists to `citizen.inventory` (LONGTEXT column, auto-added / already added).

### server.cfg
- `setr inventory:framework fsrp` (before `ensure [Scripts]`).

### vMenu (this repo → deploy to `Z:\resources\[Scripts]\vMenu`) — **requires a C# rebuild**
- `build/vMenu/server/weapons_ox.lua` — **new** net-event handlers that call
  `ox_inventory:AddItem/RemoveItem/SetMetadata/GetSlotWithItem`. All GTA-hash → ox-item mapping
  (weapons, ammo `ammoname`, component `at_*` items) is read from `ox_inventory/data/weapons.lua`
  at runtime, so it stays in sync with the inventory.
- `build/vMenu/client/weapons_ox.lua` — **new** client exports the C# calls.
- `build/vMenu/fxmanifest.lua` — added `ox_inventory` dependency (client/server `*.lua` were already globbed).
- `vMenu/CommonFunctions.cs` — `ExternalFunctions` bridge + static `Ox*` wrappers; `SpawnCustomWeapon`,
  `SetAllWeaponsAmmo`, and `SpawnWeaponLoadoutAsync` routed through ox.
- `vMenu/menus/WeaponOptions.cs` — equip/remove (server-side toggle), refill/set ammo, attachments,
  tint, get-all, remove-all all go through the ox exports. Parachute (a gadget) stays native.

## Behaviour

| Menu action | Result |
|---|---|
| Equip/Remove Weapon | Server adds the `WEAPON_*` item if absent, else removes it (no weapon wheel). |
| Re-fill / Set ammo | Sets the weapon item's loaded `metadata.ammo` and adds `ammo-*` items to reload. |
| Attachment checkbox | Toggles the matching `at_*` component item in the weapon's `metadata.components`. |
| Tint | Sets the weapon item's `metadata.tint`. |
| Get All / Remove All | Adds every permitted weapon / removes all weapon items. |

### Attachment coverage (important)
ox_inventory models attachments as a **curated set of shared component items** (`at_flashlight`,
`at_suppressor_*`, `at_scope_*`, `at_clip_*`, `at_muzzle_*`, `at_skin_*`, `at_grip`, `at_barrel`, …).
vMenu exposes the full raw GTA component matrix. Components that map to an ox item toggle and persist;
components ox doesn't define (a handful of weapon-specific variants) report *"attachment not supported by
the inventory."* Add more entries to `ox_inventory/data/weapons.lua` `Components` to widen coverage —
the vMenu server picks them up automatically.

## Deploy steps
1. **ox_inventory + server.cfg + DB**: already applied on `Z:`. Restart `ox_inventory` (and confirm the
   `citizen.inventory` column exists — ox auto-adds it).
2. **vMenu**: rebuild the C# (`dotnet build vMenu.sln -c Release`, or the repo's build task), copy the
   produced `build/vMenu` to `Z:\resources\[Scripts]\vMenu`, then `ensure`/restart `vMenu`.
3. Restart the server (or `refresh` + restart `ox_inventory`, `fsrp-characters`, `vMenu`).

## Config
- `setr vmenu:ox_default_ammo 250` — ammo given/loaded per weapon (default 250).
- `setr inventory:weaponanims false` — disables ox's draw/holster animation (FSRP has its own).

## Ammo / reload
Weapons use the **native GTA ammo system** — no ammo items in the inventory. vMenu gives each weapon
loaded via `metadata.ammo` (`vmenu:ox_default_ammo`), and native **R** reloads from the reserve. ox only
mirrors the game's ammo, so nothing blocks native reload.

## Vehicle storage (trunk / glovebox)
FSRP has no vehicle-ownership table, so storage is keyed by **saved-vehicle identity**, not the (mutable) plate:
- vMenu tags each spawned Saved Vehicle with the statebag `svStorageId` (the save name). `SpawnVehicle`
  (`CommonFunctions.cs`) fires `vMenu:ox:tagVehicleStorage`, and `server/vehicle_storage_ox.lua` sets the
  statebag **server-side** (works with `sv_stateBagStrictMode true`; includes a near-the-vehicle guard).
- `modules/bridge/fsrp/server.lua` `server.getOwnedVehicleId(entity)` returns `sv:<saveName>` for tagged
  (saved) vehicles — plate-independent, survives respawn. Untagged/non-saved vehicles return **nil**.
- **Saved vehicles** persist in the `fsrp_vehicle_storage` table (`plate` PK, `trunk`, `glovebox`); the fsrp
  mysql branch points ox's vehicle table there, and rows are auto-created (`INSERT IGNORE`) on first access.
- **Non-saved vehicles** are **session-only**: with a nil key ox marks the storage `datastore`
  (modules/inventory/server.lua), so it's never written to the DB and is evicted when the vehicle despawns.
- **Self-healing:** persistent (saved-vehicle) trunk/glovebox inventories **save + evict on close**
  (Inventory.Set), so the next open reloads fresh from the DB. Two copies of the same save converge on next
  open; the only unfixable case is both copies open at the exact same moment (last close wins).
- Result: each Saved Vehicle keeps its own trunk/glovebox across respawns & plate changes; duplicate plates
  never collide; multiple saves = separate storages; random dev spawns don't leave DB rows behind.
