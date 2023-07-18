using Azure;
using FileApi.Data;
using FileApi.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace FileApi.Services
{
    public class FileRepository : IFileRepository
    {
        private readonly FileApiDbContext _context;

        public FileRepository(FileApiDbContext context)
        {
            _context = context;
        }

        public async Task AddFileAsync(FileModel file)
        {
            _context.Files.Add(file);
            await _context.SaveChangesAsync();
        }

        public async Task<(string fileName, string contentType, Stream dataStream)> GetFileAsync(int id)
        {
            string fileName;
            string contentType;
            Stream dataStream;

            string query = "SELECT UntrustedName, ContentType, Content FROM Files WHERE Id = @id";

            var connection = _context.Database.GetDbConnection();
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = query;
            command.Parameters.Add(new SqlParameter("@id", id));

            // The reader needs to be executed with the SequentialAccess behavior to enable network streaming
            var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess);

            if (!await reader.ReadAsync())
            {
                throw new FileNotFoundException($"File with ID: {id} could not be located");
            }

            fileName = reader.GetString(0);
            contentType = reader.GetString(1);
            dataStream = reader.GetStream(2);

            return (fileName, contentType, dataStream); 
        }
    }
}
