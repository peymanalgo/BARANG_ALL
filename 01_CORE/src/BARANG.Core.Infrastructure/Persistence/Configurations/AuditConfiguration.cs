using BARANG.Core.Domain.Auditing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BARANG.Core.Infrastructure.Persistence.Configurations;

internal sealed class AuditEventConfiguration : IEntityTypeConfiguration<AuditEvent>
{
    public void Configure(EntityTypeBuilder<AuditEvent> b)
    {
        b.ToTable("audit_events");
        b.HasKey(x => x.Id).HasName("pk_audit_events");
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.OccurredAtUtc).HasColumnName("occurred_at_utc");
        b.Property(x => x.CompanyId).HasColumnName("company_id");
        b.Property(x => x.ActorUserId).HasColumnName("actor_user_id");
        b.Property(x => x.ActorUsername).HasColumnName("actor_username").HasMaxLength(100);
        b.Property(x => x.ActorType).HasColumnName("actor_type").HasMaxLength(32);
        b.Property(x => x.Action).HasColumnName("action").HasMaxLength(150);
        b.Property(x => x.EntityType).HasColumnName("entity_type").HasMaxLength(100);
        b.Property(x => x.EntityId).HasColumnName("entity_id").HasMaxLength(200);
        b.Property(x => x.CorrelationId).HasColumnName("correlation_id").HasMaxLength(64);
        b.Property(x => x.RequestId).HasColumnName("request_id").HasMaxLength(128);
        b.Property(x => x.Source).HasColumnName("source").HasMaxLength(32);
        b.Property(x => x.Outcome).HasColumnName("outcome").HasMaxLength(16);
        b.Property(x => x.FailureCode).HasColumnName("failure_code").HasMaxLength(200);
        b.Property(x => x.IpAddress).HasColumnName("ip_address").HasMaxLength(64);
        b.Property(x => x.UserAgent).HasColumnName("user_agent").HasMaxLength(512);
        b.Property(x => x.MetadataJson).HasColumnName("metadata").HasColumnType("jsonb");
        b.Property(x => x.ChangesJson).HasColumnName("changes").HasColumnType("jsonb");
        b.HasIndex(x => new { x.CompanyId, x.OccurredAtUtc }).HasDatabaseName("ix_audit_events_company_id_occurred_at_utc");
        b.HasIndex(x => new { x.ActorUserId, x.OccurredAtUtc }).HasDatabaseName("ix_audit_events_actor_user_id_occurred_at_utc");
        b.HasIndex(x => new { x.EntityType, x.EntityId, x.OccurredAtUtc }).HasDatabaseName("ix_audit_events_entity_occurred_at_utc");
        b.HasIndex(x => x.CorrelationId).HasDatabaseName("ix_audit_events_correlation_id");
        b.HasIndex(x => new { x.Action, x.OccurredAtUtc }).HasDatabaseName("ix_audit_events_action_occurred_at_utc");
    }
}
