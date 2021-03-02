using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DeltaKustoIntegration
{
    public interface IFileGateway
    {
        Task<string> GetFileContentAsync(string filePath);
    }
}