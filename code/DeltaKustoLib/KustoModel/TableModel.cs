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
    }
}