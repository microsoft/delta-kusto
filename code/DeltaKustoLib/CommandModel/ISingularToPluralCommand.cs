using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeltaKustoLib.CommandModel
{
    public interface ISingularToPluralCommand
    {
        IEnumerable<CommandBase> ToPlural(IEnumerable<CommandBase> singularCommands);
    }
}