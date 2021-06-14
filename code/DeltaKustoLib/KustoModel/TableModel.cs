using DeltaKustoLib.CommandModel;
using DeltaKustoLib.SchemaObjects;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeltaKustoLib.KustoModel
{
    public class TableModel
    {
        public EntityName TableName { get; }

        public IImmutableList<ColumnModel> Columns { get; }

        public IImmutableList<MappingModel> Mappings { get; }

        public AlterUpdatePolicyCommand? UpdatePolicy { get; }

        public QuotedText? Folder { get; }

        public QuotedText? DocString { get; }

        private TableModel(
            EntityName tableName,
            IEnumerable<ColumnModel> columns,
            IEnumerable<MappingModel> mappings,
            AlterUpdatePolicyCommand? updatePolicy,
            QuotedText? folder,
            QuotedText? docString)
        {
            TableName = tableName;
            Columns = columns.ToImmutableArray();
            Mappings = mappings
                .OrderBy(m => m.MappingName)
                .ThenBy(m => m.MappingKind)
                .ToImmutableArray();
            UpdatePolicy = updatePolicy;
            Folder = folder;
            DocString = docString;
        }

        #region Object methods
        public override bool Equals(object? obj)
        {
            var other = obj as TableModel;
            var result = other != null
                && other.TableName.Equals(TableName)
                && other.Columns.OrderBy(c => c.ColumnName).SequenceEqual(
                    Columns.OrderBy(c => c.ColumnName))
                && other.Mappings.SequenceEqual(Mappings)
                && object.Equals(other.Folder, Folder)
                && object.Equals(other.DocString, DocString);

            return result;
        }

        public override int GetHashCode()
        {
            return TableName.Name.GetHashCode()
                ^ Columns.Aggregate(0, (h, c) => h ^ c.GetHashCode())
                ^ (Folder == null ? 0 : Folder.Text.GetHashCode())
                ^ (DocString == null ? 0 : DocString.Text.GetHashCode());
        }
        #endregion

        internal static IImmutableList<TableModel> FromCommands(
            IImmutableList<CreateTableCommand> createTables,
            IImmutableList<AlterMergeTableColumnDocStringsCommand> alterMergeTableColumns,
            IImmutableList<CreateMappingCommand> createMappings,
            IImmutableList<AlterUpdatePolicyCommand> updatePolicies)
        {
            var tableDocStringColumnMap = alterMergeTableColumns
                .GroupBy(c => c.TableName)
                .ToImmutableDictionary(g => g.Key, g => g.SelectMany(c => c.Columns));
            var mappingModelMap = createMappings
                .GroupBy(m => m.TableName)
                .ToImmutableDictionary(g => g.Key, g => g.Select(c => c.ToModel()));
            var updatePolicyMap = updatePolicies.ToImmutableDictionary(c => c.TableName);
            var tables = createTables
                .Select(ct => new TableModel(
                    ct.TableName,
                    FromCodeColumn(
                        ct.Columns,
                        tableDocStringColumnMap.ContainsKey(ct.TableName)
                        ? tableDocStringColumnMap[ct.TableName]
                        : null),
                    mappingModelMap.ContainsKey(ct.TableName)
                    ? mappingModelMap[ct.TableName]
                    : ImmutableArray<MappingModel>.Empty,
                    updatePolicyMap.ContainsKey(ct.TableName)
                    ? updatePolicyMap[ct.TableName]
                    : null,
                    ct.Folder,
                    ct.DocString))
                .ToImmutableArray();

            return tables;
        }

        internal static TableModel FromTableSchema(TableSchema schema)
        {
            var columns = schema
                .OrderedColumns
                .Select(c => FromColumnSchema(c));
            var mappings = schema
                .Mappings
                .Select(m => new MappingModel(
                    new QuotedText(m.Name),
                    m.Kind,
                    new QuotedText(m.MappingAsJson)));

            return new TableModel(
                new EntityName(schema.Name),
                columns,
                mappings,
                null,
                QuotedText.FromText(schema.Folder),
                QuotedText.FromText(schema.DocString));
        }

        internal static IEnumerable<CommandBase> ComputeDelta(
            IImmutableList<TableModel> currentModels,
            IImmutableList<TableModel> targetModels)
        {
            var currentTables = currentModels.ToImmutableDictionary(m => m.TableName);
            var currentTableNames = currentTables.Keys.ToImmutableSortedSet();
            var targetTables = targetModels.ToImmutableDictionary(m => m.TableName);
            var targetTableNames = targetTables.Keys.ToImmutableSortedSet();
            var dropTableNames = currentTableNames.Except(targetTableNames);
            var createTableNames = targetTableNames.Except(currentTableNames);
            var dropTables = dropTableNames
                .Select(name => new DropTableCommand(name) as CommandBase);
            var createTables = createTableNames
                .Select(name => targetTables[name].ToCommands())
                .SelectMany(c => c);
            var modifiedTables = targetTableNames
                .Intersect(currentTableNames)
                .SelectMany(name => currentTables[name].ComputeDelta(targetTables[name]));

            return dropTables
                .Concat(createTables)
                .Concat(modifiedTables);
        }

        private static IEnumerable<ColumnModel> FromCodeColumn(
            IImmutableList<TableColumn> codeColumns,
            IEnumerable<AlterMergeTableColumnDocStringsCommand.ColumnDocString>? columnDocStrings)
        {
            var columnDocMap = columnDocStrings != null
                ? columnDocStrings.ToImmutableDictionary(c => c.ColumnName, c => c.DocString)
                : ImmutableDictionary<EntityName, QuotedText>.Empty;
            var columns = codeColumns
                .Select(c => new ColumnModel(
                    c.ColumnName,
                    c.PrimitiveType,
                    columnDocMap.ContainsKey(c.ColumnName) ? columnDocMap[c.ColumnName] : null));

            return columns.ToImmutableArray();
        }

        private IEnumerable<CommandBase> ToCommands()
        {
            yield return new CreateTableCommand(
                TableName,
                Columns.Select(c => new TableColumn(c.ColumnName, c.PrimitiveType)),
                Folder,
                DocString);

            var columnsWithDocString = Columns
                .Where(c => c.DocString != null && !c.DocString.Equals(QuotedText.Empty))
                .Select(c => new AlterMergeTableColumnDocStringsCommand.ColumnDocString(
                    c.ColumnName,
                    c.DocString!));

            if (columnsWithDocString.Any())
            {
                yield return new AlterMergeTableColumnDocStringsCommand(
                    TableName,
                    columnsWithDocString);
            }

            foreach (var mapping in Mappings)
            {
                yield return mapping.ToCreateMappingCommand(TableName);
            }

            if (UpdatePolicy != null)
            {
                yield return UpdatePolicy;
            }
        }

        private IEnumerable<CommandBase> ComputeDelta(TableModel targetModel)
        {
            var includeFolder = !object.Equals(targetModel.Folder, Folder);
            var includeDocString = !object.Equals(targetModel.DocString, DocString);
            var currentColumns = Columns.ToImmutableDictionary(c => c.ColumnName);
            var targetColumns = targetModel.Columns.ToImmutableDictionary(c => c.ColumnName);
            var currentColumnNames = Columns.Select(c => c.ColumnName).ToImmutableHashSet();
            var targetColumnNames =
                targetModel.Columns.Select(c => c.ColumnName).ToImmutableHashSet();
            var dropColumnNames = currentColumnNames.Except(targetColumnNames);
            var createColumnNames = targetColumnNames.Except(currentColumnNames);
            var keepingColumnNames = currentColumnNames.Intersect(targetColumnNames);
            var updateTypeColumnNames = keepingColumnNames
                .Where(n => currentColumns[n].PrimitiveType != targetColumns[n].PrimitiveType);
            var updateDocStringColumnNames = keepingColumnNames
                .Where(n => !object.Equals(
                    currentColumns[n].DocString,
                    targetColumns[n].DocString));
            var mappingCommands = MappingModel.ComputeDelta(
                TableName,
                Mappings,
                targetModel.Mappings);
            var updatePolicyCommands =
                AlterUpdatePolicyCommand.ComputeDelta(UpdatePolicy, targetModel.UpdatePolicy);

            if (dropColumnNames.Any())
            {
                yield return new DropTableColumnsCommand(
                    TableName,
                    dropColumnNames.ToImmutableArray());
            }
            if (createColumnNames.Any() || includeFolder || includeDocString)
            {
                yield return new CreateTableCommand(
                    TableName,
                    targetModel.Columns.Select(
                        c => new TableColumn(c.ColumnName, c.PrimitiveType)),
                    includeFolder ? (targetModel.Folder ?? QuotedText.Empty) : null,
                    includeDocString
                    ? (targetModel.DocString ?? QuotedText.Empty)
                    : null);
            }
            if (updateDocStringColumnNames.Any())
            {
                yield return new AlterMergeTableColumnDocStringsCommand(
                    TableName,
                    updateDocStringColumnNames.Select(
                        n => new AlterMergeTableColumnDocStringsCommand.ColumnDocString(
                            n,
                            targetColumns[n].DocString != null
                            ? targetColumns[n].DocString!
                            : QuotedText.Empty)));
            }
            foreach (var columnName in updateTypeColumnNames)
            {
                yield return new AlterColumnTypeCommand(
                    TableName,
                    columnName,
                    targetColumns[columnName].PrimitiveType);
            }
            foreach (var command in mappingCommands)
            {
                yield return command;
            }
            foreach (var command in updatePolicyCommands)
            {
                yield return command;
            }
        }

        private static ColumnModel FromColumnSchema(ColumnSchema column)
        {
            return new ColumnModel(
                new EntityName(column.Name),
                column.CslType,
                QuotedText.FromText(column.DocString));
        }
    }
}