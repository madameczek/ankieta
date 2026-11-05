using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Polls.Api.Infrastructure;
using Polls.Api.Polls.Domain;

namespace Polls.Api.Polls.Infrastructure;

internal sealed class QuestionConfiguration : IEntityTypeConfiguration<Question>
{
    public void Configure(EntityTypeBuilder<Question> builder)
    {
        builder.ToTable("questions", t => t.HasCheckConstraint("ck_questions_question_not_empty", "LEN([question]) > 0"));

        builder.HasKey(q => q.Id).HasName("pk_questions");
        builder.Property(q => q.Id).HasColumnName("id").UseIdentityColumn();
        builder.Property(q => q.PollId).HasColumnName("id_poll");
        // "order" in the spec; ORDER is a reserved word
        builder.Property(q => q.SortOrder).HasColumnName("sort_order");
        builder.Property(q => q.Text).HasColumnName("question").HasMaxLength(PollLimits.QuestionMaxLength).IsRequired();
        builder.Property(q => q.ModifiedAt).HasColumnName("modified_at").HasDefaultValueSql(DbDefaults.UtcNow);
        builder.Property(q => q.DeletedAt).HasColumnName("deleted_at");

        builder.HasOne(q => q.Poll).WithMany(p => p.Questions)
            .HasForeignKey(q => q.PollId).HasConstraintName("fk_questions_polls")
            .OnDelete(DeleteBehavior.NoAction);

        // D9: order is unique among the poll's non-deleted questions
        builder.HasIndex(q => new { q.PollId, q.SortOrder }).IsUnique()
            .HasFilter("[deleted_at] IS NULL").HasDatabaseName("ux_questions_poll_sort_order");

        // D8: soft-deleted questions, and questions of soft-deleted polls, are hidden
        builder.HasQueryFilter(q => q.DeletedAt == null && q.Poll.DeletedAt == null);
    }
}
