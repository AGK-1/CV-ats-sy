namespace cvAts.Class
{
    public class CoverLetterWithOnlyJobDes
    {
        public string jobDescription { get; set; } = string.Empty;

    }

    
    public class CoverLetterResponseDes
    {
        public bool Success { get; set; }
        public string? CoverLetter { get; set; }
        public string? Error { get; set; }
        public DateTime GeneratedAt { get; set; }
        public int TokensUsed { get; set; }
    }
}
