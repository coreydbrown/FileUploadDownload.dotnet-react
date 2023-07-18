using FileApi.Models;

namespace FileApi.Services
{
    public interface IFileRepository
    {
        Task AddFileAsync(FileModel file);
        Task<(string fileName, string contentType, Stream dataStream)> GetFileAsync(int id);
    }
}
