using QuizMasterDomain.Interfaces;

namespace QuizMasterInfrastructure.FileStorage
{
    public class LocalFileStorageService : IFileStorageService
    {
        private readonly string _profileDirectory;

        public LocalFileStorageService(string webRootPath)
        {
            _profileDirectory = Path.GetFullPath(Path.Combine(webRootPath, "uploads", "profiles"));
        }

        public async Task<string> SaveFileAsync( Stream stream, string fileName, string contentType, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(stream);

            var extension = Path.GetExtension(fileName).ToLowerInvariant();

            var expectedContentType = extension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                _ => throw new ArgumentException("Choose a JPG or PNG image.")
            };

            if (!string.Equals(
                contentType,
                expectedContentType,
                StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException(
                    "The image format does not match its file extension.");
            }

            const int maximumSize = 2 * 1024 * 1024;

            using var image = new MemoryStream();
            var buffer = new byte[81920];
            int bytesRead;

            while ((bytesRead = await stream.ReadAsync(
                buffer, cancellationToken)) != 0)
            {
                if (image.Length + bytesRead > maximumSize)
                {
                    throw new ArgumentException(
                        "The profile photo must be 2 MB or smaller.");
                }

                await image.WriteAsync(
                    buffer.AsMemory(0, bytesRead), cancellationToken);
            }

            var data = image.ToArray();

            bool hasValidSignature;

            if (expectedContentType == "image/jpeg")
            {
                hasValidSignature = data.Length >= 4
                    && data[0] == 0xFF
                    && data[1] == 0xD8
                    && data[2] == 0xFF
                    && data[^2] == 0xFF
                    && data[^1] == 0xD9;
            }
            else
            {
                byte[] pngSignature =
                {
            0x89, 0x50, 0x4E, 0x47,
            0x0D, 0x0A, 0x1A, 0x0A
        };

                hasValidSignature = data.Length >= 24
                    && data.AsSpan(0, 8).SequenceEqual(pngSignature)
                    && data.AsSpan(12, 4).SequenceEqual("IHDR"u8);
            }

            if (!hasValidSignature)
            {
                throw new ArgumentException(
                    "The file does not have a valid JPG or PNG signature.");
            }

            var storedExtension =
                expectedContentType == "image/png" ? ".png" : ".jpg";

            var newFileName = $"{Guid.NewGuid():N}{storedExtension}";

            Directory.CreateDirectory(_profileDirectory);

            var fullPath = Path.Combine(_profileDirectory, newFileName);

            try
            {
                await using var fileStream = new FileStream(
                    fullPath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None);

                await fileStream.WriteAsync(data, cancellationToken);
            }
            catch
            {
                File.Delete(fullPath);
                throw;
            }

            return $"/uploads/profiles/{newFileName}";
        }

        public Task DeleteFileAsync( string filePath, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            const string prefix = "/uploads/profiles/";

            if (string.IsNullOrWhiteSpace(filePath)
                || !filePath.StartsWith(prefix, StringComparison.Ordinal))
            {
                throw new ArgumentException("Invalid profile photo path.");
            }

            var fileName = filePath.Substring(prefix.Length);
            var extension = Path.GetExtension(fileName);
            var name = Path.GetFileNameWithoutExtension(fileName);

            if (fileName != Path.GetFileName(fileName)
                || name.Length != 32
                || !Guid.TryParseExact(name, "N", out _)
                || (extension != ".jpg" && extension != ".png"))
            {
                throw new ArgumentException("Invalid profile photo path.");
            }

            var fullPath = Path.GetFullPath(
                Path.Combine(_profileDirectory, fileName));

            var allowedDirectory =
                _profileDirectory + Path.DirectorySeparatorChar;

            if (!fullPath.StartsWith(
                allowedDirectory,
                StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Invalid profile photo path.");
            }

            File.Delete(fullPath);

            return Task.CompletedTask;
        }
    }
}