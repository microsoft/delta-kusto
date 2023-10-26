﻿using DeltaKustoLib.CommandModel;
using DeltaKustoLib.CommandModel.Policies.AutoDelete;
using DeltaKustoLib.CommandModel.Policies.Caching;
using DeltaKustoLib.CommandModel.Policies.IngestionBatching;
using DeltaKustoLib.CommandModel.Policies.IngestionTime;
using DeltaKustoLib.CommandModel.Policies.Merge;
using DeltaKustoLib.CommandModel.Policies.Partitioning;
using DeltaKustoLib.CommandModel.Policies.RestrictedView;
using DeltaKustoLib.CommandModel.Policies.Retention;
using DeltaKustoLib.CommandModel.Policies.RowLevelSecurity;
using DeltaKustoLib.CommandModel.Policies.Sharding;
using DeltaKustoLib.CommandModel.Policies.StreamingIngestion;
using DeltaKustoLib.CommandModel.Policies.Update;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;

namespace DeltaKustoLib.KustoModel
{
    public class DatabaseModel
    {
        private readonly IImmutableList<CreateFunctionCommand> _functionCommands;
        private readonly IImmutableList<TableModel> _tableModels;
        private readonly AlterCachingPolicyCommand? _cachingPolicy;
        private readonly AlterIngestionBatchingPolicyCommand? _ingestionBatchingPolicy;
        private readonly AlterMergePolicyCommand? _mergePolicy;
        private readonly AlterRetentionPolicyCommand? _retentionPolicy;
        private readonly AlterShardingPolicyCommand? _shardingPolicy;
        private readonly AlterStreamingIngestionPolicyCommand? _streamingIngestionPolicy;

        private DatabaseModel(
            IEnumerable<CreateFunctionCommand> functionCommands,
            IEnumerable<TableModel> tableModels,
            AlterCachingPolicyCommand? cachingPolicy,
            AlterIngestionBatchingPolicyCommand? ingestionBatchingPolicy,
            AlterMergePolicyCommand? mergePolicy,
            AlterRetentionPolicyCommand? retentionPolicy,
            AlterShardingPolicyCommand? shardingPolicy,
            AlterStreamingIngestionPolicyCommand? streamingIngestionPolicy)
        {
            if (cachingPolicy != null && cachingPolicy.EntityType != EntityType.Database)
            {
                throw new NotSupportedException("Only db caching policy is supported in this context");
            }
            if (ingestionBatchingPolicy != null && ingestionBatchingPolicy.EntityType != EntityType.Database)
            {
                throw new NotSupportedException("Only db ingestion batching policy is supported in this context");
            }
            if (mergePolicy != null && mergePolicy.EntityType != EntityType.Database)
            {
                throw new NotSupportedException("Only db merge policy is supported in this context");
            }
            if (shardingPolicy != null && shardingPolicy.EntityType != EntityType.Database)
            {
                throw new NotSupportedException("Only db sharding policy is supported in this context");
            }
            if (streamingIngestionPolicy != null
                && streamingIngestionPolicy.EntityType != EntityType.Database)
            {
                throw new NotSupportedException(
                    "Only db streaming ingestion policy is supported in this context");
            }
            _functionCommands = functionCommands
                .OrderBy(f => f.FunctionName)
                .ToImmutableArray();
            _tableModels = tableModels
                .OrderBy(t => t.TableName)
                .ToImmutableArray();
            _cachingPolicy = cachingPolicy;
            _ingestionBatchingPolicy = ingestionBatchingPolicy;
            _mergePolicy = mergePolicy;
            _retentionPolicy = retentionPolicy;
            _shardingPolicy = shardingPolicy;
            _streamingIngestionPolicy = streamingIngestionPolicy;
        }

        public static DatabaseModel FromCommands(
            IEnumerable<CommandBase> commands)
        {
            var commandTypeIndex = commands
                .GroupBy(c => c.GetType())
                .ToImmutableDictionary(g => g.Key, g => g as IEnumerable<CommandBase>);
            var commandTypes = commandTypeIndex
                .Keys
                .Select(key => (key, commandTypeIndex[key].First().CommandFriendlyName));
            var createFunctions = GetCommands<CreateFunctionCommand>(commandTypeIndex);
            //  Flatten the .create tables to integrate them with .create table
            var createPluralTables = GetCommands<CreateTablesCommand>(commandTypeIndex)
                .SelectMany(c => c.Tables.Select(t => new CreateTableCommand(
                    t.TableName,
                    t.Columns,
                    c.Folder,
                    c.DocString)));
            var createTables = GetCommands<CreateTableCommand>(commandTypeIndex)
                .Concat(createPluralTables)
                .ToImmutableArray();
            var alterMergeTableColumns =
                GetCommands<AlterMergeTableColumnDocStringsCommand>(commandTypeIndex);
            var alterMergeTableSingleColumn = alterMergeTableColumns
                .SelectMany(a => a.Columns.Select(
                    c => new AlterMergeTableColumnDocStringsCommand(a.TableName, new[] { c })));
            var createMappings = GetCommands<CreateMappingCommand>(commandTypeIndex)
                .ToImmutableArray();
            var autoDeletePolicies = GetCommands<AlterAutoDeletePolicyCommand>(commandTypeIndex)
                .ToImmutableArray();
            var cachingPolicies = GetCommands<AlterCachingPolicyCommand>(commandTypeIndex)
                .ToImmutableArray();
            var tableCachingPluralPolicies =
                GetCommands<AlterCachingPluralPolicyCommand>(commandTypeIndex)
                .Select(c => c.TableNames.Select(t => new AlterCachingPolicyCommand(
                    EntityType.Table,
                    t,
                    c.HotData.Duration!.Value,
                    c.HotIndex.Duration!.Value,
                    c.HotWindows)))
                .SelectMany(e => e);
            var tableCachingPolicies = cachingPolicies
                .Where(p => p.EntityType == EntityType.Table)
                .Concat(tableCachingPluralPolicies);
            var dbCachingPolicies = cachingPolicies
                .Where(p => p.EntityType == EntityType.Database);
            var ingestionBatchingPolicies = GetCommands<AlterIngestionBatchingPolicyCommand>(commandTypeIndex)
                .ToImmutableArray();
            var tableIngestionBatchingPolicies = ingestionBatchingPolicies
                .Where(p => p.EntityType == EntityType.Table);
            var dbIngestionBatchingPolicies = ingestionBatchingPolicies
                .Where(p => p.EntityType == EntityType.Database);
            var mergePolicies = GetCommands<AlterMergePolicyCommand>(commandTypeIndex)
                .ToImmutableArray();
            var tableMergePolicies = mergePolicies
                .Where(p => p.EntityType == EntityType.Table);
            var dbMergePolicies = mergePolicies
                .Where(p => p.EntityType == EntityType.Database);
            var shardingPolicies = GetCommands<AlterShardingPolicyCommand>(commandTypeIndex)
                .ToImmutableArray();
            var tableShardingPolicies = shardingPolicies
                .Where(p => p.EntityType == EntityType.Table);
            var dbShardingPolicies = shardingPolicies
                .Where(p => p.EntityType == EntityType.Database);
            var streamingIngestionPolicies =
                GetCommands<AlterStreamingIngestionPolicyCommand>(commandTypeIndex)
                .ToImmutableArray();
            var tableStreamingIngestionPolicies = streamingIngestionPolicies
                .Where(p => p.EntityType == EntityType.Table);
            var dbStreamingIngestionPolicies = streamingIngestionPolicies
                .Where(p => p.EntityType == EntityType.Database);
            var tablePartitioningPolicies =
                GetCommands<AlterPartitioningPolicyCommand>(commandTypeIndex);
            var retentionTablePluralPolicies = GetCommands<AlterRetentionPluralTablePolicyCommand>(commandTypeIndex)
                .SelectMany(c => c.TableNames.Select(t => new AlterRetentionPolicyCommand(
                    EntityType.Table,
                    t,
                    c.Policy)));
            var retentionPolicies = GetCommands<AlterRetentionPolicyCommand>(commandTypeIndex)
                .ToImmutableArray();
            var tableRetentionPolicies = retentionPolicies
                .Where(p => p.EntityType == EntityType.Table)
                .Concat(retentionTablePluralPolicies);
            var dbRetentionPolicies = retentionPolicies
                .Where(p => p.EntityType == EntityType.Database);
            var tableRowLevelSecurityPolicies =
                GetCommands<AlterRowLevelSecurityPolicyCommand>(commandTypeIndex)
                .ToImmutableArray();
            var tableRestrictedViewPluralPolicies =
                GetCommands<AlterRestrictedViewPluralPolicyCommand>(commandTypeIndex)
                .Select(c => c.TableNames.Select(t => new AlterRestrictedViewPolicyCommand(t, c.AreEnabled)))
                .SelectMany(e => e);
            var tableRestrictedViewPolicies =
                GetCommands<AlterRestrictedViewPolicyCommand>(commandTypeIndex)
                .Concat(tableRestrictedViewPluralPolicies)
                .ToImmutableArray();
            var tableIngestionTimePluralPolicies =
                GetCommands<AlterIngestionTimePluralPolicyCommand>(commandTypeIndex)
                .Select(c => c.TableNames.Select(t => new AlterIngestionTimePolicyCommand(t, c.AreEnabled)))
                .SelectMany(e => e);
            var tableIngestionTimePolicies =
                GetCommands<AlterIngestionTimePolicyCommand>(commandTypeIndex)
                .Concat(tableIngestionTimePluralPolicies)
                .ToImmutableArray();
            var updatePolicies = GetCommands<AlterUpdatePolicyCommand>(commandTypeIndex)
                .ToImmutableArray();

            ValidateDuplicates(createFunctions, f => f.FunctionName.Name);
            ValidateDuplicates(createTables, t => t.TableName.Name);
            ValidateDuplicates(
                alterMergeTableSingleColumn,
                a => $"{a.TableName}_{a.Columns.First().ColumnName}");
            ValidateDuplicates(
                createMappings,
                m => $"{m.TableName}_{m.MappingName}_{m.MappingKind}");
            ValidateDuplicates(autoDeletePolicies, m => m.TableName.Name);
            ValidateDuplicates(tableCachingPolicies, m => m.EntityName.Name);
            ValidateDuplicates(dbCachingPolicies, m => "Database caching policy");
            ValidateDuplicates(tableIngestionBatchingPolicies, m => m.EntityName.Name);
            ValidateDuplicates(dbIngestionBatchingPolicies, m => "Database ingestion batching policy");
            ValidateDuplicates(tableMergePolicies, m => m.EntityName.Name);
            ValidateDuplicates(dbMergePolicies, m => "Database merge policy");
            ValidateDuplicates(tableShardingPolicies, m => m.EntityName.Name);
            ValidateDuplicates(dbShardingPolicies, m => "Database sharding policy");
            ValidateDuplicates(tableStreamingIngestionPolicies, m => m.EntityName.Name);
            ValidateDuplicates(dbStreamingIngestionPolicies, m => "Database sharding policy");
            ValidateDuplicates(tablePartitioningPolicies, m => m.TableName.Name);
            ValidateDuplicates(tableRetentionPolicies, m => m.EntityName.Name);
            ValidateDuplicates(dbRetentionPolicies, m => "Database retention policy");
            ValidateDuplicates(tableRowLevelSecurityPolicies, m => m.TableName.Name);
            ValidateDuplicates(tableRestrictedViewPolicies, m => m.TableName.Name);
            ValidateDuplicates(tableIngestionTimePolicies, m => m.TableName.Name);
            ValidateDuplicates(updatePolicies, m => m.TableName.Name);

            var tableModels = TableModel.FromCommands(
                createTables,
                alterMergeTableColumns,
                createMappings,
                autoDeletePolicies,
                tableCachingPolicies,
                tableIngestionBatchingPolicies,
                tableMergePolicies,
                tableRetentionPolicies,
                tableShardingPolicies,
                tableStreamingIngestionPolicies,
                tablePartitioningPolicies,
                tableRowLevelSecurityPolicies,
                tableRestrictedViewPolicies,
                tableIngestionTimePolicies,
                updatePolicies);

            return new DatabaseModel(
                createFunctions,
                tableModels,
                dbCachingPolicies.FirstOrDefault(),
                dbIngestionBatchingPolicies.FirstOrDefault(),
                dbMergePolicies.FirstOrDefault(),
                dbRetentionPolicies.FirstOrDefault(),
                dbShardingPolicies.FirstOrDefault(),
                dbStreamingIngestionPolicies.FirstOrDefault());
        }

        public IImmutableList<CommandBase> ComputeDelta(DatabaseModel targetModel)
        {
            var functionCommands =
                CreateFunctionCommand.ComputeDelta(_functionCommands, targetModel._functionCommands);
            var tableCommands =
                TableModel.ComputeDelta(_tableModels, targetModel._tableModels);
            var cachingPolicyCommands =
                AlterCachingPolicyCommand.ComputeDelta(_cachingPolicy, targetModel._cachingPolicy);
            var ingestionBatchingPolicyCommands = AlterIngestionBatchingPolicyCommand.ComputeDelta(
                _ingestionBatchingPolicy,
                targetModel._ingestionBatchingPolicy);
            var mergePolicyCommands =
                AlterMergePolicyCommand.ComputeDelta(_mergePolicy, targetModel._mergePolicy);
            var retentionPolicyCommands = AlterRetentionPolicyCommand.ComputeDelta(
                _retentionPolicy,
                targetModel._retentionPolicy);
            var shardingPolicyCommands =
                AlterShardingPolicyCommand.ComputeDelta(_shardingPolicy, targetModel._shardingPolicy);
            var streamingIngestionPolicyCommands =
                AlterStreamingIngestionPolicyCommand.ComputeDelta(
                    _streamingIngestionPolicy,
                    targetModel._streamingIngestionPolicy);
            var deltaCommands = functionCommands
                .Concat(tableCommands)
                .Concat(cachingPolicyCommands)
                .Concat(ingestionBatchingPolicyCommands)
                .Concat(mergePolicyCommands)
                .Concat(retentionPolicyCommands)
                .Concat(shardingPolicyCommands)
                .Concat(streamingIngestionPolicyCommands);

            return deltaCommands.ToImmutableArray();
        }

        #region Object methods
        public override bool Equals(object? obj)
        {
            var other = obj as DatabaseModel;
            var result = other != null
                && Enumerable.SequenceEqual(_functionCommands, other._functionCommands)
                && Enumerable.SequenceEqual(_tableModels, other._tableModels)
                && object.Equals(_cachingPolicy, other._cachingPolicy)
                && object.Equals(_ingestionBatchingPolicy, other._ingestionBatchingPolicy)
                && object.Equals(_mergePolicy, other._mergePolicy)
                && object.Equals(_retentionPolicy, other._retentionPolicy)
                && object.Equals(_shardingPolicy, other._shardingPolicy)
                && object.Equals(_streamingIngestionPolicy, other._streamingIngestionPolicy);

            return result;
        }

        public override int GetHashCode()
        {
            return _functionCommands.Aggregate(0, (h, f) => h ^ f.GetHashCode())
                ^ _tableModels.Aggregate(0, (h, t) => h ^ t.GetHashCode());
        }
        #endregion

        private static IImmutableList<T> GetCommands<T>(
            IImmutableDictionary<Type, IEnumerable<CommandBase>> commandTypeIndex)
        {
            if (commandTypeIndex.ContainsKey(typeof(T)))
            {
                return commandTypeIndex[typeof(T)]
                    .Cast<T>()
                    .ToImmutableArray();
            }
            else
            {
                return ImmutableArray<T>.Empty;
            }
        }

        private static void ValidateDuplicates<T>(
            IEnumerable<T> commands,
            Func<T, string> keyExtractor)
            where T : CommandBase
        {
            var duplicates = commands
                .GroupBy(o => keyExtractor(o))
                .Where(g => g.Count() > 1)
                .Select(g => new
                {
                    Name = g.Key,
                    CommandFriendlyName = g.First().CommandFriendlyName,
                    Count = g.Count()
                });
            var duplicate = duplicates.FirstOrDefault();

            if (duplicate != null)
            {
                var duplicateText = string.Join(
                    ", ",
                    duplicates.Select(d => $"(Name = '{d.Name}', Count = {d.Count})"));

                throw new DeltaException(
                    $"{duplicate.CommandFriendlyName} have duplicates:  {{ {duplicateText} }}");
            }
        }
    }
}