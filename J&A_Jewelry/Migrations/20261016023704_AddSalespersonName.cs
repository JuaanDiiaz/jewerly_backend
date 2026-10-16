using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace J_A_Jewelry.Migrations
{
    /// <inheritdoc />
    public partial class AddSalespersonName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "salespersonName",
                table: "SalesOrderHeader",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "salespersonName",
                table: "SalesOrderHeader");
        }
    }
}
