# MovieNight Database Hosting: Docker and Kubernetes

This folder contains PostgreSQL hosting templates for local learning and dev use.

## Why this works for MovieNight

The API uses EF Core with Npgsql and reads its DB connection string from:

- `ConnectionStrings:DefaultConnection`

So any reachable PostgreSQL instance will work, including Docker and Kubernetes.

## 1) Docker Compose (quick start)

Compose file:

- `deploy/docker-compose.postgres.yml`

Start PostgreSQL:

```powershell
docker compose -f deploy/docker-compose.postgres.yml up -d
```

Set API connection string in current PowerShell session:

```powershell
$env:ConnectionStrings__DefaultConnection = "Host=localhost;Port=5432;Database=movienight;Username=movienight;Password=movienight_pw;Include Error Detail=true"
```

Run the app:

```powershell
dotnet run --project AppHost
```

## 2) Kubernetes (single-node or local cluster)

Manifest:

- `deploy/k8s/postgres.yaml`

Apply resources:

```powershell
kubectl apply -f deploy/k8s/postgres.yaml
```

For a local API process (not in cluster), port-forward DB to localhost:

```powershell
kubectl -n movienight port-forward svc/movienight-postgres 5432:5432
```

Then use the same local connection string:

```powershell
$env:ConnectionStrings__DefaultConnection = "Host=localhost;Port=5432;Database=movienight;Username=movienight;Password=movienight_pw;Include Error Detail=true"
```

If ApiService runs inside Kubernetes, use service DNS host:

```text
Host=movienight-postgres;Port=5432;Database=movienight;Username=movienight;Password=movienight_pw
```

## 3) Important app note

The current solution does not appear to run EF migrations automatically on startup.

Before testing full app behavior, ensure schema exists by running your migration strategy (for example via `dotnet ef database update` from the project that owns your DbContext) or by adding startup migration execution in ApiService.

## 4) Cleanup

Docker:

```powershell
docker compose -f deploy/docker-compose.postgres.yml down -v
```

Kubernetes:

```powershell
kubectl delete -f deploy/k8s/postgres.yaml
```
