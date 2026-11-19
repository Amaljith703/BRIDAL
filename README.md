# Makeup Art

Makeup Art is a beauty studio storefront and admin workspace. The backend is .NET 10 with EF Core and SQL Server; the client is an Angular 22 standalone app.

## Local setup

1. Copy `.env.example` to `.env` and replace `JWT__SIGNINGKEY` with a random value of at least 64 characters. Keep `.env` out of version control.
2. Start SQL Server: `docker compose up -d sql` (the host port is `127.0.0.1:1434`).
3. Start the API: `dotnet run --project src/MakeupArt.Api`.
4. In another terminal, install and start the client: `cd client`, `npm ci`, then `npm start`.
5. Open `http://localhost:4200`. The API listens on `http://localhost:5080`; its development OpenAPI page is at `/swagger`.

The API applies EF migrations and seeds the development catalog when it starts. Database files persist in the `makeupart-sqldata` Docker volume; SQL backup files are mounted at `database/backups`. The admin Backups screen creates and verifies SQL Server backups before listing them for download. Run `./scripts/verify-backup-restore.ps1 -BackupFileName <file>.bak` to verify a backup by restoring it to a temporary scratch database and checking the seeded tables and products.

## Local AI

The API calls Ollama at `AI__BASEURL` using `AI__MODEL`. The configured model is `qwen3.5:latest`. This machine's model files are stored at `D:\AI\Model`; keep `OLLAMA_MODELS` pointed at that directory, start Ollama, and confirm `ollama list` shows Qwen 3.5 before testing AI features. The assistant uses seeded catalog data as its grounding context and returns a local fallback response when Ollama is unavailable.

## Payments and credentials

Razorpay is the production gateway when its key ID and secret are configured. Stripe and PayPal remain unavailable adapters until credentials are provided. The mock provider is for development only. Keep provider credentials and mail settings in `.env` or user secrets; never commit them.

## Seed media

Curated seed photos, a short studio clip, and instrumental tracks are stored in the API's `wwwroot/media/seed` folder. Source pages and license links are listed in [MEDIA_ATTRIBUTION.md](MEDIA_ATTRIBUTION.md). Replace these demo assets and credits through the Admin media library when adopting a final brand identity.

## Checks

- Client build: `cd client; npm run build`
- Client tests: `cd client; npm test -- --watch=false`
- .NET build and tests: `dotnet build MakeupArt.slnx`, then `dotnet test MakeupArt.slnx`
