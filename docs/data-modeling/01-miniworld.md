# 01 — Miniworld

First step of the classic database design process (requirements gathering).
Describes, in plain language, the slice of reality the database represents.
The following steps (conceptual, logical and physical models) are derived from this text.

## Description

The planner is used by **many users**. Each user has a **name** and an **email**, and only sees their own data. A new user starts with **no categories**.

Each user organizes their cards into **categories**. Each category belongs to **exactly one user**, has a **name** — which cannot repeat among the categories of the same user (different users may have categories with the same name) — and shows up as a tab in the footer of the screen, in an **order** chosen by the user. Each category is displayed as **a single board**: a free canvas.

Boards contain **cards**. Every card has a **title**, a **color**, a **position** on the board it sits on and, optionally, rich-text **content**. When opened, **every card displays its own board**, which may contain other cards, **with no depth limit**. Every card is in **exactly one place**: on the board of a category or on the board of another card. A card can **never** be inside itself, nor inside a card that is inside it. Cards on the same board may **overlap**, and the user chooses which one stays in front (the card's **layer**).

A card can be of **a single type**. A **task** is a card that optionally has a **date**. A card with no specific type is a **note**: a generic card that holds information in its title and content.

When a category is deleted, **all of its cards are deleted with it**. When a card is deleted, **everything inside its board is deleted too**, at every level.

## Specialization classification

| Property | Value | Meaning |
|---|---|---|
| Disjointness | **Disjoint** | A card has at most one type. |
| Completeness | **Partial** | A card may have no specific type (the note). |

## Out of scope for now

- Sharing categories or cards between users.
- More than one board per category (the miniworld says "initially a single board"; the logical model should make this change easy).
- Password and login (depend on the authentication decision).
- Card types other than task and note.
