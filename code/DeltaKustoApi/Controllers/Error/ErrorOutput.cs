using System;

namespace DeltaKustoApi.Controllers.Error
{
    public class ErrorOutput
    {
        public ApiInfo ApiInfo { get; set; } = new ApiInfo();

        public Guid OperationID { get; set; } = Guid.NewGuid();
    }
}