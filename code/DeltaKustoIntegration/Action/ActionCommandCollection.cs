using DeltaKustoLib.CommandModel;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeltaKustoIntegration.Action
{
    public class ActionCommandCollection : IReadOnlyCollection<CommandBase>
    {
        private readonly IEnumerable<CommandBase> _allCommands;

        public ActionCommandCollection(IEnumerable<CommandBase> commands)
        {
            DropFunctionCommands = commands
                .OfType<DropFunctionCommand>()
                .OrderBy(d => d.ObjectName)
                .ToImmutableArray();
            CreateFunctionCommands = commands
                .OfType<CreateFunctionCommand>()
                .OrderBy(d => d.Folder)
                .ThenBy(d => d.ObjectName)
                .ToImmutableArray();
            AllDropCommands = DropFunctionCommands;
            _allCommands = AllDropCommands
                .Concat(CreateFunctionCommands);
        }

        #region IReadOnlyCollection<CommandBase> methods
        int IReadOnlyCollection<CommandBase>.Count => _allCommands.Count();

        IEnumerator<CommandBase> IEnumerable<CommandBase>.GetEnumerator()
        {
            return _allCommands.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _allCommands.GetEnumerator();
        }
        #endregion

        public IEnumerable<CommandBase> AllDropCommands { get; }

        public IImmutableList<DropFunctionCommand> DropFunctionCommands { get; }
        
        public IImmutableList<CreateFunctionCommand> CreateFunctionCommands { get; }
    }
}