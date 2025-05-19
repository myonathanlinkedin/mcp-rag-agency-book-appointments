using System.Reflection;
using Microsoft.EntityFrameworkCore;

public class RAGDbContext : BaseDbContext<RAGDbContext>
{
    public RAGDbContext(DbContextOptions<RAGDbContext> options,
       IEventDispatcher eventDispatcher)
       : base(options, eventDispatcher)
    {
    }

    public DbSet<JobStatus> JobStatuses { get; set; } = default!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        base.OnModelCreating(builder);
    }
}