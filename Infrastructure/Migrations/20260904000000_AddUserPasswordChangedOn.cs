using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    /// <remarks>
    /// Idempotent by convention with AddEsslMonthlyDeviceLogTableSupport/
    /// AddEsslIntegrationSettings (IF NOT EXISTS-guarded raw SQL rather than
    /// a typed AddColumn call, and no accompanying .Designer.cs/model-snapshot
    /// update - see those migrations' own remarks for why). Adds
    /// Users.PasswordChangedOn, stamped whenever PasswordHash is set (see
    /// Domain/Entities/User.cs and the corresponding call sites in
    /// AuthService, EmployeeService and UserService) so the Security / User
    /// (Index) list can show when each user's password was last changed.
    /// See the equivalent, more heavily commented
    /// "add PasswordChangedOn column to Users.sql" at the repo root for the
    /// deployment-facing version of this same statement.
    /// </remarks>
    public partial class AddUserPasswordChangedOn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Users') AND name = 'PasswordChangedOn')
    ALTER TABLE [dbo].[Users] ADD [PasswordChangedOn] DATETIME2 NULL;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Users') AND name = 'PasswordChangedOn')
    ALTER TABLE [dbo].[Users] DROP COLUMN [PasswordChangedOn];
");
        }
    }
}
