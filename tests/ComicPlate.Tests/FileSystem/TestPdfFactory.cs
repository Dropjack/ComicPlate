using System.Text;

namespace ComicPlate.Tests.FileSystem;

internal static class TestPdfFactory
{
    public static void WriteEmptyPdf(string path)
    {
        WritePdf(path, [
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [] /Count 0 >>"
        ]);
    }

    public static void WriteEncryptedPlaceholderPdf(string path)
    {
        WritePdf(path, [
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 200 200] /Resources << >> /Contents 4 0 R >>",
            "<< /Length 31 >>\nstream\nq 1 0 0 rg 20 20 160 160 re f Q\nendstream",
            "<< /Filter /Standard /V 1 /R 2 /O <0000000000000000000000000000000000000000000000000000000000000000> /U <0000000000000000000000000000000000000000000000000000000000000000> /P -4 >>"
        ], encryptObjectNumber: 5);
    }

    public static void WriteTwoPagePdf(string path)
    {
        WriteTwoPagePdf(path, width: 200, height: 200);
    }

    public static void WriteTwoPagePdf(string path, int width, int height)
    {
        var objects = new[]
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R 5 0 R] /Count 2 >>",
            $"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {width} {height}] /Resources << >> /Contents 4 0 R >>",
            "<< /Length 31 >>\nstream\nq 1 0 0 rg 20 20 160 160 re f Q\nendstream",
            $"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {width} {height}] /Resources << >> /Contents 6 0 R >>",
            "<< /Length 31 >>\nstream\nq 0 0 1 rg 20 20 160 160 re f Q\nendstream"
        };

        WritePdf(path, objects);
    }

    private static void WritePdf(string path, IReadOnlyList<string> objects, int? encryptObjectNumber = null)
    {
        var builder = new StringBuilder();
        var offsets = new List<int> { 0 };

        builder.Append("%PDF-1.4\n");
        foreach (var (body, index) in objects.Select((body, index) => (body, index)))
        {
            offsets.Add(Encoding.ASCII.GetByteCount(builder.ToString()));
            builder.Append(index + 1).Append(" 0 obj\n");
            builder.Append(body).Append('\n');
            builder.Append("endobj\n");
        }

        var xrefOffset = Encoding.ASCII.GetByteCount(builder.ToString());
        builder.Append("xref\n");
        builder.Append("0 ").Append(objects.Count + 1).Append('\n');
        builder.Append("0000000000 65535 f \n");
        foreach (var offset in offsets.Skip(1))
        {
            builder.Append(offset.ToString("D10")).Append(" 00000 n \n");
        }

        builder.Append("trailer\n");
        builder.Append("<< /Size ").Append(objects.Count + 1).Append(" /Root 1 0 R");
        if (encryptObjectNumber is not null)
        {
            builder.Append(" /Encrypt ").Append(encryptObjectNumber.Value).Append(" 0 R");
        }

        builder.Append(" >>\n");
        builder.Append("startxref\n");
        builder.Append(xrefOffset).Append('\n');
        builder.Append("%%EOF\n");

        File.WriteAllText(path, builder.ToString(), Encoding.ASCII);
    }
}
