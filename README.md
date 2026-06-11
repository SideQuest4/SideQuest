# SideQuest

An open quest marketplace with two-way bidding and multi-slot quests.

Posters create **quests** (tasks/gigs). Questers **bid** on them; posters can
**counter-offer**. Quests can have multiple **slots**; once all slots are filled
the quest auto-closes. Payment is held in **Stripe Connect escrow** and released
on completion, after which both sides leave **ratings**.

## Stack

| Layer     | Tech                                                      |
| --------- | -------------------------------------------------------- |
| Frontend  | React + TypeScript + Tailwind CSS (Vite)                 |
| Backend   | ASP.NET Core (C#) Web API + EF Core                      |
| Database  | Azure PostgreSQL (local dev falls back to in-memory)     |
| Storage   | Azure Blob Storage                                        |
| Auth      | Auth0                                                     |
| Payments  | Stripe Connect                                            |
| Realtime  | SignalR                                                   |
| CI/CD     | GitHub + GitHub Actions + Azure                           |

## Repo layout

```
SideQuest/
├── backend/SideQuest.Api/   ASP.NET Core Web API + EF Core models & controllers
└── frontend/                Vite React + TS + Tailwind app
```

## Running locally

### Backend (http://localhost:5080)

```bash
cd backend/SideQuest.Api
dotnet run
```

By default the API runs against an **in-memory database** seeded with sample
quests, so it works with zero setup. To use PostgreSQL, set a connection string:

```bash
# PowerShell
$env:ConnectionStrings__Postgres = "Host=localhost;Database=sidequest;Username=postgres;Password=postgres"
dotnet run
```

When `ConnectionStrings__Postgres` is present the API switches to Npgsql and
applies EF Core migrations automatically.

### Frontend (http://localhost:5173)

```bash
cd frontend
npm install
npm run dev
```

The dev server proxies `/api` to the backend at `http://localhost:5080`.

## V1 scope

Quest feed · quest creation · bidding + counter-offers · slot fill & auto-close ·
Stripe escrow · completion & payout · ratings · profiles & badges · save/bookmark ·
categories.

**Not in V1:** in-app chat, social sharing, referrals, native mobile apps,
subscription tiers, advanced analytics. Anything out of scope goes to V2 planning.

## Status

🚧 Month 1 — Foundation. Scaffolding + core quest feed and creation flow.
