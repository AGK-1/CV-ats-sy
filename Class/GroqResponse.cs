using cvAts.DTO;

namespace cvAts.Class
{
    public class GroqResponse
    {
        public List<Choice> choices { get; set; }
    }

    public class Choice
    {
        public Message message { get; set; }
    }
}

