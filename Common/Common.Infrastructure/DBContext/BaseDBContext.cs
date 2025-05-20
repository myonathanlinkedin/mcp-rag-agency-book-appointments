using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;

public abstract class BaseDbContext<TContext> : DbContext where TContext : DbContext
{
    private readonly IEventDispatcher eventDispatcher;
    private readonly Stack<object> savesChangesTracker;
    private bool dispatchEvent = true;

    protected BaseDbContext(DbContextOptions<TContext> options, IEventDispatcher eventDispatcher)
        : base(options)
    {
        this.eventDispatcher = eventDispatcher;
        savesChangesTracker = new Stack<object>();
    }

    public BaseDbContext<TContext> WithDispatchEvent(bool dispatch)
    {
        dispatchEvent = dispatch;
        return this;
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        savesChangesTracker.Push(new object());

        if (dispatchEvent)
        {
            var entitiesWithEvents = ChangeTracker
                .Entries<IEntity>()
                .Where(e => e.Entity.Events.Any())
                .Select(e => e.Entity)
                .ToArray();

            foreach (var entity in entitiesWithEvents)
            {
                var events = entity.Events.ToArray();
                entity.ClearEvents();

                foreach (var domainEvent in events)
                {
                    await eventDispatcher.Dispatch(domainEvent, cancellationToken);
                }
            }
        }

        savesChangesTracker.Pop();

        if (!savesChangesTracker.Any())
        {
            return await base.SaveChangesAsync(cancellationToken);
        }

        return 0;
    }
}