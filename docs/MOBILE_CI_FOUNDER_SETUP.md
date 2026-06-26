# Mobile CI/CD — Founder Setup (FCA Contractor Command MAUI)

Complete these steps once before Firebase App Distribution and Play Store uploads work in CI.

## 1. Firebase project: `fca-contractor-command`

1. [Firebase Console](https://console.firebase.google.com/) → **Add project** → name: `fca-contractor-command`
2. **Add app** → Android → package: `com.futurecontractorsofamerica.mobile`
3. Skip `google-services.json` download (not required for App Distribution-only)
4. **Build → App Distribution** → Get started → create tester group **`internal`** → add your Google account email
5. Copy **Firebase App ID** (`1:…:android:…`) for GitHub secret `FIREBASE_APP_ID`
6. [Google Cloud Console](https://console.cloud.google.com/) (same project) → **IAM → Service Accounts** → Create `github-actions-fca`
7. Role: **Firebase App Distribution Admin**
8. **Keys → Add key → JSON** → download → GitHub secret `FIREBASE_SERVICE_ACCOUNT_JSON`

## 2. Android release keystore

```powershell
cd C:\Users\Auricrux\Documents\GitHub\fca-mobile-maui
.\scripts\generate-android-keystore.ps1
```

## 3. GitHub secrets (`fca-mobile-maui` repo)

| Secret | Value |
|--------|-------|
| `ANDROID_KEYSTORE_BASE64` | Base64 of `fca-mobile-release.keystore` |
| `ANDROID_KEYSTORE_PASSWORD` | Keystore password |
| `ANDROID_KEY_PASSWORD` | Key password |
| `ANDROID_KEY_ALIAS` | `fca` |
| `FIREBASE_APP_ID` | From step 1 |
| `FIREBASE_SERVICE_ACCOUNT_JSON` | Full JSON from step 1 |
| `GOOGLE_PLAY_SERVICE_ACCOUNT_JSON` | *(Phase 2)* Play Console API service account |

Encode keystore:

```powershell
[Convert]::ToBase64String([IO.File]::ReadAllBytes("fca-mobile-release.keystore")) | Set-Clipboard
```

## 4. Verify CI

1. GitHub → **Actions** → **MAUI Android Release** → **Run workflow**
2. Confirm job succeeds; download APK and AAB artifacts
3. Check email for Firebase App Distribution invite; install on Android device
4. Smoke test: Command Center, Leads, Jobs, Training

## 5. Phase 2 — Google Play internal track

1. [Play Console](https://play.google.com/console) → FCA LLC org → create app **FCA Contractor Command** (`com.futurecontractorsofamerica.mobile`)
2. Link Play API service account → `GOOGLE_PLAY_SERVICE_ACCOUNT_JSON`
3. Re-run release workflow; AAB uploads to **internal** track automatically

See also: [fca-bid-tracker FOUNDER_COMPLETION_GUIDE.md](https://github.com/Future-Contractors-of-America-LLC/fca-bid-tracker/blob/main/docs/FOUNDER_COMPLETION_GUIDE.md)
