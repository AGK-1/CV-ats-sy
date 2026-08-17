namespace cvAts.Class
{
    public class Token
    {
        public int id { get; set; }
        public int TokenValue { get; set; }
        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        public int UserId { get; set; }

        public User User { get; set; } = null!;

    }
}
