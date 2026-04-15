# SecureFleet

Web-based fleet monitoring system with secure vehicle tracking and route-based fuel cost estimation, built on a Zero Trust security model.

## Stack

- **Backend**: ASP.NET Core 8 Web API (C#) — JWT auth, RBAC, Npgsql + Dapper
- **Frontend**: Vanilla HTML / CSS / JavaScript + Leaflet (OpenStreetMap)
- **Database**: Supabase (PostgreSQL) with Row Level Security
- **Auth**: JWT (Supabase Auth compatible) with `admin` / `manager` / `driver` roles

## Project layout

```
SecureFleet/
├── backend/                 ASP.NET Core 8 Web API
│   ├── Controllers/         REST endpoints
│   ├── Models/              DTOs
│   ├── Services/            Data store + fuel calculator
│   ├── Program.cs           Auth, CORS, security headers
│   └── appsettings.json     Supabase config
├── frontend/                Static site
│   ├── index.html           Public landing page
│   ├── login.html           Sign in
│   ├── admin.html           Admin dashboard
│   ├── manager.html         Fleet manager dashboard
│   ├── driver.html          Driver view
│   ├── css/styles.css
│   └── js/                  config, auth, api, map, page scripts
└── database/
    └── schema.sql           Tables, RLS policies, seed data
```

## Quick start (demo mode — no setup required)

The frontend ships with a built-in mock data layer and the backend defaults to an in-memory store, so you can run everything locally without a database.

### 1. Frontend

Serve [frontend/](frontend/) with any static server:

```bash
cd frontend
python -m http.server 5500
# open http://localhost:5500
```

Sign in on the login page — pick any email/password and a role. You'll be routed to the matching dashboard.

### 2. Backend (optional in demo mode)

```bash
cd backend
dotnet run --urls http://localhost:5080
```

The frontend auto-detects API failures and falls back to mock data, so you can review the UI without the backend running. To force the API path, set `demoMode: false` in [frontend/js/config.js](frontend/js/config.js).

## Connecting to Supabase

1. Create a Supabase project. In the SQL editor, run [database/schema.sql](database/schema.sql).
2. In **Project Settings → API**, copy:
   - Project URL
   - `anon` public key
   - JWT secret (Settings → API → JWT Settings)
3. In **Project Settings → Database**, copy the connection string.
4. Update [backend/appsettings.json](backend/appsettings.json):
   - `Supabase:Url`, `Supabase:AnonKey`, `Supabase:JwtSecret`, `Supabase:ConnectionString`
   - Set `"DemoMode": false`
5. Update [frontend/js/config.js](frontend/js/config.js):
   - `apiBase`, `supabase.url`, `supabase.anonKey`
   - Set `demoMode: false`
6. Replace the demo `Auth` helper with Supabase Auth (the `Authorization` header is already wired through every API call).

## Zero Trust controls

- **Authenticate every request** — JWT bearer required on every `/api/*` call ([backend/Program.cs](backend/Program.cs))
- **Least privilege** — `AdminOnly`, `ManagerOrAdmin`, `AnyAuthenticated` policies on each endpoint
- **Defense in depth** — PostgreSQL Row Level Security policies enforce role-based data isolation even if the API is bypassed ([database/schema.sql](database/schema.sql))
- **Hardened transport** — HSTS, X-Frame-Options, X-Content-Type-Options, Referrer-Policy headers on every response
- **Strict CORS** — only configured origins accepted
- **Audit log table** — privileged actions recorded server-side

## API surface

| Method | Path | Roles |
| --- | --- | --- |
| GET | `/api/health` | public |
| GET | `/api/vehicles` | any |
| POST | `/api/vehicles` | manager, admin |
| PATCH | `/api/vehicles/{id}/status` | manager, admin |
| PATCH | `/api/vehicles/{id}/position` | any |
| DELETE | `/api/vehicles/{id}` | admin |
| GET | `/api/routes` | any |
| GET | `/api/fuel/prices` | any |
| POST | `/api/fuel/estimate` | any |
| GET | `/api/trips` | any |
| POST | `/api/trips` | manager, admin |

## Fuel cost formula

```
liters = distance_km / vehicle.fuel_efficiency_kmpl
cost   = liters * fuel_price.price_per_liter
```

Distances come from the `routes` table. No sensors required.

dotnet run --urls http://localhost:5080
