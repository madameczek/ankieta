using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Polls.Api.Polls.Domain;
using Polls.Api.Thresholds.Domain;
using Polls.Api.Users.Domain;

namespace Polls.Api.Infrastructure;

/// <summary>Used directly by handlers, no repositories (D12). Also stores Data Protection keys (D22).</summary>
internal sealed class PollsDbContext(DbContextOptions<PollsDbContext> options) : DbContext(options), IDataProtectionKeyContext
{
    public DbSet<User> Users => Set<User>();

    public DbSet<Poll> Polls => Set<Poll>();

    public DbSet<Participant> Participants => Set<Participant>();

    public DbSet<Question> Questions => Set<Question>();

    public DbSet<Answer> Answers => Set<Answer>();

    public DbSet<ThresholdWarning> ThresholdWarnings => Set<ThresholdWarning>();

    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    // D52: Fluent API only, one IEntityTypeConfiguration per entity
    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PollsDbContext).Assembly);
}
