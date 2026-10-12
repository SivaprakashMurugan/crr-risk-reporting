# CRR Risk Reporting

Full-stack screening assignment implementation for a server-backed risk reporting grid.

## Stack

- Backend: ASP.NET Core 10 Web API, C# (`backend/src/Crr.Risk.Api`), targeting `net10.0`
- Frontend: React + TypeScript + Vite + AG Grid Community (`frontend`)
- Database: PostgreSQL 16 (`docker-compose.yml`)

## Repository layout

- `backend/` API and tests
- `frontend/` React app
- `data/risk_positions.csv` source dataset
- `DATA_NOTES.md` dataset profiling summary
- `DESIGN.md` architecture note

## Prerequisites

- Docker Desktop (or any local Docker engine)
- .NET SDK 10 (`10.0.401` validated). A user-local install (no admin) works:
  `& "$env:USERPROFILE\.dotnet\dotnet.exe"` after running the official
  `dotnet-install.ps1 -Channel 10.0 -InstallDir "$env:USERPROFILE\.dotnet"`.
- Node.js 20+

## 1) Start PostgreSQL

```powershell
Set-Location c:\Users\sivaprakash.murugan\Downloads\cargill-assessment\crr-risk-reporting
docker compose up -d
```

Database defaults:

- Host: `localhost`
- Port: `5433`
- DB: `crr_risk`
- User: `crr_user`
- Password: `crr_password`

## 2) Run backend API

```powershell
Set-Location c:\Users\sivaprakash.murugan\Downloads\cargill-assessment\crr-risk-reporting\backend\src\Crr.Risk.Api
dotnet run
```

If your PATH `dotnet` is older than 10.x, run with the user-local SDK:

```powershell
Set-Location c:\Users\sivaprakash.murugan\Downloads\cargill-assessment\crr-risk-reporting\backend\src\Crr.Risk.Api
& "$env:USERPROFILE\.dotnet\dotnet.exe" run
```

Startup behavior:

- Creates `risk_positions` table and indexes if missing
- Creates `pg_trgm` extension if missing
- Seeds CSV using `Npgsql` binary `COPY`
- Skips seed if table already has rows

Health check:

- `GET /health`

API endpoints:

- `POST /api/risk/positions`
- `GET /api/risk/positions/{tradeId}`
- `POST /api/risk/aggregations`

## 3) Run frontend

```powershell
Set-Location c:\Users\sivaprakash.murugan\Downloads\cargill-assessment\crr-risk-reporting\frontend
npm install
npm run dev
```

Frontend dev server: `http://localhost:5173`

Optional API base override:

- set `VITE_API_BASE_URL` (defaults to `http://localhost:5083`)

## Test and quality commands

Backend:

```powershell
Set-Location c:\Users\sivaprakash.murugan\Downloads\cargill-assessment\crr-risk-reporting\backend
dotnet build Crr.Risk.sln
dotnet test Crr.Risk.sln
```

Frontend:

```powershell
Set-Location c:\Users\sivaprakash.murugan\Downloads\cargill-assessment\crr-risk-reporting\frontend
npm run lint
npm run build
```

## CI pipeline (GitHub Actions)

The repository includes `.github/workflows/ci.yml` that runs on push and pull request:

- Backend: restore, build, test on .NET 10
- Frontend: `npm ci`, lint, build

This is implemented as the optional stretch-goal CI pipeline.

## Troubleshooting

- If `docker compose up` fails with pipe/engine errors, start Docker Desktop and retry.
- If frontend cannot reach API, confirm backend is running on `http://localhost:5083` and check `VITE_API_BASE_URL`.
