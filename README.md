# Credit-Card-Rewards-Optimization
A personal AI-powered credit card assistant designed to help you maximize the value of every transaction across your credit card portfolio. The assistant analyzes your spending amount, merchant, and category to recommend the best card to use based on cashback, reward points, travel miles, milestone , annual fee waiver targets, &amp; active promotions. 
# Credit Card Rewards Optimization

> A personal rewards-optimization engine that recommends the **best card to use for any given transaction** across your credit card portfolio — accounting for cashback, reward points, accelerated categories, milestones, reward caps, annual-fee-waiver targets, and active promotions.

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)
![EF Core](https://img.shields.io/badge/EF%20Core-Code%20First-512BD4)
![Tests](https://img.shields.io/badge/Tests-xUnit-green)

---

## Why this exists

Every card rewards you differently depending on *where* and *what* you spend. Picking the optimal card per transaction is a small optimization problem most people solve badly by memory. This engine models each card's reward structure as data and computes the best card for a transaction deterministically, with a human-readable explanation of *why*.

---

## Architecture

A clean, layered solution with strict dependency direction (`Api -> Core -> Data`). Business logic lives in `Core` and never depends on the web layer, which keeps the reward engine independently testable.

```
Api (presentation)        Controllers, Swagger, static SPA, CORS, Serilog wiring
   |  depends on
Core (domain logic)       RewardCalculationService, TransactionRecommendationService,
   |                      SpendTrackingService, DTOs, Interfaces  (no infra deps)
   |  depends on
Data (persistence)        AppDbContext, EF Core models, Migrations

DataRefresh               Card-offer refresh planning
Tests                     xUnit unit tests
```

| Project | Responsibility |
|---|---|
| **`.Api`** | REST controllers, Swagger, Serilog wiring, static front-end, DI composition root |
| **`.Core`** | Reward calculation, ranking, spend tracking — the domain engine, no infrastructure dependencies |
| **`.Data`** | `AppDbContext`, EF Core entity models, migrations, repository access |
| **`.DataRefresh`** | Service for building/refreshing card-offer plans |
| **`.Tests`** | xUnit unit tests for the calculation and recommendation services |

---

## The reward decision algorithm

The heart of the project is `RewardCalculationService`, which evaluates a transaction against a card using a **fixed priority order** so results are deterministic and explainable:

1. **Merchant-specific offer** — highest priority; a live, merchant-matched promotion wins outright.
2. **Accelerated category** — if the spend category has a multiplier (e.g. 5x on dining), apply it over the base rate.
3. **Base reward rate** — the fallback when nothing special applies.
4. **Reward caps** — monthly/category caps clamp the earned value so results never exceed real-world limits.
5. **Milestone contribution** — tracks progress toward spend milestones and annual-fee-waiver targets, surfacing potential future value.

Every result carries a `reasoning` string explaining which rule fired, so a recommendation is never a black box.

The `TransactionRecommendationService` runs this calculation across all cards in a portfolio and **ranks** them, returning the single best card (or a ranked list) for a given transaction.

---

## Tech stack

| Layer | Technology |
|---|---|
| Runtime | .NET 8 / ASP.NET Core Web API |
| ORM | Entity Framework Core (code-first, migrations) |
| Database | PostgreSQL (default), SQLite, EF In-Memory — switchable via config |
| Logging | Serilog (console + daily rolling file) |
| API docs | Swagger / OpenAPI |
| Testing | xUnit |
| Front-end | Static SPA served from `wwwroot` |

---

## API surface

### Cards — `/api/cards`
| Method | Route | Purpose |
|---|---|---|
| `GET` | `/api/cards?userProfileId=` | List a user's cards |
| `GET` | `/api/cards/{id}` | Get a card by ID |
| `POST` | `/api/cards` | Add a card |
| `POST` | `/api/cards/onboarding` | Bulk-onboard cards |
| `POST` | `/api/cards/offers/refresh-plan` | Build an offer-refresh plan |
| `PUT` | `/api/cards/{id}` | Update a card |
| `DELETE` | `/api/cards/{id}` | Remove a card |
| `GET` | `/api/cards/portfolio/summary?userProfileId=` | Portfolio overview |

### Recommendations — `/api/recommendations`
| Method | Route | Purpose |
|---|---|---|
| `POST` | `/api/recommendations/best` | **Best single card** for a transaction |
| `POST` | `/api/recommendations/rank` | Ranked cards for a transaction |
| `POST` | `/api/recommendations/portfolio` | Rank an entire portfolio |

### Spending — `/api/spending`
| Method | Route | Purpose |
|---|---|---|
| `POST` | `/api/spending/transaction` | Record a transaction |
| `GET` | `/api/spending/card/{cardId}/summary` | Per-card spend summary |
| `GET` | `/api/spending/portfolio/summary?userProfileId=` | Portfolio spend summary |
| `GET` | `/api/spending/card/{cardId}/milestones` | Milestone progress |

### Users — `/api/users`
| Method | Route | Purpose |
|---|---|---|
| `GET` | `/api/users` | List profiles |
| `GET` | `/api/users/{id}` | Profile by ID |
| `GET` | `/api/users/email/{email}` | Profile by email |
| `POST` | `/api/users` | Create profile |

---

## Domain model

`UserProfile` owns many `CreditCard`s. Each `CreditCard` aggregates its reward structure:

- **`RewardCategory`** — accelerated earn categories and multipliers
- **`RewardOffer`** — time-bound, optionally merchant-specific promotions
- **`RewardCap`** — monthly/category limits on earned value
- **`Milestone`** — spend thresholds for bonuses and fee waivers
- **`Transaction`** — recorded spend used for summaries and milestone tracking

---

## Getting started

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- (Optional) PostgreSQL 14+ if using the default provider

### Quick start (SQLite — zero setup)
The included launcher runs against a local SQLite file, so no database server is required:

```bash
# Windows
start-app.bat
```

This starts the API and opens `http://localhost:5044`.

### Manual run
```bash
# from the solution root
dotnet restore
dotnet run --project CreditCardRewards.Api/CreditCardRewards.Api.csproj
```

### Choosing a database provider
The provider is selected at startup via configuration (see `Program.cs`):

| Setting | Effect |
|---|---|
| `UseInMemoryDatabase: true` | EF In-Memory — fastest, ephemeral, good for demos |
| `UseSqlite: true` | Local `rewards.db` file — zero-setup persistence |
| *(neither set)* | PostgreSQL via `ConnectionStrings:DefaultConnection` |

Set the Postgres connection string in `appsettings.json` (or, preferably, user-secrets / environment variables) before using the relational default. Migrations are applied automatically at startup for relational providers.

### API documentation
With the app running in Development, browse to `/swagger` for the full interactive OpenAPI explorer.

---

## Testing

```bash
dotnet test
```

The suite (xUnit) covers the core engine: reward calculation across merchant offers, accelerated categories, cap clamping, and milestone contribution, plus the multi-card ranking logic in the recommendation service.

---

## Roadmap / known limitations

- **Auth** — no authentication yet; `userProfileId` is passed explicitly. Add identity before any shared deployment.
- **CORS** — currently `AllowAnyOrigin`; scope to known origins for production.
- **Offer data** — offers are entered/refreshed via the `DataRefresh` plan rather than pulled from a live source; a real provider integration is a natural next step.
- **Optimization depth** — the engine optimizes per-transaction; portfolio-level strategy (e.g. steering spend to hit the *most valuable* milestone across cards) is a planned extension.

---

## License

No license file is currently included. Add one (e.g. MIT) if you intend others to reuse this code.
