# Copilot instructions for AgoraProfe

## Build, test, and lint commands

Run commands from the repository root (`AgoraProfe`):

- Build all projects in the solution:
  - `dotnet build Agora.sln`
- Run all tests:
  - `dotnet test TestAgora\TestAgora.csproj`
- Run a single test method:
  - `dotnet test TestAgora\TestAgora.csproj --filter "FullyQualifiedName~TestAgora.UnitTestGenericService.TestGetAllUsuarios"`

No dedicated lint command is configured in this repository or in CI workflows.

## High-level architecture

This is a .NET 8 multi-project solution centered on training/course management (`Capacitaciones`, `Inscripciones`, `Usuarios`, etc.).

- `Backend` is an ASP.NET Core Web API + EF Core (Pomelo MySQL) project.
  - `DataContext/AgoraContext.cs` defines `DbSet<>`, seed data, and global query filters for soft-delete.
  - Controllers expose CRUD + custom endpoints (`abiertas`, `futuras`, `deleteds`, `restore/{id}`).
- `Service` is the shared domain/application layer used by every client:
  - Shared models/enums/interfaces.
  - HTTP client services (`GenericService<T>`, `CapacitacionService`, `InscripcionService`) that call the API.
- `WebBlazor` (Blazor WebAssembly), `Desktop` (WinForms), and `MovilApp` (.NET MAUI) all reference `Service` and consume the same models/services.
- `TestAgora` is an xUnit test project that exercises `Service` API clients.
- CI/CD in `.github/workflows` deploys:
  - `Backend\Backend.csproj` to Azure Web App `apiagora`.
  - `WebBlazor\WebBlazor.csproj` to Azure Web App `agora20`.

## Key conventions in this codebase

- **Endpoint naming is coupled to model type names** through `Service/Utils/ApiEndPoints.cs` + `GenericService<T>`:
  - `ApiEndpoints.GetEndpoint(typeof(T).Name)` must resolve for every model used with `GenericService<T>`.
  - If you add a new model/service pair, add its endpoint mapping there.
- **Soft-delete is the default behavior**:
  - Entities use `IsDeleted`.
  - `AgoraContext` applies global `HasQueryFilter(p => !p.IsDeleted)` for all core entities.
  - Controllers expose `GET .../deleteds` and `PUT .../restore/{id}` with `IgnoreQueryFilters()` when needed.
- **Filtering convention** for list endpoints is `?filter=...` (see `GetAllAsync` and controllers).
- **Capacitacion updates require explicit relationship handling** in `CapacitacionesController.PutCapacitacion`:
  - Existing code uses `TryAttach`, compares existing/new child collections, and nulls navigation references before add/remove.
  - Preserve this pattern when changing nested update behavior to avoid EF tracking/duplicate insert issues.
- **Service base URL is resource-driven**:
  - `GenericService<T>` currently uses `Service.Properties.Resources.UrlApiLocal` (localhost) by default.
  - `UrlApi` (Azure) exists in resources; switching environments currently requires code/resource changes.
- **Current tests are integration-style against the API**, not isolated unit tests:
  - They instantiate service clients directly and expect seeded/available backend data.
  - Keep this in mind when modifying tests or API defaults.
