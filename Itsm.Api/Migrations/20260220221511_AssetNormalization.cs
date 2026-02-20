using System;
using Itsm.Common.Models;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Itsm.Api.Migrations
{
    /// <inheritdoc />
    public partial class AssetNormalization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Agents",
                columns: table => new
                {
                    HardwareUuid = table.Column<string>(type: "text", nullable: false),
                    ComputerName = table.Column<string>(type: "text", nullable: false),
                    DisplayName = table.Column<string>(type: "text", nullable: false),
                    AgentVersion = table.Column<string>(type: "text", nullable: false),
                    IsConnected = table.Column<bool>(type: "boolean", nullable: false),
                    LastSeenUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FirstSeenUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Agents", x => x.HardwareUuid);
                });

            migrationBuilder.CreateTable(
                name: "Assets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    SerialNumber = table.Column<string>(type: "text", nullable: true),
                    AssignedUser = table.Column<string>(type: "text", nullable: true),
                    Location = table.Column<string>(type: "text", nullable: true),
                    PurchaseDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    WarrantyExpiry = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Cost = table.Column<decimal>(type: "numeric", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    Source = table.Column<string>(type: "text", nullable: false),
                    DiscoveredByAgent = table.Column<string>(type: "text", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assets", x => x.Id);
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

            migrationBuilder.CreateTable(
                name: "GpuModels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Vendor = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GpuModels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MonitorModels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Manufacturer = table.Column<string>(type: "text", nullable: false),
                    ModelName = table.Column<string>(type: "text", nullable: false),
                    WidthPixels = table.Column<int>(type: "integer", nullable: true),
                    HeightPixels = table.Column<int>(type: "integer", nullable: true),
                    DiagonalInches = table.Column<double>(type: "double precision", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonitorModels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PrinterModels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Manufacturer = table.Column<string>(type: "text", nullable: true),
                    Model = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrinterModels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SoftwareTitles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SoftwareTitles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UsbProducts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VendorId = table.Column<string>(type: "text", nullable: false),
                    ProductId = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Manufacturer = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsbProducts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Computers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ComputerName = table.Column<string>(type: "text", nullable: false),
                    ModelName = table.Column<string>(type: "text", nullable: false),
                    HardwareUuid = table.Column<string>(type: "text", nullable: false),
                    LoggedInUser = table.Column<string>(type: "text", nullable: false),
                    ChassisType = table.Column<string>(type: "text", nullable: false),
                    CpuBrand = table.Column<string>(type: "text", nullable: false),
                    CpuCores = table.Column<int>(type: "integer", nullable: false),
                    CpuArchitecture = table.Column<string>(type: "text", nullable: false),
                    TotalMemoryBytes = table.Column<long>(type: "bigint", nullable: false),
                    OsDescription = table.Column<string>(type: "text", nullable: false),
                    OsVersion = table.Column<string>(type: "text", nullable: true),
                    OsBuildNumber = table.Column<string>(type: "text", nullable: true),
                    Hostname = table.Column<string>(type: "text", nullable: false),
                    FirewallEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    FirewallStealth = table.Column<bool>(type: "boolean", nullable: true),
                    EncryptionEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    EncryptionMethod = table.Column<string>(type: "text", nullable: true),
                    BatteryPresent = table.Column<bool>(type: "boolean", nullable: false),
                    BatteryCharge = table.Column<double>(type: "double precision", nullable: true),
                    BatteryCycles = table.Column<int>(type: "integer", nullable: true),
                    BatteryHealth = table.Column<double>(type: "double precision", nullable: true),
                    BatteryCharging = table.Column<bool>(type: "boolean", nullable: true),
                    BatteryCondition = table.Column<string>(type: "text", nullable: true),
                    LastBootUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Uptime = table.Column<string>(type: "text", nullable: true),
                    LastUpdatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Computers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Computers_Assets_Id",
                        column: x => x.Id,
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Monitors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MonitorModelId = table.Column<Guid>(type: "uuid", nullable: false),
                    ManufactureYear = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Monitors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Monitors_Assets_Id",
                        column: x => x.Id,
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Monitors_MonitorModels_MonitorModelId",
                        column: x => x.MonitorModelId,
                        principalTable: "MonitorModels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NetworkPrinters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PrinterModelId = table.Column<Guid>(type: "uuid", nullable: true),
                    IpAddress = table.Column<string>(type: "text", nullable: false),
                    MacAddress = table.Column<string>(type: "text", nullable: true),
                    FirmwareVersion = table.Column<string>(type: "text", nullable: true),
                    PageCount = table.Column<int>(type: "integer", nullable: true),
                    TonerBlackPercent = table.Column<int>(type: "integer", nullable: true),
                    TonerCyanPercent = table.Column<int>(type: "integer", nullable: true),
                    TonerMagentaPercent = table.Column<int>(type: "integer", nullable: true),
                    TonerYellowPercent = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NetworkPrinters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NetworkPrinters_Assets_Id",
                        column: x => x.Id,
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NetworkPrinters_PrinterModels_PrinterModelId",
                        column: x => x.PrinterModelId,
                        principalTable: "PrinterModels",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UsbPeripherals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UsbProductId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsbPeripherals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UsbPeripherals_Assets_Id",
                        column: x => x.Id,
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UsbPeripherals_UsbProducts_UsbProductId",
                        column: x => x.UsbProductId,
                        principalTable: "UsbProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ComputerGpus",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ComputerId = table.Column<Guid>(type: "uuid", nullable: false),
                    GpuModelId = table.Column<Guid>(type: "uuid", nullable: false),
                    VramBytes = table.Column<long>(type: "bigint", nullable: true),
                    DriverVersion = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComputerGpus", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComputerGpus_Computers_ComputerId",
                        column: x => x.ComputerId,
                        principalTable: "Computers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ComputerGpus_GpuModels_GpuModelId",
                        column: x => x.GpuModelId,
                        principalTable: "GpuModels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ComputerSoftware",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ComputerId = table.Column<Guid>(type: "uuid", nullable: false),
                    SoftwareTitleId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<string>(type: "text", nullable: false),
                    InstallDate = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComputerSoftware", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComputerSoftware_Computers_ComputerId",
                        column: x => x.ComputerId,
                        principalTable: "Computers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ComputerSoftware_SoftwareTitles_SoftwareTitleId",
                        column: x => x.SoftwareTitleId,
                        principalTable: "SoftwareTitles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Disks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ComputerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Format = table.Column<string>(type: "text", nullable: false),
                    TotalBytes = table.Column<long>(type: "bigint", nullable: false),
                    FreeBytes = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Disks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Disks_Computers_ComputerId",
                        column: x => x.ComputerId,
                        principalTable: "Computers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NetworkInterfaces",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ComputerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    MacAddress = table.Column<string>(type: "text", nullable: false),
                    IpAddresses = table.Column<string[]>(type: "text[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NetworkInterfaces", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NetworkInterfaces_Computers_ComputerId",
                        column: x => x.ComputerId,
                        principalTable: "Computers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ComputerGpus_ComputerId",
                table: "ComputerGpus",
                column: "ComputerId");

            migrationBuilder.CreateIndex(
                name: "IX_ComputerGpus_GpuModelId",
                table: "ComputerGpus",
                column: "GpuModelId");

            migrationBuilder.CreateIndex(
                name: "IX_Computers_ComputerName",
                table: "Computers",
                column: "ComputerName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Computers_HardwareUuid",
                table: "Computers",
                column: "HardwareUuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ComputerSoftware_ComputerId",
                table: "ComputerSoftware",
                column: "ComputerId");

            migrationBuilder.CreateIndex(
                name: "IX_ComputerSoftware_SoftwareTitleId",
                table: "ComputerSoftware",
                column: "SoftwareTitleId");

            migrationBuilder.CreateIndex(
                name: "IX_Disks_ComputerId",
                table: "Disks",
                column: "ComputerId");

            migrationBuilder.CreateIndex(
                name: "IX_GpuModels_Name_Vendor",
                table: "GpuModels",
                columns: new[] { "Name", "Vendor" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MonitorModels_Manufacturer_ModelName",
                table: "MonitorModels",
                columns: new[] { "Manufacturer", "ModelName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Monitors_MonitorModelId",
                table: "Monitors",
                column: "MonitorModelId");

            migrationBuilder.CreateIndex(
                name: "IX_NetworkInterfaces_ComputerId",
                table: "NetworkInterfaces",
                column: "ComputerId");

            migrationBuilder.CreateIndex(
                name: "IX_NetworkPrinters_PrinterModelId",
                table: "NetworkPrinters",
                column: "PrinterModelId");

            migrationBuilder.CreateIndex(
                name: "IX_PrinterModels_Manufacturer_Model",
                table: "PrinterModels",
                columns: new[] { "Manufacturer", "Model" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SoftwareTitles_Name",
                table: "SoftwareTitles",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UsbPeripherals_UsbProductId",
                table: "UsbPeripherals",
                column: "UsbProductId");

            migrationBuilder.CreateIndex(
                name: "IX_UsbProducts_VendorId_ProductId",
                table: "UsbProducts",
                columns: new[] { "VendorId", "ProductId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Agents");

            migrationBuilder.DropTable(
                name: "ComputerGpus");

            migrationBuilder.DropTable(
                name: "ComputerSoftware");

            migrationBuilder.DropTable(
                name: "Disks");

            migrationBuilder.DropTable(
                name: "DiskUsageSnapshots");

            migrationBuilder.DropTable(
                name: "Monitors");

            migrationBuilder.DropTable(
                name: "NetworkInterfaces");

            migrationBuilder.DropTable(
                name: "NetworkPrinters");

            migrationBuilder.DropTable(
                name: "UsbPeripherals");

            migrationBuilder.DropTable(
                name: "GpuModels");

            migrationBuilder.DropTable(
                name: "SoftwareTitles");

            migrationBuilder.DropTable(
                name: "MonitorModels");

            migrationBuilder.DropTable(
                name: "Computers");

            migrationBuilder.DropTable(
                name: "PrinterModels");

            migrationBuilder.DropTable(
                name: "UsbProducts");

            migrationBuilder.DropTable(
                name: "Assets");
        }
    }
}
