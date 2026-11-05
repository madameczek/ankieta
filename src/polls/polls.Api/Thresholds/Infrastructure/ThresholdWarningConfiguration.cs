using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Polls.Api.Thresholds.Domain;

namespace Polls.Api.Thresholds.Infrastructure;

internal sealed class ThresholdWarningConfiguration : IEntityTypeConfiguration<ThresholdWarning>
{
    public void Configure(EntityTypeBuilder<ThresholdWarning> builder)
    {
        builder.ToTable("threshold_warnings", t => t.HasCheckConstraint(
            "ck_threshold_warnings_resource",
            $"[resource] IN ('{ThresholdWarning.Users}', '{ThresholdWarning.Polls}')"));

        builder.HasKey(w => w.Resource).HasName("pk_threshold_warnings");
        builder.Property(w => w.Resource).HasColumnName("resource")
            .HasMaxLength(ThresholdWarning.ResourceMaxLength).IsUnicode(false);
        builder.Property(w => w.SentAt).HasColumnName("sent_at");
    }
}
