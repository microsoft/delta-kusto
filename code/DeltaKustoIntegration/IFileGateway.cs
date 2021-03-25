using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DeltaKustoIntegration
{
    public interface IFileGateway
    {
        Task<string> GetFileContentAsync(string filePath, CancellationToken ct);

        Task SetFileContentAsync(string filePath, string content, CancellationToken ct);

        IAsyncEnumerable<(string path, string content)> GetFolderContentsAsync(
            string folderPath,
            IEnumerable<string>? extensions,
            CancellationToken ct);
    }
}