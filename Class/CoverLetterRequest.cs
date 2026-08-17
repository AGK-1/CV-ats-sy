namespace cvAts.Class
{
    public class CoverLetterRequest
    {
        public string JobTitle { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string JobDescription { get; set; } = string.Empty;
        public string ApplicantName { get; set; } = string.Empty;
        public string ApplicantExperience { get; set; } = string.Empty;
        public string ApplicantSkills { get; set; } = string.Empty;
        public string Language { get; set; } = "russian"; // russian или english
    }

    public class CoverLetterResponse
    {
        public bool Success { get; set; }
        public string? CoverLetter { get; set; }
        public string? Error { get; set; }
        public DateTime GeneratedAt { get; set; }
        public int TokensUsed { get; set; }
    }
}
