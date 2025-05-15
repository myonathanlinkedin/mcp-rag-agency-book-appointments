using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class AppointmentSlotConfiguration : IEntityTypeConfiguration<AppointmentSlot>
{
    public void Configure(EntityTypeBuilder<AppointmentSlot> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.StartTime)
            .IsRequired();

        builder.Property(e => e.EndTime)
            .IsRequired();

        builder.Property(e => e.Capacity)
            .IsRequired();

        builder.Property(e => e.AgencyId)
            .IsRequired();

        builder.Property(e => e.RowVersion)
            .IsRowVersion();

        // Configure relationships
        builder.HasOne<Agency>()
            .WithMany(a => a.Slots)
            .HasForeignKey(s => s.AgencyId)
            .OnDelete(DeleteBehavior.Cascade);

        // Add unique index on AgencyId and StartTime combination
        builder.HasIndex(e => new { e.AgencyId, e.StartTime })
            .IsUnique()
            .HasDatabaseName("IX_AppointmentSlot_AgencyId_StartTime");
    }
} 