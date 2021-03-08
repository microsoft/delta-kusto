﻿using DeltaKustoLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeltaKustoIntegration
{
    public class FileGateway : IFileGateway
    {
        async Task<string> IFileGateway.GetFileContentAsync(string filePath)
        {
            var text = await File.ReadAllTextAsync(filePath);

            return text;
        }

        async Task IFileGateway.SetFileContentAsync(string filePath, string content)
        {
            var directory = Path.GetDirectoryName(filePath);

            if (directory != null)
            {
                EnsureDirectoryExists(directory);
            }

            await File.WriteAllTextAsync(filePath, content);
        }

        async IAsyncEnumerable<(string path, string content)> IFileGateway.GetFolderContentsAsync(
            string folderPath,
            IEnumerable<string>? extensions)
        {
            var fileGateway = (IFileGateway)this;
            var directories = Directory.GetDirectories(folderPath);
            var files = Directory.GetFiles(folderPath);

            foreach (var file in files)
            {
                if (HasExtension(file, extensions))
                {
                    var script = await fileGateway.GetFileContentAsync(file);

                    yield return (file, script);
                }
            }
            foreach (var directory in directories)
            {
                var scripts = fileGateway.GetFolderContentsAsync(directory, extensions);

                await foreach (var script in scripts)
                {
                    yield return script;
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