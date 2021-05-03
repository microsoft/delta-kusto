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
            DropTableCommands = commands
                .OfType<DropTableCommand>()
                .OrderBy(d => d.TableName)
                .ToImmutableArray();
            DropTableColumnsCommands = commands
                .OfType<DropTableColumnsCommand>()
                .OrderBy(d => d.TableName)
                .ToImmutableArray();
            DropMappingCommands = commands
                .OfType<DropMappingCommand>()
                .OrderBy(d => d.MappingName)
                .ThenBy(d => d.MappingKind)
                .ToImmutableArray();
            DropFunctionCommands = commands
                .OfType<DropFunctionCommand>()
                .OrderBy(d => d.FunctionName)
                .ToImmutableArray();
            CreateTableCommands = commands
                .OfType<CreateTableCommand>()
                .OrderBy(d => d.Folder)
                .ThenBy(d => d.TableName)
                .ToImmutableArray();
            AlterColumnTypeCommands = commands
                .OfType<AlterColumnTypeCommand>()
                .OrderBy(d => d.TableName)
                .ThenBy(d => d.ColumnName)
                .ToImmutableArray();
            AlterMergeTableColumnDocStringsCommands = commands
                .OfType<AlterMergeTableColumnDocStringsCommand>()
                .OrderBy(d => d.TableName)
                .ToImmutableArray();
            CreateMappingCommands = commands
                .OfType<CreateMappingCommand>()
                .OrderBy(d => d.MappingName)
                .ThenBy(d => d.MappingKind)
                .ToImmutableArray();
            CreateFunctionCommands = commands
                .OfType<CreateFunctionCommand>()
                .OrderBy(d => d.Folder.Text)
                .ThenBy(d => d.FunctionName)
                .ToImmutableArray();
            AllDataLossCommands = DropTableCommands
                .Cast<CommandBase>()
                .Concat(DropTableColumnsCommands)
                .Concat(AlterColumnTypeCommands);
            _allCommands = AllDataLossCommands
                .Concat(DropMappingCommands)
                .Concat(DropFunctionCommands)
                .Concat(CreateTableCommands)
                .Concat(AlterMergeTableColumnDocStringsCommands)
                .Concat(CreateMappingCommands)
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

        public IEnumerable<CommandBase> AllDataLossCommands { get; }

        public IImmutableList<DropTableCommand> DropTableCommands { get; }

        public IImmutableList<DropTableColumnsCommand> DropTableColumnsCommands { get; }

        public IImmutableList<DropMappingCommand> DropMappingCommands { get; }

        public IImmutableList<DropFunctionCommand> DropFunctionCommands { get; }

        public IImmutableList<CreateTableCommand> CreateTableCommands { get; }

        public IImmutableList<AlterColumnTypeCommand> AlterColumnTypeCommands { get; }

        public IImmutableList<AlterMergeTableColumnDocStringsCommand>
            AlterMergeTableColumnDocStringsCommands
        { get; }

        public IImmutableList<CreateMappingCommand> CreateMappingCommands { get; }

        public IImmutableList<CreateFunctionCommand> CreateFunctionCommands { get; }
    }
}