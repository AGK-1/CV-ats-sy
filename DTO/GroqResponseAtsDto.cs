namespace cvAts.DTO
{
    public class GroqResponseAtsDto
    {
        public string model { get; set; } = "llama-3.3-70b-versatile";
        public List<Message> messages { get; set; }
    }
    public class Message
    {
        public string role { get; set; }
        public string content { get; set; }
    }
}
