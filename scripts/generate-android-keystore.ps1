# Generate FCA Contractor Command Android release keystore (founder-only — run once locally).
# Output: fca-mobile-release.keystore in repo root (gitignored). Store passwords in vault.

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$keystore = Join-Path $repoRoot "fca-mobile-release.keystore"

if (Test-Path $keystore) {
  Write-Host "Keystore already exists: $keystore"
  Write-Host "Delete manually if you intend to regenerate."
  exit 1
}

Write-Host "Generating release keystore at: $keystore"
Write-Host "You will be prompted for store password, key password, and certificate fields."
Write-Host "Use CN: Future Contractors of America LLC"
Write-Host ""

& keytool -genkeypair -v `
  -keystore $keystore `
  -keyalg RSA `
  -keysize 2048 `
  -validity 10000 `
  -alias fca

Write-Host ""
Write-Host "Next steps:"
Write-Host "  1. Move passwords to your password vault (NOT git)."
Write-Host "  2. Base64-encode keystore for GitHub secret ANDROID_KEYSTORE_BASE64:"
Write-Host "     [Convert]::ToBase64String([IO.File]::ReadAllBytes('$keystore')) | Set-Clipboard"
Write-Host "  3. Add ANDROID_* and FIREBASE_* secrets — see docs/MOBILE_CI_FOUNDER_SETUP.md"
Write-Host "  4. Trigger .github/workflows/android-release.yml on GitHub Actions"
