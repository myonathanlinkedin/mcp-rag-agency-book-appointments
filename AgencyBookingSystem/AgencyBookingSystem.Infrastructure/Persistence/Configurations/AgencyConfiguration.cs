using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class AgencyConfiguration : IEntityTypeConfiguration<Agency>
{
    public void Configure(EntityTypeBuilder<Agency> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.Email)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(e => e.MaxAppointmentsPerDay)
            .IsRequired();

        builder.Property(e => e.RequiresApproval)
            .IsRequired();

        builder.Property(e => e.IsApproved)
            .IsRequired();

        // Configure relationships
        builder.HasMany(e => e.AgencyUsers)
            .WithOne()
            .HasForeignKey(u => u.AgencyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Slots)
            .WithOne()
            .HasForeignKey(s => s.AgencyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Holidays)
            .WithOne()
            .HasForeignKey(h => h.AgencyId)
            .OnDelete(DeleteBehavior.Cascade);

        // Add unique index on Email
        builder.HasIndex(e => e.Email)
            .IsUnique()
            .HasDatabaseName("IX_Agency_Email");
    }
} 