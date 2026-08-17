using System.Text.Json;

namespace cvAts.Class
{
    public class Mycv
    {
        public int Id { get; set; }

        public JsonDocument Data { get; set; } = null!; // всё содержимое резюме

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        public int UserId { get; set; }

        public User User { get; set; } = null!;

    }
}
