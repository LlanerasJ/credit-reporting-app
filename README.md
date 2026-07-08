# Credit Reporting App

A demo credit reporting system built as three layers: a WPF desktop client, an
ASP.NET Core Web API, and a SQL Server database accessed through EF Core. It
covers the kind of workflow an internal tool at a data furnisher might handle:
look up a customer, view their credit report, and generate or parse Metro 2
reporting files.

```
WPF client  ->  ASP.NET Core Web API  ->  SQL Server LocalDB (EF Core)
(MVVM)          (controllers/services/repositories)
                        |
                        +-- Metro 2 module (generate/parse fixed-width files)
```

All data is synthetic. Names are made up, SSNs use the unassigned 900-xx-xxxx
range, and only a SHA-256 hash plus the last four digits of any SSN are stored.
Don't use this against real data.

## Projects

| Project | Purpose |
|---|---|
| `CreditReporting.Api` | Web API: auth, customers, accounts, credit reports, Metro 2 |
| `CreditReporting.Wpf` | WPF desktop client (MVVM via CommunityToolkit.Mvvm) |
| `CreditReporting.Shared` | DTOs shared between the API and client |
| `CreditReporting.Tests` | xUnit tests for the Metro 2 writer/parser/validator and masking |

## Running it

You'll need the .NET 8 SDK and SQL Server LocalDB (`sqllocaldb info` should list
`MSSQLLocalDB`; it ships with Visual Studio).

```powershell
# Start the API. It creates and seeds the CreditReportingDemo database on first run.
dotnet run --project CreditReporting.Api --launch-profile http
# Swagger is at http://localhost:5006/swagger

# In another terminal, start the desktop client.
dotnet run --project CreditReporting.Wpf
```

Log in with `analyst` / `Demo123!` or `admin` / `Admin123!`. To find seeded
customers, search a name like `man` (Testman) or `sample`, or an SSN last-4 in
the `1000`-`1014` range.

## How it fits together

**Credit report.** `CreditReportService` pulls a customer's accounts, 24 months
of payment history, inquiries, and score history into a single report DTO and
computes the summary totals. Every read is written to the `AuditLog` table with
the user, timestamp, customer, and stated purpose.

**Masking.** EF entities never leave the API. The DTO mapper replaces SSNs with
`***-**-1234` and account numbers with `****5678` (see `Services/Masking.cs` and
`Services/DtoMapper.cs`).

**Auth.** `POST /api/auth/login` returns an 8-hour JWT; the rest of the endpoints
require it. Passwords are stored as PBKDF2 hashes.

**Metro 2 module** (`CreditReporting.Api/Metro2/`):

- `Metro2FieldAttribute` marks each property with its 1-based start position,
  length, and type. `Metro2FixedWidth` reads those attributes to serialize and
  parse the fixed-width records, so the layout lives with the model.
- `Metro2Writer` emits the header, one base record per account (with optional J1
  and K1 appended segments), and a trailer with computed control totals.
- `Metro2Parser` reads a file back and reports structural problems instead of
  throwing on them.
- `Metro2Validator` checks required fields, code ranges, and dates, separating
  errors from warnings.
- Endpoints: `POST /api/metro2/preview` (dry run), `POST /api/metro2/generate`
  (returns the file, and refuses if there are validation errors), and
  `POST /api/metro2/parse` (upload and parse).
- The field layout is a synthetic approximation of the public Metro 2 structure,
  documented in [docs/METRO2-FORMAT.md](docs/METRO2-FORMAT.md). It does not
  reproduce the CDIA's licensed field tables.

**WPF client.** A login window opens the main window, which has tabs for search,
credit report, Metro 2 export, and Metro 2 import. The logic sits in the
ViewModels; code-behind only handles view wiring (focus, the password box, and
double-click). `Services/ApiService.cs` wraps the HTTP calls and turns failures
into readable messages.

## Notes

- The database is created with `EnsureCreated()` and a deterministic seeder
  rather than migrations, which keeps the demo self-contained. Drop the
  `CreditReportingDemo` LocalDB database to reset it.
- The JWT signing key sits in `appsettings.json` for convenience. A real
  deployment would keep it in user-secrets or a key vault.
- The API runs over plain HTTP on `localhost:5006` so the client needs no config.
