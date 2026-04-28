using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MediCore.Migrations
{
    /// <inheritdoc />
    public partial class SyncModelV2 : Migration
    {
        // No-op: the FK + index already exist in the database from
        // SwapPatientUserRelation. This migration only exists to keep the EF
        // model snapshot in sync with OnModelCreating after the relationship
        // was declared explicitly.

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) { }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) { }
    }
}
