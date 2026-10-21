using API_PQ_Global_Reporting.Data;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Text;

namespace API_PQ_Global_Reporting.CorrelationModule.Data
{
    /// <summary>An image read from a source application, with its detected content type.</summary>
    public sealed record MM_CR_Image(byte[] Content, string ContentType);

    /// <summary>
    /// Reads mapped-item images live from each application's MM_Image_Master
    /// through the application's image SP (MM_CR_Application.Image_SP_Name).
    /// Nothing is stored in PQ_Dashboard_Master.
    /// </summary>
    public class MM_CR_ImageDataService
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public MM_CR_ImageDataService(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        /// <summary>Returns null when the application has no image SP or the leaf has no image.</summary>
        public async Task<MM_CR_Image?> GetImage(int appId, decimal leafId)
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();

            // 1. Application -> image SP name (validated to exist by the SP)
            string? imageSpName;
            using (var command = new SqlCommand("SP_CR_Get_Application_Image_SP", connection) { CommandType = CommandType.StoredProcedure })
            {
                command.Parameters.Add(new SqlParameter("@App_ID", appId));
                imageSpName = (await command.ExecuteScalarAsync())?.ToString();
            }

            if (string.IsNullOrWhiteSpace(imageSpName)) return null;

            // 2. Image of the leaf
            object? fileContent;
            using (var command = new SqlCommand(imageSpName, connection) { CommandType = CommandType.StoredProcedure })
            {
                command.Parameters.Add(new SqlParameter("@Leaf_ID", SqlDbType.Decimal) { Precision = 18, Scale = 0, Value = leafId });
                fileContent = await command.ExecuteScalarAsync();
            }

            var bytes = ToBytes(fileContent);
            return bytes == null || bytes.Length == 0
                ? null
                : new MM_CR_Image(bytes, DetectContentType(bytes));
        }

        /// <summary>FileContent may be stored as binary or as a (data-URI) base64 string.</summary>
        private static byte[]? ToBytes(object? value)
        {
            switch (value)
            {
                case null:
                case DBNull:
                    return null;
                case byte[] bytes:
                    return bytes;
                case string text:
                    var base64 = text.Trim();
                    var comma = base64.IndexOf(',');
                    if (base64.StartsWith("data:", StringComparison.OrdinalIgnoreCase) && comma > 0)
                    {
                        base64 = base64[(comma + 1)..];
                    }
                    try
                    {
                        return Convert.FromBase64String(base64);
                    }
                    catch (FormatException)
                    {
                        return null;
                    }
                default:
                    return null;
            }
        }

        /// <summary>Content type from the file signature; works for any size / format the browser supports.</summary>
        private static string DetectContentType(byte[] bytes)
        {
            bool StartsWith(params byte[] signature) =>
                bytes.Length >= signature.Length && signature.Select((b, i) => bytes[i] == b).All(match => match);

            if (StartsWith(0x89, 0x50, 0x4E, 0x47)) return "image/png";
            if (StartsWith(0xFF, 0xD8, 0xFF)) return "image/jpeg";
            if (StartsWith(0x47, 0x49, 0x46, 0x38)) return "image/gif";
            if (StartsWith(0x42, 0x4D)) return "image/bmp";
            if (StartsWith(0x49, 0x49, 0x2A, 0x00) || StartsWith(0x4D, 0x4D, 0x00, 0x2A)) return "image/tiff";
            if (bytes.Length >= 12 && Encoding.ASCII.GetString(bytes, 0, 4) == "RIFF" && Encoding.ASCII.GetString(bytes, 8, 4) == "WEBP") return "image/webp";
            if (StartsWith(0x00, 0x00, 0x01, 0x00)) return "image/x-icon";

            var head = Encoding.UTF8.GetString(bytes, 0, Math.Min(bytes.Length, 256)).TrimStart();
            if (head.StartsWith("<svg", StringComparison.OrdinalIgnoreCase) || head.StartsWith("<?xml", StringComparison.OrdinalIgnoreCase))
            {
                return "image/svg+xml";
            }

            return "application/octet-stream";
        }
    }
}
