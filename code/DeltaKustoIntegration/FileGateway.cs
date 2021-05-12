using DeltaKustoLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DeltaKustoIntegration
{
    public class FileGateway : IFileGateway
    {
        private static readonly TimeSpan TIMEOUT = TimeSpan.FromSeconds(2);
        private readonly string _rootFolder;

        public FileGateway() : this(string.Empty)
        {
        }

        private FileGateway(string rootFolder)
        {
            _rootFolder = rootFolder;
        }

        IFileGateway IFileGateway.ChangeFolder(string folderPath)
        {
            var newRootFolder = Path.Combine(_rootFolder, folderPath);

            return new FileGateway(newRootFolder);
        }

        async Task<string> IFileGateway.GetFileContentAsync(
            string filePath,
            CancellationToken ct)
        {
            var path = Path.Combine(_rootFolder, filePath);
            var text = await File.ReadAllTextAsync(
                path,
                Encoding.UTF8,
                CancellationTokenHelper.MergeCancellationToken(ct, TIMEOUT));

            return text;
        }

        async Task IFileGateway.SetFileContentAsync(
            string filePath,
            string content,
            CancellationToken ct)
        {
            var path = Path.Combine(_rootFolder, filePath);
            var directory = Path.GetDirectoryName(path);

            if (!string.IsNullOrWhiteSpace(directory))
            {
                EnsureDirectoryExists(directory);
            }

            await File.WriteAllTextAsync(
                path,
                content,
                CancellationTokenHelper.MergeCancellationToken(ct, TIMEOUT));
        }

        async IAsyncEnumerable<(string path, string content)> IFileGateway.GetFolderContentsAsync(
            IEnumerable<string>? extensions,
            [EnumeratorCancellation]
            CancellationToken ct)
        {
            var fileGateway = (IFileGateway)this;
            var directories = Directory.GetDirectories(_rootFolder);
            var files = Directory.GetFiles(_rootFolder);

            foreach (var file in files)
            {
                if (HasExtension(file, extensions))
                {
                    var fileName = Path.GetFileName(file);
                    var script = await fileGateway.GetFileContentAsync(fileName, ct);

                    yield return (fileName, script);
                }
            }
            foreach (var directory in directories)
            {
                var directoryName = Path.GetDirectoryName(directory)!;
                var localFileGateway = fileGateway.ChangeFolder(directoryName);
                var scripts = fileGateway.GetFolderContentsAsync(extensions, ct);

                await foreach (var script in scripts)
                {
                    yield return (Path.Combine(directoryName, script.path), script.content);
                }
            }
        }

        private void EnsureDirectoryExists(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
        }

        private bool HasExtension(string filePath, IEnumerable<string>? extensions)
        {
            if (extensions == null)
            {
                return true;
            }
            else
            {
                var extensionMatch = extensions
                    .Where(e => filePath.EndsWith("." + e));
                var match = extensionMatch.Any();

                return match;
            }
        }
    }
}