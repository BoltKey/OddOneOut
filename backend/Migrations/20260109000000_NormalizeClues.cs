using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OddOneOut.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeClues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: For each set of duplicate clues (same CardSetId, same lowercase clue),
            // keep the game with the most guesses (or earliest if tied) as the canonical one.
            // Move ClueGivers from duplicates to the canonical game.
            migrationBuilder.Sql(@"
                WITH duplicates AS (
                    SELECT 
                        ""Id"",
                        ""CardSetId"",
                        LOWER(""Clue"") as lower_clue,
                        ROW_NUMBER() OVER (
                            PARTITION BY ""CardSetId"", LOWER(""Clue"") 
                            ORDER BY (SELECT COUNT(*) FROM ""Guesses"" WHERE ""GameId"" = ""Games"".""Id"") DESC, ""CreatedAt"" ASC
                        ) as rn
                    FROM ""Games""
                    WHERE ""Clue"" IS NOT NULL
                ),
                canonical AS (
                    SELECT ""Id"" as canonical_id, ""CardSetId"", lower_clue
                    FROM duplicates
                    WHERE rn = 1
                ),
                to_merge AS (
                    SELECT d.""Id"" as duplicate_id, c.canonical_id
                    FROM duplicates d
                    JOIN canonical c ON d.""CardSetId"" = c.""CardSetId"" AND d.lower_clue = c.lower_clue
                    WHERE d.rn > 1
                )
                -- Move ClueGivers from duplicate games to canonical games (ignore conflicts)
                INSERT INTO ""GameClueGivers"" (""GameId"", ""UserId"", ""ClueGivenAt"")
                SELECT tm.canonical_id, gcg.""UserId"", gcg.""ClueGivenAt""
                FROM ""GameClueGivers"" gcg
                JOIN to_merge tm ON gcg.""GameId"" = tm.duplicate_id
                ON CONFLICT (""GameId"", ""UserId"") DO NOTHING;
            ");

            // Step 2: Move Guesses from duplicate games to canonical games
            migrationBuilder.Sql(@"
                WITH duplicates AS (
                    SELECT 
                        ""Id"",
                        ""CardSetId"",
                        LOWER(""Clue"") as lower_clue,
                        ROW_NUMBER() OVER (
                            PARTITION BY ""CardSetId"", LOWER(""Clue"") 
                            ORDER BY (SELECT COUNT(*) FROM ""Guesses"" WHERE ""GameId"" = ""Games"".""Id"") DESC, ""CreatedAt"" ASC
                        ) as rn
                    FROM ""Games""
                    WHERE ""Clue"" IS NOT NULL
                ),
                canonical AS (
                    SELECT ""Id"" as canonical_id, ""CardSetId"", lower_clue
                    FROM duplicates
                    WHERE rn = 1
                ),
                to_merge AS (
                    SELECT d.""Id"" as duplicate_id, c.canonical_id
                    FROM duplicates d
                    JOIN canonical c ON d.""CardSetId"" = c.""CardSetId"" AND d.lower_clue = c.lower_clue
                    WHERE d.rn > 1
                )
                UPDATE ""Guesses""
                SET ""GameId"" = tm.canonical_id
                FROM to_merge tm
                WHERE ""Guesses"".""GameId"" = tm.duplicate_id;
            ");

            // Step 3: Clear CurrentGame references to duplicate games
            migrationBuilder.Sql(@"
                WITH duplicates AS (
                    SELECT 
                        ""Id"",
                        ""CardSetId"",
                        LOWER(""Clue"") as lower_clue,
                        ROW_NUMBER() OVER (
                            PARTITION BY ""CardSetId"", LOWER(""Clue"") 
                            ORDER BY (SELECT COUNT(*) FROM ""Guesses"" WHERE ""GameId"" = ""Games"".""Id"") DESC, ""CreatedAt"" ASC
                        ) as rn
                    FROM ""Games""
                    WHERE ""Clue"" IS NOT NULL
                ),
                to_delete AS (
                    SELECT ""Id"" as duplicate_id
                    FROM duplicates
                    WHERE rn > 1
                )
                UPDATE ""AspNetUsers""
                SET ""CurrentGameId"" = NULL
                WHERE ""CurrentGameId"" IN (SELECT duplicate_id FROM to_delete);
            ");

            // Step 4: Delete ClueGivers for duplicate games
            migrationBuilder.Sql(@"
                WITH duplicates AS (
                    SELECT 
                        ""Id"",
                        ""CardSetId"",
                        LOWER(""Clue"") as lower_clue,
                        ROW_NUMBER() OVER (
                            PARTITION BY ""CardSetId"", LOWER(""Clue"") 
                            ORDER BY (SELECT COUNT(*) FROM ""Guesses"" WHERE ""GameId"" = ""Games"".""Id"") DESC, ""CreatedAt"" ASC
                        ) as rn
                    FROM ""Games""
                    WHERE ""Clue"" IS NOT NULL
                ),
                to_delete AS (
                    SELECT ""Id"" as duplicate_id
                    FROM duplicates
                    WHERE rn > 1
                )
                DELETE FROM ""GameClueGivers""
                WHERE ""GameId"" IN (SELECT duplicate_id FROM to_delete);
            ");

            // Step 5: Delete duplicate games
            migrationBuilder.Sql(@"
                WITH duplicates AS (
                    SELECT 
                        ""Id"",
                        ""CardSetId"",
                        LOWER(""Clue"") as lower_clue,
                        ROW_NUMBER() OVER (
                            PARTITION BY ""CardSetId"", LOWER(""Clue"") 
                            ORDER BY (SELECT COUNT(*) FROM ""Guesses"" WHERE ""GameId"" = ""Games"".""Id"") DESC, ""CreatedAt"" ASC
                        ) as rn
                    FROM ""Games""
                    WHERE ""Clue"" IS NOT NULL
                )
                DELETE FROM ""Games""
                WHERE ""Id"" IN (SELECT ""Id"" FROM duplicates WHERE rn > 1);
            ");

            // Step 6: Normalize all remaining clues to lowercase
            migrationBuilder.Sql(@"
                UPDATE ""Games""
                SET ""Clue"" = LOWER(""Clue"")
                WHERE ""Clue"" IS NOT NULL AND ""Clue"" <> LOWER(""Clue"");
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Cannot reverse lowercase normalization - clues stay lowercase
            // The merge of duplicate games is also irreversible
        }
    }
}
