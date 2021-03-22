using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DeltaKustoApi.Controllers.Root
{
    public class RootInfo
    {
        public ApiInfo ApiInfo { get; set; } = new ApiInfo();
    }
}