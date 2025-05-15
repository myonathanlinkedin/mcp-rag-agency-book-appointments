using System.Transactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public static class TransactionHelper
{
    private static readonly Random Random = new Random();

    public static async Task<T> ExecuteInTransactionAsync<T>(
        Func<Task<T>> operation,
        IsolationLevel isolationLevel = IsolationLevel.RepeatableRead,
        int maxRetries = 3)
    {
        var attempt = 0;
        while (true)
        {
            try
            {
                using var scope = new TransactionScope(
                    TransactionScopeOption.Required,
                    new TransactionOptions
                    {
                        IsolationLevel = isolationLevel,
                        Timeout = TimeSpan.FromSeconds(30)
                    },
                    TransactionScopeAsyncFlowOption.Enabled);

                var result = await operation();
                scope.Complete();
                return result;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                if (++attempt >= maxRetries)
                    throw;

                // Exponential backoff with jitter
                var delay = (int)Math.Pow(2, attempt) * 100 + Random.Next(100);
                await Task.Delay(delay);
            }
            catch (TransactionAbortedException)
            {
                if (++attempt >= maxRetries)
                    throw;

                // Linear backoff for transaction aborts
                await Task.Delay(100 * attempt);
            }
        }
    }

    public static async Task ExecuteInTransactionAsync(
        Func<Task> operation,
        IsolationLevel isolationLevel = IsolationLevel.RepeatableRead,
        int maxRetries = 3)
    {
        await ExecuteInTransactionAsync(async () =>
        {
            await operation();
            return true;
        }, isolationLevel, maxRetries);
    }
} 