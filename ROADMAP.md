# SideQuest — Roadmap & deferred work

Things intentionally left for later, so they don't get lost. Not a schedule —
just an ordered list of what's next and why.

## No-show verification

Layer 1 (mutual check-in) is **built**: the quester checks in, the poster
confirms, and a "report no-show" opens a slot dispute with the check-in state as
evidence. The next layers upgrade the *quality* of that same signal:

- **Layer 2 — time-window auto-flag.** A background `IHostedService` scans
  `Active` slots past `Deadline + grace` with no `CheckedInAt` and auto-flags
  them as no-show candidates (notify the poster, surface for review). Pairs with
  the other accountability rule: if the poster neither completes nor disputes
  within the review window, auto-release escrow to protect the quester.
- **Layer 3 — hardware proof (mobile).** GPS check-in geofenced against the
  quest location (needs lat/long, not just the text location), and/or a photo
  upload as proof of work. Strongest evidence; needs mobile permissions,
  geocoding, and privacy handling.

## Payments / go-live

- Real Stripe needs **poster payment collection** (Checkout/Elements) and
  **quester Connect onboarding** before flipping `Stripe:SecretKey`. The
  `IPaymentService` abstraction and the full hold → release/refund state machine
  already run in mock mode.

## Auth

- **Auth0 login** is not wired. Actors (poster/quester/admin) are currently taken
  from request bodies or default to seeded users. Once auth lands, these resolve
  from the access token and the stubbed "both directions shown" UIs collapse to
  just the acting user.

## Data

- Local dev uses an **in-memory database that resets on restart**. Move to
  SQLite (dev) / PostgreSQL (prod) for persistence when we commit to a DB.

## Product

- **Cancel-entire-quest** action (quest-level), distinct from per-slot disputes.
- **Per-slot skilled bidding** for multi-slot quests (currently fixed-price
  claim-a-slot for fairness at cold launch).
- **Dark mode** and a pass to **optimize the logo asset** (currently a large PNG).
