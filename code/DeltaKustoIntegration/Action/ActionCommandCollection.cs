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

        public ActionCommandCollection(bool usePluralForms, IEnumerable<CommandBase> commands)
        {
            if (usePluralForms)
            {
                DropTableCommands = ImmutableArray<DropTableCommand>.Empty;
                DropTablesCommands = commands
                    .OfType<DropTableCommand>()
                    .MergeToPlural()
                    .OrderBy(c => c.TableNames.First().Name)
                    .ToImmutableArray();
                DropFunctionCommands = ImmutableArray<DropFunctionCommand>.Empty;
                DropFunctionsCommands = commands
                    .OfType<DropFunctionCommand>()
                    .MergeToPlural()
                    .OrderBy(d => d.FunctionNames.First().Name)
                    .ToImmutableArray();
                CreateTableCommands = ImmutableArray<CreateTableCommand>.Empty;
                CreateTablesCommands = commands
                    .OfType<CreateTableCommand>()
                    .MergeToPlural()
                    .OrderBy(d => d.Folder)
                    .ThenBy(d => d.Tables.First().TableName)
                    .ToImmutableArray();
                AlterRetentionPolicyCommands = commands
                    .OfType<AlterRetentionPolicyCommand>()
                    .Where(c => c.EntityType != EntityType.Table)
                    .OrderBy(c => c.EntityName)
                    .ToImmutableArray();
                AlterTablesRetentionPolicyCommands = commands
                    .OfType<AlterRetentionPolicyCommand>()
                    .Where(c => c.EntityType == EntityType.Table)
                    .MergeToPlural()
                    .OrderBy(c => c.TableNames.First())
                    .ToImmutableArray();
            }
            else
            {
                DropTableCommands = commands
                    .OfType<DropTableCommand>()
                    .OrderBy(d => d.TableName)
                    .ToImmutableArray();
                DropTablesCommands = ImmutableArray<DropTablesCommand>.Empty;
                DropFunctionCommands = commands
                    .OfType<DropFunctionCommand>()
                    .OrderBy(d => d.FunctionName)
                    .ToImmutableArray();
                DropFunctionsCommands = ImmutableArray<DropFunctionsCommand>.Empty;
                CreateTableCommands = commands
                    .OfType<CreateTableCommand>()
                    .OrderBy(d => d.Folder)
                    .ThenBy(d => d.TableName)
                    .ToImmutableArray();
                CreateTablesCommands = ImmutableArray<CreateTablesCommand>.Empty;
                AlterRetentionPolicyCommands = commands
                    .OfType<AlterRetentionPolicyCommand>()
                    .OrderBy(d => $"{(d.EntityType == EntityType.Database ? 1 : 2)}{d.EntityName}")
                    .ToImmutableArray();
                AlterTablesRetentionPolicyCommands =
                    ImmutableArray<AlterTablesRetentionPolicyCommand>.Empty;
            }
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
            DeleteRetentionPolicyCommands = commands
                .OfType<DeleteRetentionPolicyCommand>()
                .OrderBy(d => $"{(d.EntityType == EntityType.Database ? 1 : 2)}{d.EntityName}")
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
                .Concat(DeleteRetentionPolicyCommands)
                .Concat(DropFunctionCommands)
                .Concat(CreateTableCommands)
                .Concat(AlterMergeTableColumnDocStringsCommands)
                .Concat(CreateMappingCommands)
                .Concat(AlterUpdatePolicyCommands)
                .Concat(AlterCachingPolicyCommands)
                .Concat(AlterRetentionPolicyCommands)
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

        public IImmutableList<DropTablesCommand> DropTablesCommands { get; }

        public IImmutableList<DropTableColumnsCommand> DropTableColumnsCommands { get; }

        public IImmutableList<DropMappingCommand> DropMappingCommands { get; }

        public IImmutableList<DeleteCachingPolicyCommand> DeleteCachingPolicyCommands { get; }

        public IImmutableList<DeleteRetentionPolicyCommand> DeleteRetentionPolicyCommands { get; }

        public IImmutableList<DropFunctionCommand> DropFunctionCommands { get; }

        public IImmutableList<DropFunctionsCommand> DropFunctionsCommands { get; }

        public IImmutableList<CreateTableCommand> CreateTableCommands { get; }

        public IImmutableList<CreateTablesCommand> CreateTablesCommands { get; }

        public IImmutableList<AlterColumnTypeCommand> AlterColumnTypeCommands { get; }

        public IImmutableList<AlterMergeTableColumnDocStringsCommand>
            AlterMergeTableColumnDocStringsCommands
        { get; }

        public IImmutableList<CreateMappingCommand> CreateMappingCommands { get; }

        public IImmutableList<AlterUpdatePolicyCommand> AlterUpdatePolicyCommands { get; }

        public IImmutableList<AlterCachingPolicyCommand> AlterCachingPolicyCommands { get; }

        public IImmutableList<AlterRetentionPolicyCommand> AlterRetentionPolicyCommands { get; }

        public IImmutableList<AlterTablesRetentionPolicyCommand> AlterTablesRetentionPolicyCommands { get; }

        public IImmutableList<CreateFunctionCommand> CreateFunctionCommands { get; }
    }
}