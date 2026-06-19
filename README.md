# FCA Contractor Command — Mobile (C# / .NET MAUI)

Native iOS and Android app for **Future Contractors of America**. Pure C# — no JavaScript or Python in the mobile codebase.

Connects to the live FCA platform:

- `https://auricrux-central.azurewebsites.net` (API)
- `https://futurecontractorsofamerica.com` (web / billing)

## Product surfaces

| Area | Mobile screen |
|------|----------------|
| Lead pipeline | Leads tab |
| Job sites | Jobs tab |
| Plan room | Command Center ? Plan room |
| Billing | Command Center ? Billing |
| Communications | Command Center ? Customer communications |
| Customer success | Command Center ? Customer success |
| Workforce training | Training tab |

## Stack

- .NET 8 + .NET MAUI
- Targets: `net8.0-android`, `net8.0-ios`
- CI: GitHub Actions (`maui-ci.yml`) — Android on Windows, iOS on macOS

## Local build

Requires .NET 8 SDK + MAUI workload:

```powershell
dotnet workload install maui
dotnet restore FcaMobile.sln
dotnet build src/FcaMobile/FcaMobile.csproj -c Release -f net8.0-android
```

For iOS (macOS only):

```powershell
dotnet build src/FcaMobile/FcaMobile.csproj -c Release -f net8.0-ios
```

## Legacy note

The Expo/React Native repo (`fca-mobile`) is deprecated for product delivery. This repo is the canonical mobile stack.

## App ID

`com.futurecontractorsofamerica.mobile`

## Plans / checkout

| Plan | Price |
|------|-------|
| Startup | $99/mo |
| Pilot | $2,500 |
| Enterprise | Custom |

Pilot checkout: `https://buy.stripe.com/bJe14o0fQ5Pn8Tt7Bw5gc01`
