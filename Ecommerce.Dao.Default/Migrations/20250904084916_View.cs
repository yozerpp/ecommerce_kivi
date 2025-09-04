using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Ecommerce.Dao.Default.Migrations;
[Migration("20250904084916_View")]
[DbContext(typeof(DefaultDbContext))]
public class View : Initialize{
    protected override void Up(MigrationBuilder migrationBuilder) {
        ViewMigrations.Up(migrationBuilder);
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        ViewMigrations.Down(migrationBuilder);
    }
}