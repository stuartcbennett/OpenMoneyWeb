# OpenMoneyWeb

A web-based port of the [OpenMoney](https://github.com/stuartcbennett/OpenMoney) personal finance desktop app, built with **ASP.NET Core 10 Web API** and **React**, designed to run as a single container on **Google Cloud Run**.

## Architecture

```
OpenMoneyWeb.Core    — Domain models (Account, Investment, Transaction, PriceHistory)
                       and services (ReturnCalculator, TransactionImporter)
OpenMoneyWeb.Data    — EF Core 9 + Pomelo MySQL; repositories for each entity
OpenMoneyWeb.Api     — ASP.NET Core 10 Web API; serves React SPA from wwwroot/
OpenMoneyWeb.Client  — React 18 + TypeScript + Material UI + Recharts
```

In production the Dockerfile builds both projects and packages them into a single container image. The .NET API serves the compiled React app as static files and handles all `/api/*` routes.

**Pages:** Portfolio (expandable account/investment tree with returns) · Accounts (transaction list with add/edit/delete) · Reports (net worth over time, account value, price history, performance table) · Settings (manage accounts & investments, import legacy transactions)

---

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 22+](https://nodejs.org)
- MySQL 8 (local or via Docker)

---

## Running locally

### Option A — Docker Compose (recommended)

```bash
docker compose up --build
```

The app is at `http://localhost:8080`. MySQL data persists in the `mysql-data` Docker volume.

### Option B — Run API and React dev server separately

1. **Start MySQL** and ensure the database `openmoney` exists.

2. **Configure the connection string** in `OpenMoneyWeb.Api/appsettings.json`:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=localhost;Database=openmoney;User=root;Password=;"
   }
   ```

3. **Run the API** (auto-applies migrations on first start):
   ```bash
   dotnet run --project OpenMoneyWeb.Api
   ```

4. **Run the React dev server** (in a second terminal):
   ```bash
   cd OpenMoneyWeb.Client
   npm install
   npm run dev
   ```
   Open `http://localhost:5173`. API calls are proxied to `http://localhost:5000`.

---

## Database migrations

EF Core migrations are applied automatically when the app starts. To create a new migration after changing models:

```bash
# From the solution root
dotnet ef migrations add <MigrationName> \
  --project OpenMoneyWeb.Data \
  --startup-project OpenMoneyWeb.Api
```

---

## Deploying to GCP Cloud Run

### One-time setup

```bash
# Create a Cloud SQL MySQL 8 instance named "openmoney"
gcloud sql instances create openmoney --database-version=MYSQL_8_0 --region=northamerica-northeast1 --tier=db-f1-micro

# Create the database
gcloud sql databases create openmoney --instance=openmoney

# Store the connection string in Secret Manager
gcloud secrets create db-connection-string --data-file=- <<< \
  "Server=/cloudsql/PROJECT_ID:northamerica-northeast1:openmoney;Database=openmoney;User=root;Password=YOUR_PASSWORD;"

# Enable Cloud Build and Cloud Run APIs
gcloud services enable cloudbuild.googleapis.com run.googleapis.com
```

### Deploy

```bash
gcloud builds submit --config cloudbuild.yaml
```

Cloud Build builds the image, pushes it to Container Registry, and deploys to Cloud Run. The connection string is injected from Secret Manager at deploy time.

### Manual deploy (skip Cloud Build)

```bash
# Build and push the image
docker build -t gcr.io/PROJECT_ID/openmoneyweb .
docker push gcr.io/PROJECT_ID/openmoneyweb

# Deploy to Cloud Run
gcloud run deploy openmoneyweb \
  --image gcr.io/PROJECT_ID/openmoneyweb \
  --region northamerica-northeast1 \
  --platform managed \
  --allow-unauthenticated \
  --set-env-vars "ConnectionStrings__DefaultConnection=Server=/cloudsql/PROJECT_ID:northamerica-northeast1:openmoney;Database=openmoney;User=root;Password=YOUR_PASSWORD;" \
  --add-cloudsql-instances PROJECT_ID:northamerica-northeast1:openmoney
```

---

## Testing

| Project | Tests | Approach |
|---|---|---|
| `OpenMoneyWeb.Core.Tests` | 14 tests | Pure unit tests, no mocks — exercises `ReturnCalculator` and `TransactionImporter` directly |
| `OpenMoneyWeb.Data.Tests` | 13 tests | Integration tests against SQLite in-memory — covers repository CRUD, `GetLatestPrices`, and silent no-ops on missing ids |
| `OpenMoneyWeb.Api.Tests` | 23 tests | Full HTTP integration tests via `WebApplicationFactory` with SQLite replacing MySQL — covers controllers, services, and end-to-end import/portfolio/reports flows |

```bash
dotnet test OpenMoneyWeb.slnx
```

---

## API reference

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/portfolio` | Full portfolio with returns |
| GET/POST/PUT/DELETE | `/api/accounts` | Manage accounts |
| GET | `/api/accounts/{id}/transactions` | Transactions for an account |
| GET/POST/PUT/DELETE | `/api/investments` | Manage investments |
| POST | `/api/investments/{id}/price` | Add a price history entry |
| POST/PUT/DELETE | `/api/transactions` | Manage transactions |
| POST | `/api/import` | Import legacy CSV transactions |
| GET | `/api/reports/networth` | Net worth over time |
| GET | `/api/reports/account/{id}/value` | Account value over time |
| GET | `/api/reports/investments/performance` | Performance table |
| GET | `/api/reports/investments/{id}/pricehistory` | Price history for an investment |
