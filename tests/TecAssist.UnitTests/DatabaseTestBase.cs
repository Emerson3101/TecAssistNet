using Microsoft.EntityFrameworkCore;
using TecAssist.UnitTests.Persistence;

namespace TecAssist.UnitTests;

public abstract class DatabaseTestBase
{
    private static int _databaseCounter;

    protected static async Task<TestDbContext> CreateContextAsync()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase($"tecasist-tests-{Interlocked.Increment(ref _databaseCounter)}")
            .Options;

        var context = new TestDbContext(options);
        await context.Database.EnsureCreatedAsync();
        return context;
    }
}
