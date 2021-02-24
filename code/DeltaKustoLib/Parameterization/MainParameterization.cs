using System;
using System.Collections.Generic;
using System.Text;

namespace DeltaKustoLib.Parameterization
{
    public class MainParameterization
    {
        public int? ServicePrincipal { get; set; }

        public SourceParameterization? Current { get; set; }
        
        public SourceParameterization? Target { get; set; }

        public ActionParameterization? Action { get; set; }
    }
}