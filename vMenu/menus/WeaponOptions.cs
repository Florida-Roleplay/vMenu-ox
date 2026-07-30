using System;
using System.Collections.Generic;

using CitizenFX.Core;

using MenuAPI;

using vMenuClient.data;

using static CitizenFX.Core.Native.API;
using static vMenuClient.CommonFunctions;
using static vMenuShared.PermissionsManager;

namespace vMenuClient.menus
{
    public class WeaponOptions
    {
        // Variables
        private Menu menu;

        public bool UnlimitedAmmo { get; private set; } = UserDefaults.WeaponsUnlimitedAmmo;
        public bool NoReload { get; private set; } = UserDefaults.WeaponsNoReload;
        public bool AutoEquipChute { get; private set; } = UserDefaults.AutoEquipChute;
        public bool UnlimitedParachutes { get; private set; } = UserDefaults.WeaponsUnlimitedParachutes;

        private string SearchTerm = "";

        private Dictionary<Menu, ValidWeapon> weaponInfo;
        private Dictionary<MenuItem, string> weaponComponents;

        #region Create Menu
        /// <summary>
        /// Creates the menu.
        /// </summary>
        private void CreateMenu()
        {
            menu = new Menu(Game.Player.Name, "Weapon Options");
            RefreshSpawnableWeapons(menu);
        }
        private void RefreshSpawnableWeapons(Menu menu)
        {
            // Setup weapon dictionaries.
            weaponInfo = new Dictionary<Menu, ValidWeapon>();
            weaponComponents = new Dictionary<MenuItem, string>();

            #region create main weapon options menu and add items

            var getAllWeapons = new MenuItem("Get All Weapons", "Get all weapons.");
            var removeAllWeapons = new MenuItem("Remove All Weapons", "Removes all weapons in your inventory.");
            var unlimitedAmmo = new MenuCheckboxItem("Unlimited Ammo", "Unlimited ammunition supply.", UnlimitedAmmo);
            var noReload = new MenuCheckboxItem("No Reload", "Never reload.", NoReload);
            var setAmmo = new MenuItem("Set All Ammo Count", "Set the amount of ammo in all your weapons.");
            var refillMaxAmmo = new MenuItem("Refill All Ammo", "Give all your weapons max ammo.");
            var spawnByName = new MenuItem("Spawn Weapon By Name", "Enter a weapon mode name to spawn.");
            var searchButton = new MenuItem("Search for Weapon", "This will allow you to search through the available weapons.");

            // Add items based on permissions
            if (IsAllowed(Permission.WPGetAll))
            {
                menu.AddMenuItem(getAllWeapons);
            }
            if (IsAllowed(Permission.WPRemoveAll))
            {
                menu.AddMenuItem(removeAllWeapons);
            }
            if (IsAllowed(Permission.WPUnlimitedAmmo) || IsAllowed(Permission.WPNoReload))
            {
                menu.AddMenuItem(GetSpacerMenuItem("Ammo Options"));
            }
            if (IsAllowed(Permission.WPUnlimitedAmmo))
            {
                menu.AddMenuItem(unlimitedAmmo);
            }
            if (IsAllowed(Permission.WPNoReload))
            {
                menu.AddMenuItem(noReload);
            }
            if (IsAllowed(Permission.WPSetAllAmmo))
            {
                menu.AddMenuItem(setAmmo);
                menu.AddMenuItem(refillMaxAmmo);
            }
            menu.AddMenuItem(searchButton);
            if (IsAllowed(Permission.WPSpawnByName))
            {
                menu.AddMenuItem(spawnByName);
            }
            #endregion

            #region parachute options menu

            if (IsAllowed(Permission.WPParachute))
            {
                // main parachute options menu setup
                var parachuteMenu = new Menu("Parachute Options", "Parachute Options");
                var parachuteBtn = new MenuItem("Parachute Options", "All parachute related options can be changed here.") { Label = "→→→" };

                MenuController.AddSubmenu(menu, parachuteMenu);
                menu.AddMenuItem(parachuteBtn);
                MenuController.BindMenuItem(menu, parachuteMenu, parachuteBtn);

                var chutes = new List<string>()
                {
                    GetLabelText("PM_TINT0"),
                    GetLabelText("PM_TINT1"),
                    GetLabelText("PM_TINT2"),
                    GetLabelText("PM_TINT3"),
                    GetLabelText("PM_TINT4"),
                    GetLabelText("PM_TINT5"),
                    GetLabelText("PM_TINT6"),
                    GetLabelText("PM_TINT7"),

                    // broken in FiveM for some weird reason:
                    GetLabelText("PS_CAN_0"),
                    GetLabelText("PS_CAN_1"),
                    GetLabelText("PS_CAN_2"),
                    GetLabelText("PS_CAN_3"),
                    GetLabelText("PS_CAN_4"),
                    GetLabelText("PS_CAN_5")
                };
                var chuteDescriptions = new List<string>()
                {
                    GetLabelText("PD_TINT0"),
                    GetLabelText("PD_TINT1"),
                    GetLabelText("PD_TINT2"),
                    GetLabelText("PD_TINT3"),
                    GetLabelText("PD_TINT4"),
                    GetLabelText("PD_TINT5"),
                    GetLabelText("PD_TINT6"),
                    GetLabelText("PD_TINT7"),

                    // broken in FiveM for some weird reason:
                    GetLabelText("PSD_CAN_0") + " ~r~For some reason this one doesn't seem to work in FiveM.",
                    GetLabelText("PSD_CAN_1") + " ~r~For some reason this one doesn't seem to work in FiveM.",
                    GetLabelText("PSD_CAN_2") + " ~r~For some reason this one doesn't seem to work in FiveM.",
                    GetLabelText("PSD_CAN_3") + " ~r~For some reason this one doesn't seem to work in FiveM.",
                    GetLabelText("PSD_CAN_4") + " ~r~For some reason this one doesn't seem to work in FiveM.",
                    GetLabelText("PSD_CAN_5") + " ~r~For some reason this one doesn't seem to work in FiveM."
                };

                var togglePrimary = new MenuItem("Toggle Primary Parachute", "Equip or remove the primary parachute");
                var toggleReserve = new MenuItem("Enable Reserve Parachute", "Enables the reserve parachute. Only works if you enabled the primary parachute first. Reserve parachute can not be removed from the player once it's activated.");
                var primaryChutes = new MenuListItem("Primary Chute Style", chutes, 0, $"Primary chute: {chuteDescriptions[0]}");
                var secondaryChutes = new MenuListItem("Reserve Chute Style", chutes, 0, $"Reserve chute: {chuteDescriptions[0]}");
                var unlimitedParachutes = new MenuCheckboxItem("Unlimited Parachutes", "Enable unlimited parachutes and reserve parachutes.", UnlimitedParachutes);
                var autoEquipParachutes = new MenuCheckboxItem("Auto Equip Parachutes", "Automatically equip a parachute and reserve parachute when entering planes/helicopters.", AutoEquipChute);

                // smoke color list
                var smokeColorsList = new List<string>()
                {
                    GetLabelText("PM_TINT8"), // no smoke
                    GetLabelText("PM_TINT9"), // red
                    GetLabelText("PM_TINT10"), // orange
                    GetLabelText("PM_TINT11"), // yellow
                    GetLabelText("PM_TINT12"), // blue
                    GetLabelText("PM_TINT13"), // black
                };
                var colors = new List<int[]>()
                {
                    new int[3] { 255, 255, 255 },
                    new int[3] { 255, 0, 0 },
                    new int[3] { 255, 165, 0 },
                    new int[3] { 255, 255, 0 },
                    new int[3] { 0, 0, 255 },
                    new int[3] { 20, 20, 20 },
                };

                var smokeColors = new MenuListItem("Smoke Trail Color", smokeColorsList, 0, "Choose a smoke trail color, then press select to change it. Changing colors takes 4 seconds, you can not use your smoke while the color is being changed.");

                parachuteMenu.AddMenuItem(togglePrimary);
                parachuteMenu.AddMenuItem(toggleReserve);
                parachuteMenu.AddMenuItem(autoEquipParachutes);
                parachuteMenu.AddMenuItem(unlimitedParachutes);
                parachuteMenu.AddMenuItem(smokeColors);
                parachuteMenu.AddMenuItem(primaryChutes);
                parachuteMenu.AddMenuItem(secondaryChutes);

                parachuteMenu.OnItemSelect += (sender, item, index) =>
                {
                    if (item == togglePrimary)
                    {
                        if (HasPedGotWeapon(Game.PlayerPed.Handle, (uint)GetHashKey("gadget_parachute"), false))
                        {
                            Subtitle.Custom("Primary parachute removed.");
                            RemoveWeaponFromPed(Game.PlayerPed.Handle, (uint)GetHashKey("gadget_parachute"));
                        }
                        else
                        {
                            Subtitle.Custom("Primary parachute added.");
                            GiveWeaponToPed(Game.PlayerPed.Handle, (uint)GetHashKey("gadget_parachute"), 0, false, false);
                        }
                    }
                    else if (item == toggleReserve)
                    {
                        SetPlayerHasReserveParachute(Game.Player.Handle);
                        Subtitle.Custom("Reserve parachute has been added.");

                    }
                };

                parachuteMenu.OnCheckboxChange += (sender, item, index, _checked) =>
                {
                    if (item == unlimitedParachutes)
                    {
                        UnlimitedParachutes = _checked;
                    }
                    else if (item == autoEquipParachutes)
                    {
                        AutoEquipChute = _checked;
                    }
                };

                var switching = false;
                async void IndexChangedEventHandler(Menu sender, MenuListItem item, int oldIndex, int newIndex, int itemIndex)
                {
                    if (item == smokeColors && oldIndex == -1)
                    {
                        if (!switching)
                        {
                            switching = true;
                            SetPlayerCanLeaveParachuteSmokeTrail(Game.Player.Handle, false);
                            await Delay(4000);
                            var color = colors[newIndex];
                            SetPlayerParachuteSmokeTrailColor(Game.Player.Handle, color[0], color[1], color[2]);
                            SetPlayerCanLeaveParachuteSmokeTrail(Game.Player.Handle, newIndex != 0);
                            switching = false;
                        }
                    }
                    else if (item == primaryChutes)
                    {
                        item.Description = $"Primary chute: {chuteDescriptions[newIndex]}";
                        SetPlayerParachuteTintIndex(Game.Player.Handle, newIndex);
                    }
                    else if (item == secondaryChutes)
                    {
                        item.Description = $"Reserve chute: {chuteDescriptions[newIndex]}";
                        SetPlayerReserveParachuteTintIndex(Game.Player.Handle, newIndex);
                    }
                }

                parachuteMenu.OnListItemSelect += (sender, item, index, itemIndex) => IndexChangedEventHandler(sender, item, -1, index, itemIndex);
                parachuteMenu.OnListIndexChange += IndexChangedEventHandler;
            }
            #endregion

            #region Create Weapon Category Submenus
            var spacer = GetSpacerMenuItem("↓ Weapon Categories ↓");
            menu.AddMenuItem(spacer);

            var handGuns = new Menu("Weapons", "Handguns");
            var handGunsBtn = new MenuItem("Handguns");

            var rifles = new Menu("Weapons", "Assault Rifles");
            var riflesBtn = new MenuItem("Assault Rifles");

            var shotguns = new Menu("Weapons", "Shotguns");
            var shotgunsBtn = new MenuItem("Shotguns");

            var smgs = new Menu("Weapons", "Sub-/Light Machine Guns");
            var smgsBtn = new MenuItem("Sub-/Light Machine Guns");

            var throwables = new Menu("Weapons", "Throwables");
            var throwablesBtn = new MenuItem("Throwables");

            var melee = new Menu("Weapons", "Melee");
            var meleeBtn = new MenuItem("Melee");

            var heavy = new Menu("Weapons", "Heavy Weapons");
            var heavyBtn = new MenuItem("Heavy Weapons");

            var snipers = new Menu("Weapons", "Sniper Rifles");
            var snipersBtn = new MenuItem("Sniper Rifles");

            MenuNui.SetIcon(handGunsBtn, "wpn_pistol");
            MenuNui.SetIcon(riflesBtn, "wpn_rifle");
            MenuNui.SetIcon(shotgunsBtn, "wpn_shotgun");
            MenuNui.SetIcon(smgsBtn, "wpn_smg");
            MenuNui.SetIcon(throwablesBtn, "wpn_throwable");
            MenuNui.SetIcon(meleeBtn, "wpn_melee");
            MenuNui.SetIcon(heavyBtn, "wpn_heavy");
            MenuNui.SetIcon(snipersBtn, "wpn_sniper");

            MenuController.AddSubmenu(menu, handGuns);
            MenuController.AddSubmenu(menu, rifles);
            MenuController.AddSubmenu(menu, shotguns);
            MenuController.AddSubmenu(menu, smgs);
            MenuController.AddSubmenu(menu, throwables);
            MenuController.AddSubmenu(menu, melee);
            MenuController.AddSubmenu(menu, heavy);
            MenuController.AddSubmenu(menu, snipers);
            #endregion

            #region Setup weapon category buttons and submenus.
            handGunsBtn.Label = "→→→";
            menu.AddMenuItem(handGunsBtn);
            MenuController.BindMenuItem(menu, handGuns, handGunsBtn);

            riflesBtn.Label = "→→→";
            menu.AddMenuItem(riflesBtn);
            MenuController.BindMenuItem(menu, rifles, riflesBtn);

            shotgunsBtn.Label = "→→→";
            menu.AddMenuItem(shotgunsBtn);
            MenuController.BindMenuItem(menu, shotguns, shotgunsBtn);

            smgsBtn.Label = "→→→";
            menu.AddMenuItem(smgsBtn);
            MenuController.BindMenuItem(menu, smgs, smgsBtn);

            throwablesBtn.Label = "→→→";
            menu.AddMenuItem(throwablesBtn);
            MenuController.BindMenuItem(menu, throwables, throwablesBtn);

            meleeBtn.Label = "→→→";
            menu.AddMenuItem(meleeBtn);
            MenuController.BindMenuItem(menu, melee, meleeBtn);

            heavyBtn.Label = "→→→";
            menu.AddMenuItem(heavyBtn);
            MenuController.BindMenuItem(menu, heavy, heavyBtn);

            snipersBtn.Label = "→→→";
            menu.AddMenuItem(snipersBtn);
            MenuController.BindMenuItem(menu, snipers, snipersBtn);
            #endregion

            #region addon weapon categories (config/addon_weapons.json)
            foreach (var cat in AddonCategories.WeaponCategories)
            {
                var catBtn = new MenuItem(cat.Name, $"Addon weapons — ~o~{cat.Name}~s~.") { LeftIcon = MenuItem.Icon.GUN };
                var catMenu = new Menu("Weapon Options", cat.Name);
                MenuController.AddSubmenu(menu, catMenu);
                menu.AddMenuItem(catBtn);

                if (AddonCategories.IsAllowed(cat) && IsAllowed(Permission.WPSpawn))
                {
                    MenuController.BindMenuItem(menu, catMenu, catBtn);
                }
                else
                {
                    catBtn.LeftIcon = MenuItem.Icon.LOCK;
                    catBtn.Description = "You don't have permission to access this category.";
                    catBtn.Enabled = false;
                }

                var catSpawnNames = new List<string>();
                foreach (var w in cat.Weapons)
                {
                    catMenu.AddMenuItem(new MenuItem(w.Key, $"Add ~y~{w.Key}~s~ to your inventory."));
                    catSpawnNames.Add(w.Value);
                }

                var spawnNames = catSpawnNames;
                catMenu.OnItemSelect += (s2, it2, idx2) =>
                {
                    if (idx2 < 0 || idx2 >= spawnNames.Count)
                    {
                        return;
                    }

                    if (!CanDoInteraction("spawnweapon"))
                    {
                        return;
                    }

                    OxGiveWeapon(spawnNames[idx2]);
                };
            }
            #endregion

            #region Loop through all weapons, create menus for them and add all menu items and handle events.
            foreach (var weapon in ValidWeapons.WeaponList)
            {
                var cat = (uint)GetWeapontypeGroup(weapon.Hash);
                if (!string.IsNullOrEmpty(weapon.Name) && IsAllowed(weapon.Perm))
                {
                    //Log($"[DEBUG LOG] [WEAPON-BUG] {weapon.Name} - {weapon.Perm} = {IsAllowed(weapon.Perm)} & All = {IsAllowed(Permission.WPGetAll)}");
                    #region Create menu for this weapon and add buttons
                    var weaponMenu = new Menu("Weapon Options", weapon.Name)
                    {
                        ShowWeaponStatsPanel = true
                    };
                    var stats = new Game.WeaponHudStats();
                    Game.GetWeaponHudStats(weapon.Hash, ref stats);
                    weaponMenu.SetWeaponStats(stats.hudDamage / 100f, stats.hudSpeed / 100f, stats.hudAccuracy / 100f, stats.hudRange / 100f);
                    var weaponItem = new MenuItem(weapon.Name, $"Open the options for ~y~{weapon.Name}~s~.")
                    {
                        Label = "→→→",
                        LeftIcon = MenuItem.Icon.GUN,
                        ItemData = stats
                    };

                    weaponInfo.Add(weaponMenu, weapon);

                    var getOrRemoveWeapon = new MenuItem("Equip/Remove Weapon", "Add or remove this weapon to/form your inventory.")
                    {
                        LeftIcon = MenuItem.Icon.GUN
                    };
                    weaponMenu.AddMenuItem(getOrRemoveWeapon);
                    if (!IsAllowed(Permission.WPSpawn))
                    {
                        getOrRemoveWeapon.Enabled = false;
                        getOrRemoveWeapon.Description = "You do not have permission to use this option.";
                        getOrRemoveWeapon.LeftIcon = MenuItem.Icon.LOCK;
                    }

                    var fillAmmo = new MenuItem("Re-fill Ammo", "Get max ammo for this weapon.")
                    {
                        LeftIcon = MenuItem.Icon.AMMO
                    };
                    weaponMenu.AddMenuItem(fillAmmo);

                    if (IsAllowed(Permission.WPEditStats))
                    {
                        var statsMenu = new Menu("Edit Weapon Stats", "Edit Weapon Stats");
                        var statsBtn =  new MenuItem("Edit Weapon Stats", "Edit the stats of the weapon here.") { Label = "→→→" };

                        MenuController.AddSubmenu(weaponMenu, statsMenu);
                        weaponMenu.AddMenuItem(statsBtn);
                        MenuController.BindMenuItem(weaponMenu, statsMenu, statsBtn);

                        var defaultDmgMult = GetWeaponDamageModifier(weapon.Hash).ToString();
                        var dmgMultBtn = new MenuItem("Weapon Damage Multiplier", "Edit the weapon damage multiplier for this weapon") { Label = defaultDmgMult + "x"};

                        var defaultShakeAmplitude = GetWeaponRecoilShakeAmplitude(weapon.Hash).ToString();
                        var shakeAmplitudeBtn = new MenuItem("Weapon Shake Amplitude", "Edit the weapon shake amplitude for this weapon") { Label = defaultShakeAmplitude + "x"};
                        
                        statsMenu.AddMenuItem(dmgMultBtn);
                        statsMenu.AddMenuItem(shakeAmplitudeBtn);

                        statsMenu.OnItemSelect += async (sender, item, index) =>
                        {
                            var hash = weapon.Hash;

                            if (item == dmgMultBtn)
                            {
                                var multString = await GetUserInput("Weapon Damage Multiplier", defaultDmgMult);
                                if (float.TryParse(multString, out float multInput))
                                {
                                    SetWeaponDamageModifier(hash, multInput);
                                    item.Label = multInput + "x";
                                    defaultDmgMult = multInput.ToString();
                                    Notify.Success("Set damage modifier to " + multInput.ToString("F2") + "x");
                                }
                                else
                                {
                                    Notify.Error("You must input a valid number for the damage multiplier.");
                                }
                            }
                            else if (item == shakeAmplitudeBtn)
                            {
                                var shakeString = await GetUserInput("Weapon Shake Amplitude", defaultShakeAmplitude);
                                if (float.TryParse(shakeString, out float shakeInput))
                                {
                                    SetWeaponRecoilShakeAmplitude(hash, shakeInput);
                                    item.Label = shakeInput + "x";
                                    defaultShakeAmplitude = shakeInput.ToString();
                                    Notify.Success("Set damage modifier to " + shakeInput.ToString("F2") + "x");
                                }
                                else
                                {
                                    Notify.Error("You must input a valid number for the shake multiplier.");
                                }
                            }
                        };
                    }

                    var tints = new List<string>();
                    if (weapon.Name.Contains(" Mk II"))
                    {
                        foreach (var tint in ValidWeapons.WeaponTintsMkII)
                        {
                            tints.Add(tint.Key);
                        }
                    }
                    else
                    {
                        foreach (var tint in ValidWeapons.WeaponTints)
                        {
                            tints.Add(tint.Key);
                        }
                    }

                    var weaponTints = new MenuListItem("Tints", tints, 0, "Select a tint for your weapon.");
                    weaponMenu.AddMenuItem(weaponTints);
                    #endregion

                    #region Handle weapon specific list changes
                    weaponMenu.OnListIndexChange += (sender, item, oldIndex, newIndex, itemIndex) =>
                    {
                        if (item == weaponTints)
                        {
                            // Tint is stored on the weapon's ox_inventory item metadata; the server applies it
                            // (and notifies if the weapon isn't owned yet).
                            OxSetTint(weaponInfo[sender].SpawnName, newIndex);
                        }
                    };
                    #endregion

                    #region Handle weapon specific button presses
                    // ox_inventory integration: weapons are inventory items, not weapon-wheel entries.
                    weaponMenu.OnItemSelect += (sender, item, index) =>
                    {
                        var info = weaponInfo[sender];

                        if (item == getOrRemoveWeapon)
                        {
                            if (!CanDoInteraction("spawnweapon"))
                            {
                                return;
                            }

                            // The server adds the weapon item if the player doesn't have it, otherwise removes it.
                            OxToggleWeapon(info.SpawnName, 0);
                        }
                        else if (item == fillAmmo)
                        {
                            if (!CanDoInteraction("refillammo"))
                            {
                                return;
                            }

                            OxRefillAmmo(info.SpawnName);
                        }
                    };
                    #endregion

                    #region load components
                    if (weapon.Components?.Count > 0)
                    {
                        foreach (var comp in weapon.Components)
                        {
                            var compItem = new MenuCheckboxItem(comp.Key, "Click to equip or remove this attachment.");
                            weaponComponents.Add(compItem, comp.Key);

                            weaponMenu.AddMenuItem(compItem);

                            #region Handle component button presses
                            weaponMenu.OnCheckboxChange += (sender, item, index, _checked) =>
                            {
                                if (item != compItem) return;
                                var weaponData = weaponInfo[sender];
                                var componentHash = weaponData.Components[weaponComponents[item]];

                                // Attachments live on the weapon's ox_inventory item metadata. The server toggles the
                                // attachment (and notifies "get the weapon first" if the weapon isn't owned yet).
                                OxToggleComponent(weaponData.SpawnName, componentHash);
                            };
                            #endregion
                        }
                    }
                    #endregion
                    #region refresh and add to menu.
                    weaponMenu.RefreshIndex();
                    if (cat == 970310034) // 970310034 rifles
                    {
                        MenuController.AddSubmenu(rifles, weaponMenu);
                        MenuController.BindMenuItem(rifles, weaponMenu, weaponItem);
                        rifles.AddMenuItem(weaponItem);
                    }
                    else if (cat is 416676503 or 690389602) // 416676503 hand guns // 690389602 stun gun
                    {
                        MenuController.AddSubmenu(handGuns, weaponMenu);
                        MenuController.BindMenuItem(handGuns, weaponMenu, weaponItem);
                        handGuns.AddMenuItem(weaponItem);
                    }
                    else if (cat == 860033945) // 860033945 shotguns
                    {
                        MenuController.AddSubmenu(shotguns, weaponMenu);
                        MenuController.BindMenuItem(shotguns, weaponMenu, weaponItem);
                        shotguns.AddMenuItem(weaponItem);
                    }
                    else if (cat is 3337201093 or 1159398588) // 3337201093 sub machine guns // 1159398588 light machine guns
                    {
                        MenuController.AddSubmenu(smgs, weaponMenu);
                        MenuController.BindMenuItem(smgs, weaponMenu, weaponItem);
                        smgs.AddMenuItem(weaponItem);
                    }
                    else if (cat is 1548507267 or 4257178988 or 1595662460) // 1548507267 throwables // 4257178988 fire extinghuiser // jerry can
                    {
                        MenuController.AddSubmenu(throwables, weaponMenu);
                        MenuController.BindMenuItem(throwables, weaponMenu, weaponItem);
                        throwables.AddMenuItem(weaponItem);
                    }
                    else if (cat is 3566412244 or 2685387236) // 3566412244 melee weapons // 2685387236 knuckle duster
                    {
                        MenuController.AddSubmenu(melee, weaponMenu);
                        MenuController.BindMenuItem(melee, weaponMenu, weaponItem);
                        melee.AddMenuItem(weaponItem);
                    }
                    else if (cat == 2725924767) // 2725924767 heavy weapons
                    {
                        MenuController.AddSubmenu(heavy, weaponMenu);
                        MenuController.BindMenuItem(heavy, weaponMenu, weaponItem);
                        heavy.AddMenuItem(weaponItem);
                    }
                    else if (cat == 3082541095) // 3082541095 sniper rifles
                    {
                        MenuController.AddSubmenu(snipers, weaponMenu);
                        MenuController.BindMenuItem(snipers, weaponMenu, weaponItem);
                        snipers.AddMenuItem(weaponItem);
                    }
                    #endregion
                }
            }
            #endregion

            #region Disable submenus if no weapons in that category are allowed.
            if (handGuns.Size == 0)
            {
                handGunsBtn.LeftIcon = MenuItem.Icon.LOCK;
                handGunsBtn.Description = "The server owner removed the permissions for all weapons in this category.";
                handGunsBtn.Enabled = false;
            }
            if (rifles.Size == 0)
            {
                riflesBtn.LeftIcon = MenuItem.Icon.LOCK;
                riflesBtn.Description = "The server owner removed the permissions for all weapons in this category.";
                riflesBtn.Enabled = false;
            }
            if (shotguns.Size == 0)
            {
                shotgunsBtn.LeftIcon = MenuItem.Icon.LOCK;
                shotgunsBtn.Description = "The server owner removed the permissions for all weapons in this category.";
                shotgunsBtn.Enabled = false;
            }
            if (smgs.Size == 0)
            {
                smgsBtn.LeftIcon = MenuItem.Icon.LOCK;
                smgsBtn.Description = "The server owner removed the permissions for all weapons in this category.";
                smgsBtn.Enabled = false;
            }
            if (throwables.Size == 0)
            {
                throwablesBtn.LeftIcon = MenuItem.Icon.LOCK;
                throwablesBtn.Description = "The server owner removed the permissions for all weapons in this category.";
                throwablesBtn.Enabled = false;
            }
            if (melee.Size == 0)
            {
                meleeBtn.LeftIcon = MenuItem.Icon.LOCK;
                meleeBtn.Description = "The server owner removed the permissions for all weapons in this category.";
                meleeBtn.Enabled = false;
            }
            if (heavy.Size == 0)
            {
                heavyBtn.LeftIcon = MenuItem.Icon.LOCK;
                heavyBtn.Description = "The server owner removed the permissions for all weapons in this category.";
                heavyBtn.Enabled = false;
            }
            if (snipers.Size == 0)
            {
                snipersBtn.LeftIcon = MenuItem.Icon.LOCK;
                snipersBtn.Description = "The server owner removed the permissions for all weapons in this category.";
                snipersBtn.Enabled = false;
            }
            #endregion

            #region Handle button presses
            menu.OnItemSelect += async (sender, item, index) =>
            {
                if (item == getAllWeapons)
                {
                    // Adds every permitted weapon to the player's ox_inventory (server-side).
                    OxGiveAllWeapons();
                }
                else if (item == removeAllWeapons)
                {
                    OxRemoveAllWeapons();
                }
                else if (item == setAmmo)
                {
                    SetAllWeaponsAmmo();
                }
                else if (item == refillMaxAmmo)
                {
                    if (!CanDoInteraction("refillammo"))
                    {
                        return;
                    }

                    // Refill the loaded ammo on every weapon currently in the inventory.
                    OxSetAllAmmo(250);
                }
                else if (item == searchButton)
                {
                    SearchTerm = await GetUserInput(windowTitle: "Enter Search Term (Leave BLANK to reset)", maxInputLength: 100);
                    RefreshSpawnableWeapons(menu);
                    SearchTerm = "";
                }
                else if (item == spawnByName)
                {
                    SpawnCustomWeapon();
                }
            };
            #endregion

            #region Handle checkbox changes
            menu.OnCheckboxChange += (sender, item, index, _checked) =>
            {
                if (item == noReload)
                {
                    NoReload = _checked;
                    Subtitle.Custom($"No reload is now {(_checked ? "enabled" : "disabled")}.");
                }
                else if (item == unlimitedAmmo)
                {
                    UnlimitedAmmo = _checked;
                    Subtitle.Custom($"Unlimited ammo is now {(_checked ? "enabled" : "disabled")}.");
                }
            };
            #endregion

            void OnIndexChange(Menu m, MenuItem i)
            {
                if (i.ItemData is Game.WeaponHudStats stats)
                {
                    m.SetWeaponStats(stats.hudDamage / 100f, stats.hudSpeed / 100f, stats.hudAccuracy / 100f, stats.hudRange / 100f);
                    m.ShowWeaponStatsPanel = true;
                }
                else
                {
                    m.ShowWeaponStatsPanel = false;
                }
            }

            handGuns.OnIndexChange += (sender, oldItem, newItem, oldIndex, newIndex) => { OnIndexChange(sender, newItem); };
            rifles.OnIndexChange += (sender, oldItem, newItem, oldIndex, newIndex) => { OnIndexChange(sender, newItem); };
            shotguns.OnIndexChange += (sender, oldItem, newItem, oldIndex, newIndex) => { OnIndexChange(sender, newItem); };
            smgs.OnIndexChange += (sender, oldItem, newItem, oldIndex, newIndex) => { OnIndexChange(sender, newItem); };
            throwables.OnIndexChange += (sender, oldItem, newItem, oldIndex, newIndex) => { OnIndexChange(sender, newItem); };
            melee.OnIndexChange += (sender, oldItem, newItem, oldIndex, newIndex) => { OnIndexChange(sender, newItem); };
            heavy.OnIndexChange += (sender, oldItem, newItem, oldIndex, newIndex) => { OnIndexChange(sender, newItem); };
            snipers.OnIndexChange += (sender, oldItem, newItem, oldIndex, newIndex) => { OnIndexChange(sender, newItem); };

            handGuns.OnMenuOpen += (sender) => { OnIndexChange(sender, sender.GetCurrentMenuItem()); };
            rifles.OnMenuOpen += (sender) => { OnIndexChange(sender, sender.GetCurrentMenuItem()); };
            shotguns.OnMenuOpen += (sender) => { OnIndexChange(sender, sender.GetCurrentMenuItem()); };
            smgs.OnMenuOpen += (sender) => { OnIndexChange(sender, sender.GetCurrentMenuItem()); };
            throwables.OnMenuOpen += (sender) => { OnIndexChange(sender, sender.GetCurrentMenuItem()); };
            melee.OnMenuOpen += (sender) => { OnIndexChange(sender, sender.GetCurrentMenuItem()); };
            heavy.OnMenuOpen += (sender) => { OnIndexChange(sender, sender.GetCurrentMenuItem()); };
            snipers.OnMenuOpen += (sender) => { OnIndexChange(sender, sender.GetCurrentMenuItem()); };
        }

        #endregion

        /// <summary>
        /// Create the menu if it doesn't exist, and then returns it.
        /// </summary>
        /// <returns>The Menu</returns>
        public Menu GetMenu()
        {
            if (menu == null)
            {
                CreateMenu();
            }
            return menu;
        }
    }
}
