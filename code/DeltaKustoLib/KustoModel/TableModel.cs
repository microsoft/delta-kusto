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
        public string TableName { get; }

        public IImmutableList<ColumnModel> Columns { get; }

        public string? Folder { get; }

        public string? DocString { get; }

        private TableModel(
            string tableName,
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
            IImmutableList<CreateTableCommand> createTables)
        {
            var tables = createTables
                .Select(ct => new TableModel(
                    ct.TableName,
                    FromCodeColumn(ct.Columns),
                    ct.Folder,
                    ct.DocString))
                .ToImmutableArray();

            return tables;
        }

        private static IEnumerable<ColumnModel> FromCodeColumn(
            IImmutableList<TableColumn> codeColumns)
        {
            var columns = codeColumns
                .Select(c => new ColumnModel(
                    c.ColumnName,
                    c.PrimitiveType,
                    null));

            return columns.ToImmutableArray();
        }
    }
}