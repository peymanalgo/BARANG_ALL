using BARANG.Core.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BARANG.Core.Infrastructure.Persistence.Migrations;

/// <summary>
/// دستیار مشتری B2C: ستون «channel» ('staff'|'customer') روی ai_conversations و ai_chat_messages
/// تا صف مشتریان از صف کارکنان جدا شود (سهمیه ۱۰ پرسش/۲۴ساعت فقط روی کانال مشتری). ایندکس یگانه
/// گفتگو به (company_id, user_id, channel) تغییر می‌کند تا یک کاربر بتواند در هر دو کانال گفتگوی خودش را داشته باشد.
/// Idempotent SQL — ستون‌ها با DEFAULT 'staff' اضافه می‌شوند و ردیف‌های قدیمی خودکار staff می‌مانند.
/// (ویژگی‌های Migration/DbContext در Designer partial همین کلاس هستند.)
/// </summary>
public partial class AddAiCustomerChannel : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE barang.ai_conversations ADD COLUMN IF NOT EXISTS channel character varying(20) NOT NULL DEFAULT 'staff';
            ALTER TABLE barang.ai_chat_messages ADD COLUMN IF NOT EXISTS channel character varying(20) NOT NULL DEFAULT 'staff';
            DROP INDEX IF EXISTS barang.uq_ai_conversations_company_id_user_id;
            CREATE UNIQUE INDEX IF NOT EXISTS uq_ai_conversations_company_user_channel ON barang.ai_conversations (company_id, user_id, channel);
            CREATE INDEX IF NOT EXISTS ix_ai_chat_messages_company_user_channel_created ON barang.ai_chat_messages (company_id, user_id, channel, created_at_utc);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP INDEX IF EXISTS barang.ix_ai_chat_messages_company_user_channel_created;
            DROP INDEX IF EXISTS barang.uq_ai_conversations_company_user_channel;
            CREATE UNIQUE INDEX IF NOT EXISTS uq_ai_conversations_company_id_user_id ON barang.ai_conversations (company_id, user_id);
            ALTER TABLE barang.ai_chat_messages DROP COLUMN IF EXISTS channel;
            ALTER TABLE barang.ai_conversations DROP COLUMN IF EXISTS channel;
            """);
    }
}
