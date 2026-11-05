using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Polls.Api.Infrastructure;

/// <summary>Lets <c>dotnet ef</c> build the model without starting the host. Never connects.</summary>
internal sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<PollsDbContext>
{
    public PollsDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<PollsDbContext>().UseSqlServer("Server=design-time-only").Options);
}
