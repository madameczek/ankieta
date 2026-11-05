using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Polls.Api.Polls.Domain;

namespace Polls.Api.Polls.Infrastructure;

internal sealed class ParticipantConfiguration : IEntityTypeConfiguration<Participant>
{
    public void Configure(EntityTypeBuilder<Participant> builder)
    {
        builder.ToTable("user_poll");

        builder.HasKey(p => new { p.PollId, p.UserId }).HasName("pk_user_poll");
        builder.Property(p => p.PollId).HasColumnName("id_poll");
        builder.Property(p => p.UserId).HasColumnName("id_user");
        builder.Property(p => p.IsOwner).HasColumnName("is_owner");

        builder.HasOne(p => p.Poll).WithMany(p => p.Participants)
            .HasForeignKey(p => p.PollId).HasConstraintName("fk_user_poll_polls")
            .OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(p => p.User).WithMany()
            .HasForeignKey(p => p.UserId).HasConstraintName("fk_user_poll_users")
            .OnDelete(DeleteBehavior.NoAction);

        // D4: exactly one owner per poll
        builder.HasIndex(p => p.PollId).IsUnique().HasFilter("[is_owner] = 1").HasDatabaseName("ux_user_poll_owner");
        // "my polls" lookup (D35)
        builder.HasIndex(p => p.UserId).HasDatabaseName("ix_user_poll_user");

        // Hidden together with a soft-deleted poll (D8)
        builder.HasQueryFilter(p => p.Poll.DeletedAt == null);
    }
}
