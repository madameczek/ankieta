using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Polls.Api.Infrastructure;
using Polls.Api.Users.Domain;

namespace Polls.Api.Users.Infrastructure;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users", t => t.HasCheckConstraint("ck_users_name_not_empty", "LEN([name]) > 0"));

        builder.HasKey(u => u.Id).HasName("pk_users");
        builder.Property(u => u.Id).HasColumnName("id").UseIdentityColumn();
        builder.Property(u => u.Name).HasColumnName("name").HasMaxLength(UserLimits.NameMaxLength).IsRequired();
        builder.Property(u => u.CreatedAt).HasColumnName("created_at").HasDefaultValueSql(DbDefaults.UtcNow);

        // D6: names are unique
        builder.HasIndex(u => u.Name).IsUnique().HasDatabaseName("ux_users_name");
    }
}
