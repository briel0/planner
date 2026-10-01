# 04 — Physical model

Last step of the design process. Turns the [logical model](03-logical-model.md) into PostgreSQL:
exact types, constraints and indexes. Targets **PostgreSQL 18** (pinned in the Docker image), the first
version with native `uuidv7()`.

## Types

| Table | Column | Type | Null | Default | Notes |
|---|---|---|---|---|---|
| `users` | `id` | `uuid` | no | `uuidv7()` | |
| | `name` | `text` | no | | 1–100 characters |
| | `email` | `text` | no | | up to 254 characters (email standard limit) |
| | `created_at` | `timestamptz` | no | `now()` | |
| | `updated_at` | `timestamptz` | no | `now()` | |
| `categories` | `id` | `uuid` | no | `uuidv7()` | |
| | `user_id` | `uuid` | no | | |
| | `name` | `text` | no | | 1–100 characters |
| | `sort_order` | `integer` | no | | tab position, renumbered on reorder |
| | `created_at` | `timestamptz` | no | `now()` | |
| | `updated_at` | `timestamptz` | no | `now()` | |
| `cards` | `id` | `uuid` | no | `uuidv7()` | |
| | `category_id` | `uuid` | no | | |
| | `parent_id` | `uuid` | yes | | empty for root cards |
| | `title` | `text` | no | | 1–200 characters |
| | `color` | `text` | no | `'#e5e7eb'` | free color, `#rrggbb` lowercase hex |
| | `position_x` | `double precision` | no | `0` | |
| | `position_y` | `double precision` | no | `0` | |
| | `width` | `double precision` | no | `208` | 120–1200, in canvas pixels |
| | `height` | `double precision` | no | `64` | 48–900, in canvas pixels |
| | `layer` | `integer` | no | `0` | higher is in front, within the same board |
| | `content` | `jsonb` | yes | | Tiptap document |
| | `properties` | `jsonb` | no | `'{}'` | property catalog values |
| | `created_at` | `timestamptz` | no | `now()` | |
| | `updated_at` | `timestamptz` | no | `now()` | |

### Rationale

- **`text` + length `CHECK` instead of `varchar(n)`:** same performance in PostgreSQL; the limit becomes an
  explicit, easily changed rule. The same checks forbid empty or whitespace-only names and titles.
- **`double precision` for positions and sizes:** the canvas zooms, so cards can sit at fractional coordinates and have fractional sizes.
- **Size limits (120×48 to 1200×900):** below the minimum a short title no longer fits; above the maximum one card would hide most of the board. The defaults (208×64) are the size cards had before they could be resized.
- **`integer` for `sort_order` and `layer`:** "bring to front" is the board's max layer + 1; reordering tabs
  renumbers a user's few categories. Fractional ordering schemes would add complexity with no gain here.
- **`timestamptz` for instants:** stores an absolute moment (normalized to UTC) and converts to the reader's
  time zone. Plain `timestamp` has no time zone and cannot be compared safely across services.
- **Absent properties are not stored:** a card without a due date has no `due_on` key at all (not
  `"due_on": null`), so a note's properties are `{}` and the partial `due_on` index only holds cards that
  have a deadline.
- **`due_on` is a calendar day, not an instant:** stored in `properties` as `YYYY-MM-DD`, with no time or
  time zone. "Due on the 30th" means the 30th wherever the user is.
- **`jsonb` instead of `json`:** a binary format PostgreSQL can query and index; `json` only keeps the raw text.
- **Free color (`#rrggbb`):** users may pick any color. Hex is stored in lowercase so the same color always
  has the same representation. Since a fixed color does not adapt to light and dark themes, the frontend
  picks a readable text color (dark or light) for each card from the background's contrast.
  Rejected — a named palette (`blue`, `red`...): theme-friendly and readable by other systems, but limits
  users to the palette.

## Constraints

### Foreign keys and cascades

| Foreign key | References | On delete | On update |
|---|---|---|---|
| `categories.user_id` | `users.id` | cascade | — |
| `cards.category_id` | `categories.id` | cascade | — |
| `cards (parent_id, category_id)` | `cards (id, category_id)` | cascade | cascade |

- Deleting a user deletes their categories; deleting a category deletes its cards; deleting a card deletes
  its children, recursively — the cascade walks the whole subtree.
- **Same category as the parent** is enforced by the composite foreign key. `cards` declares
  `UNIQUE (id, category_id)` (redundant for uniqueness, since `id` is already unique, but required so a
  foreign key can reference the pair). The pair (my parent, my category) must exist as (id, category) of some
  card: the parent must exist *and* share my category. When `parent_id` is null, PostgreSQL skips the check
  (default `MATCH SIMPLE`), which is exactly the root-card case.
- `ON UPDATE CASCADE` on that key means moving a tree to another category only requires changing the root's
  `category_id`; the change propagates down to every descendant.
- **Implementation note:** a plain `parent_id → cards (id)` foreign key (on delete cascade) also exists. It is
  redundant with the composite key, but EF Core needs it to save parents before children. The composite key
  itself cannot be declared in EF Core, because EF Core forbids changing alternate-key values and a card's
  category does change; it is created with hand-written SQL in the migration.

### Uniqueness

- `users`: unique `email`. The API stores emails in lowercase, so uniqueness is case-insensitive.
- `categories`: unique index on `(user_id, lower(name))` — category names are unique per user regardless of
  letter case, while the name is stored as typed.

### Checks

- `users.name`, `categories.name`, `cards.title`: not empty, no leading or trailing spaces
  (`x = btrim(x)`), within the length limits in the types table.
- `users.email`: at most 254 characters.
- `cards.color`: matches `^#[0-9a-f]{6}$`.
- `cards.width` between 120 and 1200, `cards.height` between 48 and 900.
- `cards.properties`: always a JSON object (`jsonb_typeof(properties) = 'object'`).
- `cards.content`: null or a JSON object.

### Triggers

- **`updated_at`:** a trigger on every table sets `updated_at = now()` on each update, so no writer has to
  remember it.
- **No cycles:** a trigger on `cards` runs before any insert or update of `parent_id`. It walks up from the
  new parent to the root with a recursive query (`WITH RECURSIVE`) and rejects the write if the card itself
  appears on the path. The API translates the database error into a friendly message. Enforced in the
  database — not only in the API — because a cycle does not just make data wrong, it breaks the system
  (recursive queries and cascades would loop). Known limitation: two perfectly simultaneous opposite moves
  could both pass; negligible for a planner where each user edits only their own data, and solvable with
  a lock if it ever matters.

## Indexes

Indexes are derived from the queries the system actually runs. Each one speeds up reads but costs disk
space and slows down every write to its table, so none is created "just in case". Primary keys and unique
constraints create their indexes automatically; **foreign key columns are not indexed automatically** in
PostgreSQL, so they are covered explicitly.

A composite index `(a, b)` also serves queries that filter only by `a` (leftmost-prefix rule), but not
queries that filter only by `b`.

| Index | Serves |
|---|---|
| `users (email)` — unique, automatic | login and lookup by email |
| `categories (user_id, lower(name))` — unique | name uniqueness; listing a user's tabs (`WHERE user_id = ?`, by prefix); cascade from `users` |
| `cards (id, category_id)` — unique | target of the parent composite foreign key |
| `cards (category_id, parent_id)` | root cards of a board (`WHERE category_id = ? AND parent_id IS NULL`); cascade from `categories` (by prefix) |
| `cards (parent_id, category_id)` | children of a card (`WHERE parent_id = ?`, by prefix); cascades and the cycle trigger walking the tree |
| `cards ((properties ->> 'due_on')) WHERE properties ? 'due_on'` | cards due on a day or in a range. ISO dates (`YYYY-MM-DD`) sort correctly as text, so ranges like "this week" work. Partial: only cards that have a due date are indexed |

Not created yet, on purpose:

- **GIN index on `properties`** (generic index for any key, `@>` containment queries): added if queries on
  other properties become common.
- **`updated_at` indexes** for "what changed since X" synchronization: added when another tool starts
  syncing.

Every index is validated with `EXPLAIN ANALYZE` once there is real data.
