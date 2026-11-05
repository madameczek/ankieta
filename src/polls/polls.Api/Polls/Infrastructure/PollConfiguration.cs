using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Polls.Api.Infrastructure;
using Polls.Api.Polls.Domain;

namespace Polls.Api.Polls.Infrastructure;

internal sealed class PollConfiguration : IEntityTypeConfiguration<Poll>
{
    public void Configure(EntityTypeBuilder<Poll> builder)
    {
        builder.ToTable("polls", t => t.HasCheckConstraint("ck_polls_title_not_empty", "LEN([title]) > 0"));

        builder.HasKey(p => p.Id).HasName("pk_polls");
        builder.Property(p => p.Id).HasColumnName("id").UseIdentityColumn();
        builder.Property(p => p.Title).HasColumnName("title").HasMaxLength(PollLimits.TitleMaxLength).IsRequired();
        builder.Property(p => p.Description).HasColumnName("description").HasMaxLength(PollLimits.DescriptionMaxLength);
        builder.Property(p => p.CreatedAt).HasColumnName("created_at").HasDefaultValueSql(DbDefaults.UtcNow);
        builder.Property(p => p.ModifiedAt).HasColumnName("modified_at").HasDefaultValueSql(DbDefaults.UtcNow);
        builder.Property(p => p.DeletedAt).HasColumnName("deleted_at");

        // D8: soft-deleted polls are hidden everywhere
        builder.HasQueryFilter(p => p.DeletedAt == null);
    }
}
