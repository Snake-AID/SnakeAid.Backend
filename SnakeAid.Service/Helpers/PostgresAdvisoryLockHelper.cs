using System.Data;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace SnakeAid.Service.Helpers;

internal static class PostgresAdvisoryLockHelper
{
    public static async Task<bool> TryAcquireSessionLockAsync(
        DbContext dbContext,
        string lockName,
        CancellationToken cancellationToken = default)
    {
        if (!SupportsPostgresAdvisoryLocks(dbContext))
        {
            return true;
        }

        var connection = dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT pg_try_advisory_lock(@lockKey)";
        AddLockKeyParameter(command, lockName);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is bool acquired && acquired;
    }

    public static async Task ReleaseSessionLockAsync(
        DbContext dbContext,
        string lockName,
        CancellationToken cancellationToken = default)
    {
        if (!SupportsPostgresAdvisoryLocks(dbContext))
        {
            return;
        }

        var connection = dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            return;
        }

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT pg_advisory_unlock(@lockKey)";
        AddLockKeyParameter(command, lockName);
        await command.ExecuteScalarAsync(cancellationToken);
    }

    public static async Task AcquireTransactionLockAsync(
        DbContext dbContext,
        string lockName,
        CancellationToken cancellationToken = default)
    {
        if (!SupportsPostgresAdvisoryLocks(dbContext))
        {
            return;
        }

        var transaction = dbContext.Database.CurrentTransaction;
        if (transaction == null)
        {
            throw new InvalidOperationException("Transaction lock requires an active database transaction.");
        }

        var connection = dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var command = connection.CreateCommand();
        command.Transaction = transaction.GetDbTransaction();
        command.CommandText = "SELECT pg_advisory_xact_lock(@lockKey)";
        AddLockKeyParameter(command, lockName);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static bool SupportsPostgresAdvisoryLocks(DbContext dbContext)
    {
        return dbContext.Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true;
    }

    private static void AddLockKeyParameter(IDbCommand command, string lockName)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = "lockKey";
        parameter.Value = ComputeStableLockKey(lockName);
        command.Parameters.Add(parameter);
    }

    private static long ComputeStableLockKey(string lockName)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(lockName));
        return BitConverter.ToInt64(hash, 0);
    }
}
