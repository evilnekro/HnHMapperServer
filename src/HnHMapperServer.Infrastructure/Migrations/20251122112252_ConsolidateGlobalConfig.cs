using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HnHMapperServer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ConsolidateGlobalConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Consolidate 'prefix' config from all tenants into a single global config
            // Strategy:
            // 1. Create __global__ tenant if it doesn't exist
            // 2. Take the first non-null prefix value we find
            // 3. Create global config entry
            // 4. Delete tenant-specific entries
            //
            // NOTE: Using suppressTransaction to avoid EF Core migration locks in SQLite

            migrationBuilder.Sql(
                "INSERT INTO Tenants (Id, Name, StorageQuotaMB, CurrentStorageMB, CreatedAt, IsActive) " +
                "SELECT '__global__', 'Global System Settings', 0, 0, datetime('now'), 1 " +
                "WHERE NOT EXISTS (SELECT 1 FROM Tenants WHERE Id = '__global__');",
                suppressTransaction: true);

            migrationBuilder.Sql(
                "INSERT INTO Config (Key, TenantId, Value) " +
                "SELECT 'prefix', '__global__', Value " +
                "FROM Config " +
                "WHERE Key = 'prefix' " +
                "LIMIT 1;",
                suppressTransaction: true);

            migrationBuilder.Sql(
                "DELETE FROM Config WHERE Key = 'prefix' AND TenantId != '__global__';",
                suppressTransaction: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Rollback: Delete global prefix and restore tenant-specific ones
            // Note: We can't perfectly restore the old state, but we can create a default entry for each tenant

            migrationBuilder.Sql(
                "INSERT INTO Config (Key, TenantId, Value) " +
                "SELECT 'prefix', t.Id, (SELECT Value FROM Config WHERE Key = 'prefix' AND TenantId = '__global__') " +
                "FROM Tenants t " +
                "WHERE t.Id != '__global__' " +
                "  AND NOT EXISTS (SELECT 1 FROM Config WHERE Key = 'prefix' AND TenantId = t.Id);",
                suppressTransaction: true);

            migrationBuilder.Sql(
                "DELETE FROM Config WHERE Key = 'prefix' AND TenantId = '__global__';",
                suppressTransaction: true);

            migrationBuilder.Sql(
                "DELETE FROM Tenants " +
                "WHERE Id = '__global__' " +
                "  AND NOT EXISTS (SELECT 1 FROM Config WHERE TenantId = '__global__');",
                suppressTransaction: true);
        }
    }
}
