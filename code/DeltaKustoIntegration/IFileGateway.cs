using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DeltaKustoIntegration
{
    public interface IFileGateway
    {
        IFileGateway ChangeFolder(string folderPath);

        Task<string> GetFileContentAsync(string filePath, CancellationToken ct = default);

        Task SetFileContentAsync(string filePath, string content, CancellationToken ct = default);

        IAsyncEnumerable<(string path, string content)> GetFolderContentsAsync(
            IEnumerable<string>? extensions,
            CancellationToken ct = default);
    }
}