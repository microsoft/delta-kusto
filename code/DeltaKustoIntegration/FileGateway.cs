using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace DeltaKustoIntegration
{
    public class FileGateway : IFileGateway
    {
        Task<string> IFileGateway.GetFileContentAsync(string filePath)
        {
            var text = File.ReadAllTextAsync(filePath);

            return text;
        }
    }
}