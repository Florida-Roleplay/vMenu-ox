-- Manifest data
fx_version 'bodacious'
games { 'gta5' }

description 'vMenu Fork - github.com/DukeOfCheese/vMenu-ox'
version '3.0.0'
author 'Tom Grobbe (vMenu), Gravxd & DukeOfCheese (vMenu-ox)'
ui_page 'nui/index.html'

lua54 "yes"
shared_scripts {
    "@ox_lib/init.lua"
}

-- Adds additional logging, useful when debugging issues.
client_debug_mode 'false'
server_debug_mode 'false'

-- Leave this set to '0' to prevent compatibility issues
-- and to keep the save files your users.
experimental_features_enabled '0'

-- Files & scripts
files {
    'Newtonsoft.Json.dll',
    'MenuAPI.dll',
    'config/*.json',
    'storage.html',
    'nui/index.html',
    'images/*.png'
}

client_scripts {
    'vMenuClient.net.dll',
    'config/config_client.lua',
    'client/*.lua',
    'lua/menu_api.lua',
    'addons/*.lua'
}
server_scripts {
    'vMenuServer.net.dll',
    'config/config_server.lua',
    'server/*.lua',
    'server/addons/*.lua'
}

dependencies {
    'ox_lib'
}
