using DeltaKustoIntegration;
using DeltaKustoIntegration.Parameterization;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace delta_kusto
{
    internal class DeltaOrchestration
    {
        private readonly IFileGateway _fileGateway;

        public DeltaOrchestration(IFileGateway fileGateway)
        {
            _fileGateway = fileGateway;
        }

        public async Task ComputeDeltaAsync(string parameterFilePath)
        {
            var parameterText = await _fileGateway.GetFileContentAsync(parameterFilePath);
            var parameters = JsonSerializer.Deserialize<MainParameterization>(
                parameterText,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            parameters.Validate();

            throw new NotImplementedException();
        }
    }
}