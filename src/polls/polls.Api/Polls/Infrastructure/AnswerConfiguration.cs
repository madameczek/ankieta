using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Polls.Api.Infrastructure;
using Polls.Api.Polls.Domain;

namespace Polls.Api.Polls.Infrastructure;

internal sealed class AnswerConfiguration : IEntityTypeConfiguration<Answer>
{
    public void Configure(EntityTypeBuilder<Answer> builder)
    {
        // D31: no empty answers; clearing an answer deletes the row
        builder.ToTable("answers", t => t.HasCheckConstraint("ck_answers_answer_not_empty", "LEN([answer]) > 0"));

        builder.HasKey(a => a.Id).HasName("pk_answers");
        builder.Property(a => a.Id).HasColumnName("id").UseIdentityColumn();
        builder.Property(a => a.QuestionId).HasColumnName("id_question");
        builder.Property(a => a.UserId).HasColumnName("id_user");
        builder.Property(a => a.Text).HasColumnName("answer").HasMaxLength(PollLimits.AnswerMaxLength).IsRequired();
        builder.Property(a => a.ModifiedAt).HasColumnName("modified_at").HasDefaultValueSql(DbDefaults.UtcNow);

        builder.HasOne(a => a.Question).WithMany(q => q.Answers)
            .HasForeignKey(a => a.QuestionId).HasConstraintName("fk_answers_questions")
            .OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(a => a.User).WithMany()
            .HasForeignKey(a => a.UserId).HasConstraintName("fk_answers_users")
            .OnDelete(DeleteBehavior.NoAction);

        // D3: one answer per user per question
        builder.HasIndex(a => new { a.QuestionId, a.UserId }).IsUnique().HasDatabaseName("ux_answers_question_user");
        builder.HasIndex(a => a.UserId).HasDatabaseName("ix_answers_user");

        // Hidden together with a soft-deleted question or poll (D8)
        builder.HasQueryFilter(a => a.Question.DeletedAt == null && a.Question.Poll.DeletedAt == null);
    }
}
