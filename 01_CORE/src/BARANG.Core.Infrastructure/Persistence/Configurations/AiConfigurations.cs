using BARANG.Core.Domain.Ai;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BARANG.Core.Infrastructure.Persistence.Configurations;

internal sealed class AiModelSettingsConfiguration : IEntityTypeConfiguration<AiModelSettings>
{
    public void Configure(EntityTypeBuilder<AiModelSettings> b)
    {
        b.ToTable("ai_model_settings", "barang");
        b.HasKey(x => x.Id).HasName("pk_ai_model_settings");
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.CompanyId).HasColumnName("company_id");
        b.Property(x => x.ProviderName).HasColumnName("provider_name").HasMaxLength(100);
        b.Property(x => x.BaseUrl).HasColumnName("base_url").HasMaxLength(500);
        b.Property(x => x.ApiKeyCipher).HasColumnName("api_key_cipher");
        b.Property(x => x.ApiKeyHint).HasColumnName("api_key_hint").HasMaxLength(40);
        b.Property(x => x.ModelName).HasColumnName("model_name").HasMaxLength(200);
        b.Property(x => x.Temperature).HasColumnName("temperature").HasPrecision(4, 2);
        b.Property(x => x.MaxOutputTokens).HasColumnName("max_output_tokens");
        b.Property(x => x.TimeoutSeconds).HasColumnName("timeout_seconds");
        b.Property(x => x.SystemPromptOverride).HasColumnName("system_prompt_override");
        b.Property(x => x.ToolCallingEnabled).HasColumnName("tool_calling_enabled");
        b.Property(x => x.IsEnabled).HasColumnName("is_enabled");
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        b.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
        b.Property(x => x.Version).HasColumnName("version").IsConcurrencyToken();
        b.HasIndex(x => x.CompanyId).IsUnique().HasDatabaseName("uq_ai_model_settings_company_id");
    }
}

internal sealed class AiConversationConfiguration : IEntityTypeConfiguration<AiConversation>
{
    public void Configure(EntityTypeBuilder<AiConversation> b)
    {
        b.ToTable("ai_conversations", "barang");
        b.HasKey(x => x.Id).HasName("pk_ai_conversations");
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.CompanyId).HasColumnName("company_id");
        b.Property(x => x.UserId).HasColumnName("user_id");
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        b.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
        b.HasIndex(x => new { x.CompanyId, x.UserId }).IsUnique().HasDatabaseName("uq_ai_conversations_company_id_user_id");
    }
}

internal sealed class AiChatMessageConfiguration : IEntityTypeConfiguration<AiChatMessage>
{
    public void Configure(EntityTypeBuilder<AiChatMessage> b)
    {
        b.ToTable("ai_chat_messages", "barang");
        b.HasKey(x => x.Id).HasName("pk_ai_chat_messages");
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.ConversationId).HasColumnName("conversation_id");
        b.Property(x => x.CompanyId).HasColumnName("company_id");
        b.Property(x => x.UserId).HasColumnName("user_id");
        b.Property(x => x.Role).HasColumnName("role").HasMaxLength(20);
        b.Property(x => x.Content).HasColumnName("content");
        b.Property(x => x.ToolsUsed).HasColumnName("tools_used");
        b.Property(x => x.PromptTokens).HasColumnName("prompt_tokens");
        b.Property(x => x.CompletionTokens).HasColumnName("completion_tokens");
        b.Property(x => x.Culture).HasColumnName("culture").HasMaxLength(10);
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        b.HasIndex(x => x.ConversationId).HasDatabaseName("ix_ai_chat_messages_conversation_id");
        b.HasIndex(x => new { x.CompanyId, x.UserId, x.CreatedAtUtc }).HasDatabaseName("ix_ai_chat_messages_company_user_created");
    }
}

internal sealed class AiAuditEntryConfiguration : IEntityTypeConfiguration<AiAuditEntry>
{
    public void Configure(EntityTypeBuilder<AiAuditEntry> b)
    {
        b.ToTable("ai_audit_log", "barang");
        b.HasKey(x => x.Id).HasName("pk_ai_audit_log");
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.CompanyId).HasColumnName("company_id");
        b.Property(x => x.ActorUserId).HasColumnName("actor_user_id");
        b.Property(x => x.ActorUsername).HasColumnName("actor_username").HasMaxLength(150);
        b.Property(x => x.Action).HasColumnName("action").HasMaxLength(100);
        b.Property(x => x.Detail).HasColumnName("detail");
        b.Property(x => x.OccurredAtUtc).HasColumnName("occurred_at_utc");
        b.HasIndex(x => new { x.CompanyId, x.OccurredAtUtc }).HasDatabaseName("ix_ai_audit_log_company_occurred");
    }
}
