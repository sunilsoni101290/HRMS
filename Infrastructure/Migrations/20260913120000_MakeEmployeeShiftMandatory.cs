using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MakeEmployeeShiftMandatory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ======================================================
            // DATA SAFETY FIX-UP (must run BEFORE the NOT NULL constraint
            // below, or the ALTER COLUMN would fail against any existing
            // Employee row that still has a NULL Shift - see spec section 5:
            // "handle the migration safely before applying NOT NULL").
            //
            // No Employee row is deleted or has any other column touched.
            // The Shift assigned is always resolved from EXISTING Shift
            // master data (never a hard-coded Id):
            //
            //   1) that employee's own tenant's default Shift
            //      (Shifts.IsDefaultShift = 1 - "General Shift" is seeded
            //      this way, see Infrastructure/Data/DbSeeder.cs);
            //   2) if that tenant has no shift flagged default yet, any one
            //      of that tenant's own active shifts;
            //   3) last-resort, any shift at all in the database - kept only
            //      so this migration can never fail on data it cannot
            //      otherwise safely resolve; in practice every tenant
            //      already has a seeded "General Shift", so this step is
            //      not expected to affect any row.
            // ======================================================

            migrationBuilder.Sql(@"
UPDATE e
SET e.ShiftId = ds.Id
FROM Employees e
CROSS APPLY (
    SELECT TOP 1 s.Id
    FROM Shifts s
    WHERE s.TenantId = e.TenantId
      AND s.IsDefaultShift = 1
      AND s.IsActive = 1
      AND s.IsDeleted = 0
) ds
WHERE e.ShiftId IS NULL;
");

            migrationBuilder.Sql(@"
UPDATE e
SET e.ShiftId = anyShift.Id
FROM Employees e
CROSS APPLY (
    SELECT TOP 1 s.Id
    FROM Shifts s
    WHERE s.TenantId = e.TenantId
      AND s.IsActive = 1
      AND s.IsDeleted = 0
) anyShift
WHERE e.ShiftId IS NULL;
");

            migrationBuilder.Sql(@"
UPDATE e
SET e.ShiftId = (SELECT TOP 1 s.Id FROM Shifts s WHERE s.IsDeleted = 0)
FROM Employees e
WHERE e.ShiftId IS NULL
  AND EXISTS (SELECT 1 FROM Shifts s WHERE s.IsDeleted = 0);
");

            migrationBuilder.AlterColumn<string>(
                name: "ShiftId",
                table: "Employees",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ShiftId",
                table: "Employees",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");
        }
    }
}
