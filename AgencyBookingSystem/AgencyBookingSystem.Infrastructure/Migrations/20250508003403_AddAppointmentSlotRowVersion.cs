using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgencyBookingSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAppointmentSlotRowVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "AppointmentSlots",
                type: "rowversion",
                rowVersion: true,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "AppointmentSlots");
        }
    }
} 