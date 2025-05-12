using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Collections.Generic;

public class AgencyUserConfiguration : IEntityTypeConfiguration<AgencyUser>
{
    public void Configure(EntityTypeBuilder<AgencyUser> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Email)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(e => e.FullName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.AgencyId)
            .IsRequired();

        // Configure the roles collection using backing field
        builder.Property<List<string>>("roles")
            .HasColumnName("Roles")
            .IsRequired()
            .HasDefaultValue(new List<string> { CommonModelConstants.Role.Agency });

        // Configure relationship with Agency
        builder.HasOne<Agency>()
            .WithMany(a => a.AgencyUsers)
            .HasForeignKey(au => au.AgencyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
} 