using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Polls.Api.Infrastructure;
using Testcontainers.MsSql;
using Xunit;

namespace Polls.Tests.Common;

/// <summary>
/// One throwaway SQL Server per test assembly (D27). The schema comes from the committed
/// idempotent script <c>db/migrations.sql</c>, so tests check the artifact that is run in production (D13).
/// </summary>
public sealed partial class SqlServerFixture : IAsyncLifetime
{
    public const string Image = "mcr.microsoft.com/mssql/server:2022-latest";
    public const string DatabaseName = "polls_test";
    public const string ScriptPath = "db/migrations.sql";

    private readonly MsSqlContainer _container = new MsSqlBuilder(Image).Build();

    public string ConnectionString { get; private set; } = string.Empty;

    public static string ReadScript() => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, ScriptPath));

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();

        await using (var master = new SqlConnection(_container.GetConnectionString()))
        {
            await master.OpenAsync();
            await using var create = new SqlCommand($"CREATE DATABASE [{DatabaseName}]", master);
            await create.ExecuteNonQueryAsync();
        }

        ConnectionString = new SqlConnectionStringBuilder(_container.GetConnectionString()) { InitialCatalog = DatabaseName }.ConnectionString;

        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();
        foreach (var batch in GoSeparator().Split(ReadScript()).Where(b => !string.IsNullOrWhiteSpace(b)))
        {
            await using var command = new SqlCommand(batch, connection);
            await command.ExecuteNonQueryAsync();
        }
    }

    public ValueTask DisposeAsync() => _container.DisposeAsync();

    internal PollsDbContext CreateContext(TimeProvider? timeProvider = null) =>
        new(new DbContextOptionsBuilder<PollsDbContext>()
            .UseSqlServer(ConnectionString)
            .AddInterceptors(new TimestampInterceptor(timeProvider ?? TimeProvider.System))
            .Options);

    [GeneratedRegex(@"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase)]
    private static partial Regex GoSeparator();
}
