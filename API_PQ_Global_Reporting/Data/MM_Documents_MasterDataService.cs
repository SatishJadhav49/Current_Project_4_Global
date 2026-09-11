using Microsoft.Data.SqlClient;
using API_PQ_Global_Reporting.Models.DTOs;
using Newtonsoft.Json;
using System.Data;

namespace API_PQ_Global_Reporting.Data
{
    public class MM_Documents_MasterDataService
    {

        private readonly IDbConnectionFactory _connectionFactory;



        public MM_Documents_MasterDataService(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<string> UploadDocumentFile(IFormFile file, DocumentsMasterCreateDtos dto)
        {
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("Invalid file.");
            }

            string uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/Document_File/");
            if (!Directory.Exists(uploadPath))
            {
                Directory.CreateDirectory(uploadPath);
            }

            string fileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
            string filePath = Path.Combine(uploadPath, fileName);

            await using var fileStream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(fileStream);

            // Save metadata to database
            using var connection = await _connectionFactory.CreateConnectionAsync();
            const string query = @"
            INSERT INTO MM_Documents_Master 
            (Document_Title, Document_Path, Inserted_Date, Inserted_User_ID, Inserted_Host, Plant_Code)
            VALUES (@Document_Title, @Document_Path, @Inserted_Date, @Inserted_User_ID, @Inserted_Host, @Plant_Code)";

            using var command = new SqlCommand(query, (SqlConnection)connection);
            command.Parameters.AddWithValue("@Document_Title", dto.Document_Title);
            command.Parameters.AddWithValue("@Document_Path", fileName);
            command.Parameters.AddWithValue("@Inserted_Date", DateTime.Now);
            command.Parameters.AddWithValue("@Inserted_User_ID", dto.Inserted_User_ID ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Inserted_Host", dto.Inserted_Host ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Plant_Code", dto.Plant_Code ?? (object)DBNull.Value);

            await command.ExecuteNonQueryAsync();

            return fileName;
        }


        public async Task<IEnumerable<DocumentsMasterListDtos>> GetTableDatas()
        {
            var Documents = new List<DocumentsMasterListDtos>();

            using var connection = await _connectionFactory.CreateConnectionAsync();
            const string query = @"
                SELECT 
                Document.Document_ID, 
                Document.Document_Title, 
                Document.Document_Path, 
                Document.Inserted_Date,
                Document.Inserted_User_ID,  
                employee.Employee_Name
                FROM MM_Documents_Master AS Document
                Inner Join MM_Employee AS employee on Document.Inserted_User_ID = employee.Employee_ID
                ";

            using var command = new SqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                Documents.Add(new DocumentsMasterListDtos
                {
                    Document_ID = reader.GetDecimal(reader.GetOrdinal("Document_ID")),
                    Document_Title = reader.GetString(reader.GetOrdinal("Document_Title")),
                    Document_Path = reader.GetString(reader.GetOrdinal("Document_Path")),
                    Inserted_Date = reader.GetDateTime(reader.GetOrdinal("Inserted_Date")),
                    Inserted_User_ID = reader.GetDecimal(reader.GetOrdinal("Inserted_User_ID")),
                    Employee_Name = reader.IsDBNull(reader.GetOrdinal("Employee_Name")) ? string.Empty : reader.GetString(reader.GetOrdinal("Employee_Name"))
                });
            }

            return Documents;
        }

        public async Task<bool> DeleteFile(decimal DocumentId)
        {
            try
            {
                using var connection = await _connectionFactory.CreateConnectionAsync();
                using var transaction = connection.BeginTransaction();

                try
                {
                    const string query = "DELETE FROM dbo.MM_Documents_Master WHERE Document_ID = @Document_ID";
                    using var command = new SqlCommand(query, connection, transaction);
                    command.Parameters.Add(new SqlParameter("@Document_ID", DocumentId));

                    int rowsAffected = await command.ExecuteNonQueryAsync();

                    if (rowsAffected == 0)
                    {
                        transaction.Rollback();
                        return false; // No rows were deleted
                    }

                    transaction.Commit();
                    return true;
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
            catch
            {
                throw;
            }
        }
    }
}
