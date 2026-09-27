# Backend architecture

The backend (`api/`) follows **Clean Architecture**, in a lean form: the layer structure and its dependency
rule, a rich domain model at the center, and no extra libraries or abstractions until a concrete need
appears.

## The dependency rule

Source code dependencies point **inwards**. Inner layers know nothing about outer layers: business rules
never depend on the database, HTTP or frameworks. The compiler enforces it, because each layer is a
separate project that only references the layers inside it.

```
Api  ──────────►  Application  ──────────►  Domain
Infrastructure ─►  Application
```

## Projects

| Project | Responsibility | References |
|---|---|---|
| `Planner.Domain` | Entities with their rules (`Card`, `Category`, `User`), value objects, domain errors. Pure C#. | nothing |
| `Planner.Application` | Use cases ("create card", "move card"): load entities, call their methods, save. Defines the interfaces it needs from the outside (e.g., persistence). | `Domain` |
| `Planner.Infrastructure` | Technical details: EF Core `DbContext`, entity mappings, migrations, PostgreSQL. Implements the Application's interfaces. | `Application`, `Domain` |
| `Planner.Api` | HTTP: endpoints, request/response DTOs, Problem Details, OpenAPI. Translates between HTTP and use cases, and wires everything with dependency injection. | `Application`, `Infrastructure` (composition only) |

Tests mirror the projects (`Planner.Domain.Tests`, ...). The domain is tested with plain objects: no
database, no HTTP.

## Rich domain model

- Entities **encapsulate** their data: properties have public getters and private setters.
- State only changes through methods that **enforce the invariants** (`card.ChangeColor(...)`,
  `card.MoveInto(...)`), so an invalid entity cannot exist anywhere in the code.
- Rejected — **anemic model** (entities as property bags, rules in services): procedural code in disguise;
  nothing stops other code from bypassing the rules.

Defense in depth: the domain is the **first** line of defense; database constraints and triggers remain the
**last** one.

## Lean on purpose

Not used until a concrete need appears: MediatR/command handlers, AutoMapper, CQRS, generic repositories.
Each would add ceremony without solving a current problem.

Rejected alternatives:

- **Layered architecture with an anemic model** (Controllers → Services → Repositories): see above.
- **Vertical slices + rich domain** (two projects, slices using `DbContext` directly): less ceremony, but
  Clean Architecture was chosen to practice the structure used professionally in .NET teams.
