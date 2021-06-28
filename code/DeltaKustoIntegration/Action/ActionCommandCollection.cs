using DeltaKustoLib;
using DeltaKustoLib.CommandModel;
using DeltaKustoLib.CommandModel.Policies;
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
            DeleteCachingPolicyCommands = commands
                .OfType<DeleteCachingPolicyCommand>()
                .OrderBy(d => $"{(d.EntityType == EntityType.Database ? 1 : 2)}{d.EntityName}")
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
            AlterUpdatePolicyCommands = commands
                .OfType<AlterUpdatePolicyCommand>()
                .OrderBy(p => p.TableName)
                .ToImmutableArray();
            AlterCachingPolicyCommands = commands
                .OfType<AlterCachingPolicyCommand>()
                .OrderBy(d => $"{(d.EntityType == EntityType.Database ? 1 : 2)}{d.EntityName}")
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
                .Concat(DeleteCachingPolicyCommands)
                .Concat(DropFunctionCommands)
                .Concat(CreateTableCommands)
                .Concat(AlterMergeTableColumnDocStringsCommands)
                .Concat(CreateMappingCommands)
                .Concat(AlterUpdatePolicyCommands)
                .Concat(AlterCachingPolicyCommands)
                .Concat(CreateFunctionCommands);

            if (_allCommands.Count() != commands.Count())
            {
                throw new DeltaException("Commands count mismatch");
            }
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

        public IImmutableList<DeleteCachingPolicyCommand> DeleteCachingPolicyCommands { get; }

        public IImmutableList<DropFunctionCommand> DropFunctionCommands { get; }

        public IImmutableList<CreateTableCommand> CreateTableCommands { get; }

        public IImmutableList<AlterColumnTypeCommand> AlterColumnTypeCommands { get; }

        public IImmutableList<AlterMergeTableColumnDocStringsCommand>
            AlterMergeTableColumnDocStringsCommands
        { get; }

        public IImmutableList<CreateMappingCommand> CreateMappingCommands { get; }

        public IImmutableList<AlterUpdatePolicyCommand> AlterUpdatePolicyCommands { get; }

        public IImmutableList<AlterCachingPolicyCommand> AlterCachingPolicyCommands { get; }

        public IImmutableList<CreateFunctionCommand> CreateFunctionCommands { get; }
    }
}