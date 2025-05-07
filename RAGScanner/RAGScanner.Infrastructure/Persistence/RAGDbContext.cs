using System.Reflection;
using Microsoft.EntityFrameworkCore;

internal class RAGDbContext : DbContext
{
    public RAGDbContext(DbContextOptions<RAGDbContext> options)
        : base(options)
    {
    }

    public DbSet<JobStatus> JobStatuses { get; set; } = default!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        base.OnModelCreating(builder);
    }
}