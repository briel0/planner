using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Planner.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    name = table.Column<string>(type: "text", nullable: false),
                    email = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                    table.CheckConstraint("ck_users_email", "length(email) <= 254");
                    table.CheckConstraint("ck_users_name", "name = btrim(name) AND length(name) BETWEEN 1 AND 100");
                });

            migrationBuilder.CreateTable(
                name: "categories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_categories", x => x.id);
                    table.CheckConstraint("ck_categories_name", "name = btrim(name) AND length(name) BETWEEN 1 AND 100");
                    table.ForeignKey(
                        name: "fk_categories_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "cards",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    parent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    title = table.Column<string>(type: "text", nullable: false),
                    color = table.Column<string>(type: "text", nullable: false, defaultValue: "#e5e7eb"),
                    layer = table.Column<int>(type: "integer", nullable: false),
                    content = table.Column<string>(type: "jsonb", nullable: true),
                    properties = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb"),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    position_x = table.Column<double>(type: "double precision", nullable: false),
                    position_y = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cards", x => x.id);
                    table.CheckConstraint("ck_cards_color", "color ~ '^#[0-9a-f]{6}$'");
                    table.CheckConstraint("ck_cards_content", "content IS NULL OR jsonb_typeof(content) = 'object'");
                    table.CheckConstraint("ck_cards_properties", "jsonb_typeof(properties) = 'object'");
                    table.CheckConstraint("ck_cards_title", "title = btrim(title) AND length(title) BETWEEN 1 AND 200");
                    table.ForeignKey(
                        name: "fk_cards_cards_parent_id",
                        column: x => x.parent_id,
                        principalTable: "cards",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_cards_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_cards_category_id_parent_id",
                table: "cards",
                columns: new[] { "category_id", "parent_id" });

            migrationBuilder.CreateIndex(
                name: "ix_cards_parent_id_category_id",
                table: "cards",
                columns: new[] { "parent_id", "category_id" });

            migrationBuilder.CreateIndex(
                name: "ix_users_email",
                table: "users",
                column: "email",
                unique: true);

            // ----------------------------------------------------------------------------------------------
            // Escrito à mão: partes do modelo físico (docs/data-modeling/04-physical-model.md) que o EF Core
            // não gera a partir do mapeamento.
            // ----------------------------------------------------------------------------------------------

            // Mesma categoria do pai: (parent_id, category_id) precisa existir como (id, category_id) de algum
            // cartão. ON UPDATE CASCADE propaga a troca de categoria da raiz para todos os descendentes.
            migrationBuilder.Sql("""
                ALTER TABLE cards ADD CONSTRAINT ak_cards_id_category_id UNIQUE (id, category_id);

                ALTER TABLE cards ADD CONSTRAINT fk_cards_parent_same_category
                    FOREIGN KEY (parent_id, category_id) REFERENCES cards (id, category_id)
                    ON DELETE CASCADE ON UPDATE CASCADE;
                """);

            // Nome de categoria único por usuário, ignorando maiúsculas/minúsculas.
            migrationBuilder.Sql("""
                CREATE UNIQUE INDEX ux_categories_user_id_lower_name ON categories (user_id, lower(name));
                """);

            // Cartões por prazo (dia ou intervalo). Parcial: só cartões que têm prazo entram no índice.
            migrationBuilder.Sql("""
                CREATE INDEX ix_cards_due_on ON cards ((properties ->> 'due_on')) WHERE properties ? 'due_on';
                """);

            // updated_at = agora, em toda alteração de linha.
            migrationBuilder.Sql("""
                CREATE FUNCTION set_updated_at() RETURNS trigger LANGUAGE plpgsql AS $$
                BEGIN
                    NEW.updated_at = now();
                    RETURN NEW;
                END
                $$;

                CREATE TRIGGER trg_users_updated_at BEFORE UPDATE ON users
                    FOR EACH ROW EXECUTE FUNCTION set_updated_at();
                CREATE TRIGGER trg_categories_updated_at BEFORE UPDATE ON categories
                    FOR EACH ROW EXECUTE FUNCTION set_updated_at();
                CREATE TRIGGER trg_cards_updated_at BEFORE UPDATE ON cards
                    FOR EACH ROW EXECUTE FUNCTION set_updated_at();
                """);

            // Sem ciclos: sobe do novo pai até a raiz; se o próprio cartão aparecer no caminho, recusa.
            // O nome ck_cards_no_cycle no erro permite à API traduzi-lo para o código card.cycle.
            migrationBuilder.Sql("""
                CREATE FUNCTION prevent_card_cycle() RETURNS trigger LANGUAGE plpgsql AS $$
                BEGIN
                    IF NEW.parent_id IS NULL THEN
                        RETURN NEW;
                    END IF;

                    IF EXISTS (
                        WITH RECURSIVE ancestors (id, parent_id) AS (
                            SELECT id, parent_id FROM cards WHERE id = NEW.parent_id
                            UNION
                            SELECT c.id, c.parent_id FROM cards c JOIN ancestors a ON c.id = a.parent_id
                        )
                        SELECT 1 FROM ancestors WHERE id = NEW.id
                    ) THEN
                        RAISE EXCEPTION 'card % cannot be moved inside itself or one of its descendants', NEW.id
                            USING ERRCODE = 'check_violation', CONSTRAINT = 'ck_cards_no_cycle';
                    END IF;

                    RETURN NEW;
                END
                $$;

                CREATE TRIGGER trg_cards_no_cycle BEFORE INSERT OR UPDATE OF parent_id ON cards
                    FOR EACH ROW EXECUTE FUNCTION prevent_card_cycle();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Os triggers, os índices e a FK composta somem junto com as tabelas; as funções, não.
            migrationBuilder.Sql("DROP TRIGGER trg_cards_no_cycle ON cards;");
            migrationBuilder.Sql("DROP FUNCTION prevent_card_cycle();");
            migrationBuilder.Sql("""
                DROP TRIGGER trg_users_updated_at ON users;
                DROP TRIGGER trg_categories_updated_at ON categories;
                DROP TRIGGER trg_cards_updated_at ON cards;
                DROP FUNCTION set_updated_at();
                """);

            migrationBuilder.DropTable(
                name: "cards");

            migrationBuilder.DropTable(
                name: "categories");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
