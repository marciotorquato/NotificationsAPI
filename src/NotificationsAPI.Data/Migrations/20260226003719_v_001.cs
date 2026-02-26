using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NotificationsAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class v_001 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    Destinatario = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Assunto = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Corpo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    DataCriacao = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DataEnvio = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TentativasEnvio = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    ErroMensagem = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_DataCriacao",
                table: "Notifications",
                column: "DataCriacao");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_Status",
                table: "Notifications",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UsuarioId",
                table: "Notifications",
                column: "UsuarioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Notifications");
        }
    }
}
