using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.Date)
            .IsRequired();

        builder.Property(e => e.Status)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(e => e.Token)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.AgencyId)
            .IsRequired();

        builder.Property(e => e.AgencyUserId)
            .IsRequired();

        // Configure relationships
        builder.HasOne(e => e.AgencyUser)
            .WithMany()
            .HasForeignKey(e => e.AgencyUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Add unique index on Token
        builder.HasIndex(e => e.Token)
            .IsUnique()
            .HasDatabaseName("IX_Appointment_Token");
    }
} 