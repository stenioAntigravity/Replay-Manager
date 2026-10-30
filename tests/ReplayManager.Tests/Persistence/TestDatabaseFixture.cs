using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ReplayManager.Infrastructure.Data;

namespace ReplayManager.Tests.Persistence;

public class TestDatabaseFixture : IDisposable
{
    private readonly SqliteConnection _connection;

    public TestDatabaseFixture()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        // Ativa suporte estrito a chaves estrangeiras no SQLite
        using var command = _connection.CreateCommand();
        command.CommandText = "PRAGMA foreign_keys = ON;";
        command.ExecuteNonQuery();
    }

    public ReplayManagerDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ReplayManagerDbContext>()
            .UseSqlite(_connection)
            .Options;

        var context = new ReplayManagerDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    public void Dispose()
    {
        _connection.Close();
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }
}
