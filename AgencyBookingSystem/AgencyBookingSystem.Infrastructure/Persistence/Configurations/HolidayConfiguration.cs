using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class HolidayConfiguration : IEntityTypeConfiguration<Holiday>
{
    public void Configure(EntityTypeBuilder<Holiday> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Date)
            .IsRequired();

        builder.Property(e => e.Reason)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.AgencyId)
            .IsRequired();

        // Configure relationships
        builder.HasOne<Agency>()
            .WithMany(a => a.Holidays)
            .HasForeignKey(h => h.AgencyId)
            .OnDelete(DeleteBehavior.Cascade);

        // Add unique index on AgencyId and Date combination
        builder.HasIndex(e => new { e.AgencyId, e.Date })
            .IsUnique()
            .HasDatabaseName("IX_Holiday_AgencyId_Date");
    }
} 