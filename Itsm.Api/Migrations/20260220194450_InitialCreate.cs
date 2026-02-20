using System;
using Itsm.Common.Models;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Itsm.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Computers",
                columns: table => new
                {
                    ComputerName = table.Column<string>(type: "text", nullable: false),
                    LastUpdatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Data = table.Column<Computer>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Computers", x => x.ComputerName);
                });

            migrationBuilder.CreateTable(
                name: "DiskUsageSnapshots",
                columns: table => new
                {
                    ComputerName = table.Column<string>(type: "text", nullable: false),
                    ScannedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Data = table.Column<DiskUsageSnapshot>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiskUsageSnapshots", x => x.ComputerName);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Computers");

            migrationBuilder.DropTable(
                name: "DiskUsageSnapshots");
        }
    }
}
