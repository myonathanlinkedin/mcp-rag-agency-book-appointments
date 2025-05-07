using MediatR;

public abstract class BaseCommand<T> : IRequest<Result>
{
    // Any shared properties or methods can be added here later
}