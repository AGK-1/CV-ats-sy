using UglyToad.PdfPig;
using System.Text;

public class PdfService
{
    public string ExtractText(string filePath)
    {
        var sb = new StringBuilder();

        using (var pdf = PdfDocument.Open(filePath))
        {
            foreach (var page in pdf.GetPages())
            {
                sb.AppendLine(page.Text);
            }
        }

        return sb.ToString();
    }
}
