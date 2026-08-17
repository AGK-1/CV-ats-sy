namespace cvAts.Class
{
    public class AuditLog
    {
        public int Id { get; set; }

        // Кто выполнял действие
        public int UserId { get; set; }
        public User User { get; set; }

        // Если действие выполнял админ от имени пользователя
        public int? ImpersonatedBy { get; set; }

        // Что произошло
        public string Action { get; set; } = null!;

        // Над чем произошло действие
        public string? EntityName { get; set; }

        public int? EntityId { get; set; }

        // Дополнительная информация
        public string? Description { get; set; }

        // IP и User-Agent
        public string? IpAddress { get; set; }

        public string? UserAgent { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
