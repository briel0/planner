# API design

The HTTP API is the planner's contract with every client: the Angular app, scripts and other tools of the
ecosystem. The database and the backend's internal code are implementation details.

This document defines the **conventions** every endpoint follows. It does not list the endpoints:
**endpoints are created as they are needed**, following these conventions, and the concrete, authoritative
list is the **OpenAPI specification generated from the code**. Paths below are examples that illustrate
the conventions, not a promise that those endpoints exist.

## Style

- **REST, resource-oriented:** paths are nouns (`/cards`), HTTP methods are the verbs. No verbs in paths
  (`/moveCard`).
- **Collection and item:** `/cards` is a collection (`GET` lists, `POST` creates); `/cards/{id}` is an item
  (`GET` reads, `PATCH` changes, `DELETE` removes).
- **Stateless:** every request carries everything needed to serve it, including who is asking.

## Naming

- **Paths:** English, plural, lowercase, hyphen-separated when needed (`/categories`, `/due-cards`).
- **JSON fields:** `camelCase` (`categoryId`, `createdAt`). The database keeps `snake_case`; DTOs translate.
- **Property keys** follow the same rule in the API (`dueOn`), while the database stores them as `due_on`.

## Routes

**Shallow nesting.**

- **Items live at a flat, permanent URL**, no matter where they sit in the tree. A card is always
  `/cards/{id}`; its URL never changes when it is moved. Moving is changing its parent or category with
  `PATCH`.
- **Collections are nested exactly one level under their direct owner.** Example: the cards on a category's
  board are `/categories/{id}/cards`; the cards on a card's own board are `/cards/{id}/children`. Creating
  through the owner's collection gives the new resource its context (a card created under a parent takes the
  parent's category), so contradictory requests cannot be expressed.
- **Queries that cross owners live on the top-level collection**, with filters in the query string. Example:
  `/cards?dueFrom=2026-09-28&dueTo=2026-10-04` finds cards across all categories and depths.

Rejected:

- **Full nesting** (`/categories/c1/cards/k1/cards/k5/...`): unbounded URLs, a card cannot be reached from its
  id alone, and its URL would change whenever it is moved.
- **Everything flat with filters** (`/cards?parentId=null`, `POST /cards` with `categoryId` and `parentId` in
  the body): awkward "null" filters and room for contradictory requests.

## Users and access

- **The user is implicit:** no path contains a user id. Every request is made on behalf of the authenticated
  caller, and a collection like `/categories` always means "mine".
- **Other users' resources answer `404 Not Found`**, not `403 Forbidden`, so their existence is never
  revealed.

## Representations

- **Lists return summaries; items return the full representation.** The endpoint's purpose picks the
  representation — the client does not choose. Summaries leave out heavy fields that lists do not need.
  Example: card summaries omit `content`, so a board with 30 cards does not download 30 rich-text documents
  (over-fetching), while `GET /cards/{id}` returns the card with its content in one request (no
  under-fetching). Create and update responses return the full representation.
- **Representations are shaped for clients, not copied from tables.** Example: a card's position is
  `{ "x": 120.5, "y": 80 }`, although the database splits it into two columns.
- **References to parents are always included** (`categoryId`, `parentId`), so results of cross-cutting
  queries can tell where each resource lives.
- **Instants** use ISO 8601 in UTC (`2026-09-28T03:12:45Z`); **calendar days** use `YYYY-MM-DD`.

Example — a card summary:

```json
{
  "id": "0192f6a3-7b1c-7cc2-9a4e-5d3b8f1e2a07",
  "categoryId": "0192f6a0-5c2e-7a11-8b3f-2e4d6c8a1b90",
  "parentId": null,
  "title": "Lista 3",
  "color": "#3b82f6",
  "position": { "x": 120.5, "y": 80 },
  "layer": 2,
  "properties": { "dueOn": "2026-09-30", "done": false },
  "createdAt": "2026-09-28T03:12:45Z",
  "updatedAt": "2026-09-28T03:40:02Z"
}
```

Not now — **field selection** (`?include=content`, GraphQL-style queries): no client needs it yet.

## Errors

Every error response uses **Problem Details** ([RFC 9457](https://www.rfc-editor.org/rfc/rfc9457)), with
`Content-Type: application/problem+json`. ASP.NET Core produces this format natively.

```json
{
  "type": "https://planner.dev/errors/card-cycle",
  "title": "Card cannot be moved inside itself",
  "status": 409,
  "detail": "Card 'Física Q.' cannot be moved into 'Exerc. 5', which is inside it.",
  "instance": "/cards/0192f6a3-7b1c-7cc2-9a4e-5d3b8f1e2a07",
  "code": "card.cycle"
}
```

| Field | Role |
|---|---|
| `type` | Identifies the kind of error (a URI that can document it) |
| `title` | Short, fixed summary of that kind, for humans |
| `status` | The HTTP status code, repeated for logs |
| `detail` | Explanation of this specific occurrence, for humans |
| `instance` | The resource involved |
| `code` | **Extension.** Stable, machine-readable error code (`card.cycle`, `validation`, `category.name-taken`) |

- **`code` is part of the contract; texts are not.** Programs branch on `code`, never on `title` or `detail`,
  so messages can be reworded freely without breaking anyone. Every `code` is documented and never changes
  meaning.
- **Messages are in English.** Clients show their own messages chosen by `code`: the Angular app displays
  Portuguese text, and other languages need no API change.
- **Validation errors** (`400`, `code: "validation"`) add an `errors` object mapping each invalid field to its
  messages, so clients can show every problem next to its field at once:

```json
{
  "type": "https://planner.dev/errors/validation",
  "title": "One or more fields are invalid",
  "status": 400,
  "code": "validation",
  "errors": {
    "title": ["Title must not be empty."],
    "properties.dueOn": ["Must be a date in YYYY-MM-DD format."]
  }
}
```

- **Status codes:** `400` invalid request · `401` not authenticated · `404` not found or not yours ·
  `409` conflicts with the current state (cycles, duplicate category names) · `500` server failure
  (details are logged, never exposed to the client).

## Evolving the contract

Once a client depends on an endpoint, changes must be **backward compatible**:

| Change | Safe? |
|---|---|
| Add a field to a response | yes |
| Add an optional parameter or a new endpoint | yes |
| Rename or remove a field | no |
| Change a field's type or meaning | no |
| Make something required that was optional | no |

Until the first client depends on the API, anything can change freely. How to ship an unavoidable breaking
change (versioning) is decided later.
