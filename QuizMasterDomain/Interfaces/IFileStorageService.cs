namespace QuizMasterDomain.Interfaces
{
    public interface IFileStorageService
    {
        Task<string> SaveFileAsync(Stream stream, string fileName, string contentType, CancellationToken cancellationToken = default);

        Task DeleteFileAsync(string filePath, CancellationToken cancellationToken = default);
    }
}