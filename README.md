# SideQuest

An open quest marketplace. Everyone is a **quester** by default and can also post
quests. What sets it apart:

- **Two-way bidding** — a quester bids, the poster accepts / declines / **counters once**,
  the quester accepts or declines. One counter, terminating (single-slot quests only).
- **Multi-slot quests** — one quest, several **fixed-price** slots that fill in real
  time and auto-close; a dropped/kicked slot reopens and the quest returns to the feed.
- **Live feed** — the board updates over SignalR as slots fill and bids land.
- **Stripe escrow** — funds are captured when a bid is accepted and released on completion.

> Status: V1 in active development. **Mobile (iOS/Android) is in scope from the start** —
> the React frontend is kept portable and the API serves any client.

## Stack

| Layer | Tech |
|---|---|
| Frontend | React 19 · TypeScript 5.7 · Vite 6 · Tailwind CSS v4 · SignalR client |
| Backend | ASP.NET Core 9 Web API · EF Core 9 · SignalR · Stripe.net |
| Database | In-memory by default; PostgreSQL (Npgsql) when a connection string is set |
| Payments | Mock by default; Stripe (separate charges & transfers) when a key is set |
| Planned | Auth0 (auth), Azure Blob (storage), GitHub Actions + Azure (CI/CD) |

## Prerequisites

- [.NET SDK 9](https://dotnet.microsoft.com/download) (built against 9.0.x)
- [Node.js 20+](https://nodejs.org) (developed on 22.x)

## Running locally

The app is two processes: the API on **:5080** and the Vite dev server on **:5173**.
Vite proxies `/api` and `/hubs` (WebSocket) to the API, so run both.

**1. Backend**

```bash
cd backend/SideQuest.Api
dotnet run
```

- API: http://localhost:5080
- Swagger UI: http://localhost:5080/swagger
- Health: http://localhost:5080/health

On startup it seeds categories, users, and a handful of quests into an in-memory
database. **The data resets on every restart.**

**2. Frontend** (in a second terminal)

```bash
cd frontend
npm install
npm run dev
```

Open http://localhost:5173.

## Configuration

Everything runs with zero config out of the box (in-memory DB + mock payments).
To switch to the real backends, set these (env vars or `appsettings` overrides):

| Setting | Effect when set |
|---|---|
| `ConnectionStrings__Postgres` | Use PostgreSQL instead of the in-memory database |
| `Stripe__SecretKey` | Use the real Stripe payment service instead of the mock |
| `Payments__PosterFeePercent` | Poster fee (default `0.12`) |
| `Payments__QuesterFeePercent` | Quester fee (default `0.05`) |

Example — run against a local Postgres:

```bash
# PowerShell
$env:ConnectionStrings__Postgres = "Host=localhost;Database=sidequest;Username=postgres;Password=postgres"
dotnet run
```

Do **not** commit real keys — `appsettings.json` ships with empty values, and
`.env`, `secrets.json`, and `appsettings.*.local.json` are git-ignored.

## Project layout

```
backend/SideQuest.Api/
  Controllers/   Quests, Bids, Categories
  Models/        Quest, QuestSlot, Bid, EscrowPayment, User, ...
  Services/      Payments (Mock + Stripe), QuestWorkflow, QuestNotifier
  Hubs/          QuestHub (SignalR)
  Data/          AppDbContext, SeedData
frontend/src/
  pages/         Quest feed, quest detail, create quest
  components/    QuestCard, BidPanel, StatusBadge, ...
  api.ts         Backend client        hub.ts   SignalR client
```

## How the money flow works

Escrow uses **capture-at-acceptance**: posting is free, and funds are captured
per slot when a bid is accepted. A failed capture returns `402` and leaves the
slot open (nothing is persisted on failure). Completion (`POST /api/quests/{id}/complete`)
releases held escrow to the questers, with fees taken at payout.

Going live with real Stripe still needs poster payment collection (Checkout/Elements)
and quester Connect onboarding — the `IPaymentService` abstraction is in place for both.

## V1 scope

Feed (location-aware; filter by category/price/slots) · quest creation ·
bidding + counter-offer · slot fill & auto-close (SignalR) · Stripe escrow ·
completion & payout · dispute/flag (manual review) · ratings (both sides) ·
profiles & badges · save-quest (notify on slot open) · categories · mobile apps.

**Deferred to V2:** in-app chat, social sharing, referrals, subscription tiers,
advanced analytics.
