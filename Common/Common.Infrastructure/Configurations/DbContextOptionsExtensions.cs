using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging;

namespace Common.Infrastructure.Configurations;

public static class DbContextOptionsExtensions
{
    public static DbContextOptionsBuilder ConfigureCommonOptions(
        this DbContextOptionsBuilder options,
        string connectionString)
    {
        return options
            .UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 10,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null);
                sqlOptions.CommandTimeout(30);
            })
            .ConfigureWarnings(warnings =>
                warnings.Default(WarningBehavior.Log))
            .EnableDetailedErrors(false)
            .EnableSensitiveDataLogging(false);
    }
} 