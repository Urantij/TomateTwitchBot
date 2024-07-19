using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TomateTwitchBot.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TwitchId = table.Column<string>(type: "TEXT", nullable: false),
                    LastSeenUsername = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Timeouts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Text = table.Column<string>(type: "TEXT", nullable: false),
                    Roll = table.Column<double>(type: "REAL", nullable: false),
                    KillerId = table.Column<int>(type: "INTEGER", nullable: false),
                    VictimId = table.Column<int>(type: "INTEGER", nullable: false),
                    Date = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Timeouts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Timeouts_Users_KillerId",
                        column: x => x.KillerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Timeouts_Users_VictimId",
                        column: x => x.VictimId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Timeouts_KillerId",
                table: "Timeouts",
                column: "KillerId");

            migrationBuilder.CreateIndex(
                name: "IX_Timeouts_VictimId",
                table: "Timeouts",
                column: "VictimId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Timeouts");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
