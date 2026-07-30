const { execSync } = require('child_process')
const fs = require('fs')

const MANIFEST = 'build/vMenu/fxmanifest.lua'

function parseSemver(raw) {
  if (!raw) return null
  const match = String(raw).trim().replace(/^v/i, '').match(/^(\d+)\.(\d+)\.(\d+)$/)
  if (!match) return null
  return { major: Number(match[1]), minor: Number(match[2]), patch: Number(match[3]) }
}

function latestGitTag() {
  try {
    return execSync('git tag -l "v*" --sort=-v:refname', { encoding: 'utf8' })
      .split(/\r?\n/)
      .map((line) => line.trim())
      .find(Boolean)
  } catch {
    return null
  }
}

function manifestVersion() {
  const match = fs.readFileSync(MANIFEST, 'utf8').match(/\bversion\s+['"]([^'"]+)['"]/m)
  return match ? match[1] : null
}

const override = (process.env.TGT_RELEASE_VERSION || '').trim()
let next

if (override) {
  next = parseSemver(override)
  if (!next) throw new Error(`Invalid version override: ${override}`)
} else {
  const base = parseSemver(latestGitTag()) || parseSemver(manifestVersion()) || { major: 0, minor: 0, patch: 0 }
  next = { ...base, patch: base.patch + 1 }
}

const version = `${next.major}.${next.minor}.${next.patch}`
const tag = `v${version}`

console.log(`Resolved release version: ${tag}`)

if (process.env.GITHUB_OUTPUT) {
  fs.appendFileSync(process.env.GITHUB_OUTPUT, `tag=${tag}\nversion=${version}\n`)
}
