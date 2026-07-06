# Deploying OpenMoneyWeb to GCP

## Phase 1 — Create a GCP Account & Project

1. Go to [cloud.google.com](https://cloud.google.com) → **Get started for free**
2. Sign in with your Google account and enter billing details (you get $300 free credit)
3. In the [GCP Console](https://console.cloud.google.com), click the project dropdown → **New Project**
4. Name it `openmoneyweb` and note the **Project ID** (may have a number suffix like `openmoneyweb-123456`)

---

## Phase 2 — Enable Required APIs

In the Console go to **APIs & Services → Enable APIs** and enable these (or run the gcloud commands after installing the CLI in Phase 3):

- Cloud Run API
- Cloud Build API
- Container Registry API
- Cloud SQL Admin API
- Secret Manager API

---

## Phase 3 — Install Google Cloud CLI

1. Download from [cloud.google.com/sdk/docs/install](https://cloud.google.com/sdk/docs/install) — choose the Windows installer
2. After install, open a new terminal and run:

```powershell
gcloud init
# Follow prompts: log in, select your project
gcloud config set project YOUR_PROJECT_ID

# Enable all required APIs at once
gcloud services enable run.googleapis.com cloudbuild.googleapis.com containerregistry.googleapis.com sqladmin.googleapis.com secretmanager.googleapis.com
```

---

## Phase 4 — Create a Cloud SQL (MySQL) Instance

```powershell
# Create the instance (~5 minutes)
gcloud sql instances create openmoney-db `
  --database-version=MYSQL_8_0 `
  --tier=db-f1-micro `
  --region=northamerica-northeast1

# Create the database
gcloud sql databases create openmoney --instance=openmoney-db

# Create a user
gcloud sql users create appuser --instance=openmoney-db --password=CHOOSE_A_STRONG_PASSWORD
```

Note your **connection name** — it looks like `YOUR_PROJECT_ID:northamerica-northeast1:openmoney-db`. Find it with:
```powershell
gcloud sql instances describe openmoney-db --format="value(connectionName)"
```

---

## Phase 5 — Store the Connection String in Secret Manager

```powershell
# The connection string for Cloud SQL uses the Unix socket path when running on Cloud Run
$conn = "Server=/cloudsql/YOUR_PROJECT_ID:northamerica-northeast1:openmoney-db;Database=openmoney;User=appuser;Password=CHOOSE_A_STRONG_PASSWORD;"

echo $conn | gcloud secrets create db-connection-string --data-file=-
```

---

## Phase 6 — Update `cloudrun.yaml`

Replace the two placeholder values in `cloudrun.yaml`:

```yaml
run.googleapis.com/cloudsql-instances: YOUR_PROJECT_ID:northamerica-northeast1:openmoney-db
image: gcr.io/YOUR_PROJECT_ID/openmoneyweb:latest
```

---

## Phase 7 — Grant Cloud Run Permissions

Cloud Run needs permission to access Cloud SQL and Secret Manager:

```powershell
# Get the Cloud Run service account
$SA = "$(gcloud projects describe YOUR_PROJECT_ID --format='value(projectNumber)')-compute@developer.gserviceaccount.com"

gcloud projects add-iam-policy-binding YOUR_PROJECT_ID --member="serviceAccount:$SA" --role="roles/cloudsql.client"
gcloud projects add-iam-policy-binding YOUR_PROJECT_ID --member="serviceAccount:$SA" --role="roles/secretmanager.secretAccessor"
```

---

## Phase 8 — Build & Deploy

```powershell
# From the project root — this builds the image, pushes it, and deploys to Cloud Run
gcloud builds submit --config cloudbuild.yaml
```

This runs the existing `cloudbuild.yaml` which handles everything automatically. First run takes ~10 minutes.

---

## Phase 9 — Run Database Migrations

Your app uses EF Core, so migrations need to run once after first deploy. The easiest way is a one-off Cloud Run job:

```powershell
gcloud run jobs create migrate-db `
  --image=gcr.io/YOUR_PROJECT_ID/openmoneyweb:latest `
  --region=northamerica-northeast1 `
  --set-cloudsql-instances=YOUR_PROJECT_ID:northamerica-northeast1:openmoney-db `
  --set-secrets=ConnectionStrings__DefaultConnection=db-connection-string:latest `
  --command="dotnet" `
  --args="OpenMoneyWeb.Api.dll,--migrate"

gcloud run jobs execute migrate-db --region=northamerica-northeast1
```

> **Note:** This requires the app to support a `--migrate` CLI argument to run `dbContext.Database.Migrate()` and exit. See Phase 9a below.

### Phase 9a — Add migration support to the API

In `Program.cs`, add before `app.Run()`:

```csharp
if (args.Contains("--migrate"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
    return;
}
```

---

## Phase 10 — Get Your Live URL

```powershell
gcloud run services describe openmoneyweb --region=northamerica-northeast1 --format="value(status.url)"
```

---

## Cost Summary (approximate)

| Service | Free tier | Beyond free |
|---|---|---|
| Cloud Run | 2M requests/month | ~$0.40/million |
| Cloud SQL `db-f1-micro` | None | ~$7/month |
| Container Registry | 0.5 GB | $0.10/GB |

The biggest ongoing cost will be Cloud SQL. Stop the instance when not in use to save money:

```powershell
gcloud sql instances patch openmoney-db --activation-policy=NEVER   # stop
gcloud sql instances patch openmoney-db --activation-policy=ALWAYS  # start
```
