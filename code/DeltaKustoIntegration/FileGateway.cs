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

                await foreach(var script in scripts)
                {
                    yield return script;
                }
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