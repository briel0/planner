# 01 — Miniworld

First step of the classic database design process (requirements gathering).
Describes, in plain language, the slice of reality the database represents.
The following steps (conceptual, logical and physical models) are derived from this text.

## Description

The planner is used by **many users**. Each user has a **name** and an **email**, and only sees their own data. A new user starts with **no categories**.

Each user organizes their cards into **categories**. Each category belongs to **exactly one user**, has a **name** — which cannot repeat among the categories of the same user (different users may have categories with the same name) — and shows up as a tab in the footer of the screen, in an **order** chosen by the user. Each category is displayed as **a single board**: a free canvas.

Boards contain **cards**. Every card has a **title**, a **color**, a **position** on the board it sits on, a **size** (width and height, adjusted by the user by dragging the card's corner, within minimum and maximum limits) and, optionally, rich-text **content**. When opened, **every card displays its own board**, which may contain other cards, **with no depth limit**. Every card is in **exactly one place**: on the board of a category or on the board of another card. A card can **never** be inside itself, nor inside a card that is inside it. Cards on the same board may **overlap**, and the user chooses which one stays in front (the card's **layer**).

Every card has the same shape: there are no card types. Besides its content, a card may carry **properties**: typed pieces of information about the card that the user can filter and query, like a **due date**. Properties come from a **catalog defined by the system**, which grows over time; a card only carries the properties that apply to it. What makes a card a "task" is simply having task-like properties (for example, a due date or a done flag).

Richer material — text, tables, images — belongs to the card's **content**, not to its properties. The rule of thumb: if you want to filter or query by it, it is a property; otherwise, it is content.

The system records **when** each user, category and card was **created** and **last updated**, so other tools can find out what changed since a given moment.

When a category is deleted, **all of its cards are deleted with it**. When a card is deleted, **everything inside its board is deleted too**, at every level.

## Out of scope for now

- Sharing categories or cards between users.
- More than one board per category (the miniworld says "initially a single board"; the logical model should make this change easy).
- Password and login (depend on the authentication decision).
- Properties defined by the user (as in Notion). The catalog is fixed by the system for now, but stored so that user-defined properties can be added later.
- Image file storage (content can reference images; where the files live is decided later).
