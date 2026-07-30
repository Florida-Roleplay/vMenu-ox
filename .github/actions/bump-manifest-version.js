const fs = require('fs')

const MANIFEST = 'build/vMenu/fxmanifest.lua'

const newVersion = process.env.TGT_RELEASE_VERSION.replace('v', '')
const manifest = fs.readFileSync(MANIFEST, 'utf8')

fs.writeFileSync(MANIFEST, manifest.replace(/\bversion\s+(.*)$/gm, `version '${newVersion}'`))
