using Microsoft.EntityFrameworkCore;
using System.Reflection;

public class AgencyBookingDbContext : BaseDbContext<AgencyBookingDbContext>
{
    public AgencyBookingDbContext(DbContextOptions<AgencyBookingDbContext> options,
        IEventDispatcher eventDispatcher)
        : base(options, eventDispatcher)
    {
    }

    public DbSet<Agency> Agencies { get; set; } = default!;
    public DbSet<AgencyUser> AgencyUsers { get; set; } = default!;
    public DbSet<Appointment> Appointments { get; set; } = default!;
    public DbSet<AppointmentSlot> AppointmentSlots { get; set; } = default!;
    public DbSet<Holiday> Holidays { get; set; } = default!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        // Define relationships for AgencyUser linking
        builder.Entity<Appointment>()
            .HasOne(a => a.AgencyUser)
            .WithMany()
            .HasForeignKey(a => a.AgencyUserId)
            .OnDelete(DeleteBehavior.Restrict);

        base.OnModelCreating(builder);
    }
}
