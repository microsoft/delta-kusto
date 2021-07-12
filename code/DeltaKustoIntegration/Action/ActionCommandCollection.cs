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
                AlterTablesRetentionPolicyCommands = commands
                    .OfType<AlterRetentionPolicyCommand>()
                    .Where(c => c.EntityType == EntityType.Table)
                    .MergeToPlural()
                    .OrderBy(c => c.TableNames.First())
                    .ToImmutableArray();
                AlterRetentionPolicyCommands = commands
                    .OfType<AlterRetentionPolicyCommand>()
                    .Where(c => c.EntityType != EntityType.Table)
                    .OrderBy(c => c.EntityName)
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
                AlterTablesRetentionPolicyCommands =
                    ImmutableArray<AlterTablesRetentionPolicyCommand>.Empty;
                AlterRetentionPolicyCommands = commands
                   .OfType<AlterRetentionPolicyCommand>()
                   .OrderBy(d => $"{(d.EntityType == EntityType.Database ? 1 : 2)}{d.EntityName}")
                   .ToImmutableArray();
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

            #region Policies
            AlterAutoDeletePolicyCommands = commands
                .OfType<AlterAutoDeletePolicyCommand>()
                .OrderBy(d => d.TableName)
                .ToImmutableArray();
            DeleteAutoDeletePolicyCommands = commands
                .OfType<DeleteAutoDeletePolicyCommand>()
                .OrderBy(d => d.TableName)
                .ToImmutableArray();
            AlterCachingPolicyCommands = commands
                .OfType<AlterCachingPolicyCommand>()
                .OrderBy(d => $"{(d.EntityType == EntityType.Database ? 1 : 2)}{d.EntityName}")
                .ToImmutableArray();
            DeleteCachingPolicyCommands = commands
                .OfType<DeleteCachingPolicyCommand>()
                .OrderBy(d => $"{(d.EntityType == EntityType.Database ? 1 : 2)}{d.EntityName}")
                .ToImmutableArray();
            AlterIngestionBatchingPolicyCommands = commands
                .OfType<AlterIngestionBatchingPolicyCommand>()
                .OrderBy(d => $"{(d.EntityType == EntityType.Database ? 1 : 2)}{d.EntityName}")
                .ToImmutableArray();
            DeleteIngestionBatchingPolicyCommands = commands
                .OfType<DeleteIngestionBatchingPolicyCommand>()
                .OrderBy(d => $"{(d.EntityType == EntityType.Database ? 1 : 2)}{d.EntityName}")
                .ToImmutableArray();
            AlterMergePolicyCommands = commands
                .OfType<AlterMergePolicyCommand>()
                .OrderBy(d => $"{(d.EntityType == EntityType.Database ? 1 : 2)}{d.EntityName}")
                .ToImmutableArray();
            DeleteMergePolicyCommands = commands
                .OfType<DeleteMergePolicyCommand>()
                .OrderBy(d => $"{(d.EntityType == EntityType.Database ? 1 : 2)}{d.EntityName}")
                .ToImmutableArray();
            DeleteRetentionPolicyCommands = commands
                .OfType<DeleteRetentionPolicyCommand>()
                .OrderBy(d => $"{(d.EntityType == EntityType.Database ? 1 : 2)}{d.EntityName}")
                .ToImmutableArray();
            AlterShardingPolicyCommands = commands
                .OfType<AlterShardingPolicyCommand>()
                .OrderBy(d => $"{(d.EntityType == EntityType.Database ? 1 : 2)}{d.EntityName}")
                .ToImmutableArray();
            DeleteShardingPolicyCommands = commands
                .OfType<DeleteShardingPolicyCommand>()
                .OrderBy(d => $"{(d.EntityType == EntityType.Database ? 1 : 2)}{d.EntityName}")
                .ToImmutableArray();
            AlterUpdatePolicyCommands = commands
                .OfType<AlterUpdatePolicyCommand>()
                .OrderBy(p => p.TableName)
                .ToImmutableArray();
            #endregion

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
                .Concat(CreateFunctionCommands)
                .Concat(AlterCachingPolicyCommands)
                .Concat(DeleteCachingPolicyCommands)
                .Concat(AlterMergePolicyCommands)
                .Concat(DeleteMergePolicyCommands)
                .Concat(AlterRetentionPolicyCommands)
                .Concat(DeleteRetentionPolicyCommands)
                .Concat(AlterUpdatePolicyCommands);

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

        public IImmutableList<DropFunctionCommand> DropFunctionCommands { get; }

        public IImmutableList<DropFunctionsCommand> DropFunctionsCommands { get; }

        public IImmutableList<CreateTableCommand> CreateTableCommands { get; }

        public IImmutableList<CreateTablesCommand> CreateTablesCommands { get; }

        public IImmutableList<AlterColumnTypeCommand> AlterColumnTypeCommands { get; }

        public IImmutableList<AlterMergeTableColumnDocStringsCommand>
            AlterMergeTableColumnDocStringsCommands
        { get; }

        public IImmutableList<CreateFunctionCommand> CreateFunctionCommands { get; }

        public IImmutableList<CreateMappingCommand> CreateMappingCommands { get; }

        #region Policies
        public IImmutableList<AlterAutoDeletePolicyCommand> AlterAutoDeletePolicyCommands { get; }

        public IImmutableList<DeleteAutoDeletePolicyCommand> DeleteAutoDeletePolicyCommands { get; }

        public IImmutableList<AlterCachingPolicyCommand> AlterCachingPolicyCommands { get; }

        public IImmutableList<DeleteCachingPolicyCommand> DeleteCachingPolicyCommands { get; }

        public IImmutableList<AlterIngestionBatchingPolicyCommand> AlterIngestionBatchingPolicyCommands { get; }

        public IImmutableList<DeleteIngestionBatchingPolicyCommand> DeleteIngestionBatchingPolicyCommands { get; }

        public IImmutableList<AlterMergePolicyCommand> AlterMergePolicyCommands { get; }

        public IImmutableList<DeleteMergePolicyCommand> DeleteMergePolicyCommands { get; }

        public IImmutableList<DeleteRetentionPolicyCommand> DeleteRetentionPolicyCommands { get; }

        public IImmutableList<AlterRetentionPolicyCommand> AlterRetentionPolicyCommands { get; }

        public IImmutableList<AlterTablesRetentionPolicyCommand> AlterTablesRetentionPolicyCommands { get; }

        public IImmutableList<AlterShardingPolicyCommand> AlterShardingPolicyCommands { get; }

        public IImmutableList<DeleteShardingPolicyCommand> DeleteShardingPolicyCommands { get; }

        public IImmutableList<AlterUpdatePolicyCommand> AlterUpdatePolicyCommands { get; }
        #endregion
    }
}