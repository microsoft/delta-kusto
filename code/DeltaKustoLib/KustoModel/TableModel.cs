using DeltaKustoLib.CommandModel;
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

        public string? Folder { get; }

        public string? DocString { get; }

        private TableModel(
            EntityName tableName,
            IEnumerable<ColumnModel> columns,
            string? folder,
            string? docString)
        {
            TableName = tableName;
            Columns = columns.ToImmutableArray();
            Folder = string.IsNullOrEmpty(folder) ? null : folder;
            DocString = string.IsNullOrEmpty(docString) ? null : docString;
        }

        internal static IImmutableList<TableModel> FromCommands(
            IImmutableList<CreateTableCommand> createTables,
            IImmutableList<AlterMergeTableColumnDocStringsCommand> alterMergeTableColumns)
        {
            var tableDocStringColumnMap = alterMergeTableColumns
                .GroupBy(c => c.TableName)
                .ToImmutableDictionary(g => g.Key, g => g.SelectMany(c => c.Columns));
            var tables = createTables
                .Select(ct => new TableModel(
                    ct.TableName,
                    FromCodeColumn(
                        ct.Columns,
                        tableDocStringColumnMap.ContainsKey(ct.TableName)
                        ? tableDocStringColumnMap[ct.TableName]
                        : null),
                    ct.Folder?.Text,
                    ct.DocString?.Text))
                .ToImmutableArray();

            return tables;
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
    }
}