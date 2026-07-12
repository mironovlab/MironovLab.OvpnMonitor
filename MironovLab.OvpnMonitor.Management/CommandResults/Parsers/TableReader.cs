using System;
using System.Collections.Generic;
using MironovLab.OpenVPN.Management.Core;
using MironovLab.OpenVPN.Management.Exceptions;

namespace MironovLab.OpenVPN.Management.CommandResults.Parsers
{
    internal class TableReader
    {
        private readonly string[] _columns;
        private readonly string[] _buffer;
        private readonly Dictionary<string, string> _rowData;
        public string TableName { get; }
        public IReadOnlyList<string> Columns { get; }
        public string this[int columnIndex] => _rowData[_columns[columnIndex]];
        public string this[string columnName] => _rowData[columnName];

        public TableReader(string headerLine)
        {
            var headerData = ParseUtils.SplitDataParts(headerLine);
            if (headerData[0] != Constants.TableHeader)
                throw new TableReaderException(Resources.TableReader_NotAHeader);

            TableName = headerData[1];
            var columnCount = headerData.Length - 2;
            _columns = new string[columnCount];
            _buffer = new string[columnCount + 1];
            Array.Copy(headerData, 2, _columns, 0, columnCount);
            Columns = _columns;
            _rowData = new Dictionary<string, string>(columnCount, StringComparer.OrdinalIgnoreCase);
        }

        public void ReadRow(string rowLine)
        {
            var rowColumnCount = ParseUtils.SequentialSplit(rowLine, Constants.MessageParamSplitter, _buffer) - 1;

            if (_buffer[0] != TableName)
                throw new TableReaderException(string.Format(Resources.TableReader_NotARow, TableName));

            if (rowColumnCount != _columns.Length)
                throw new TableReaderException(string.Format(Resources.TableReader_RowHasInvalidColumnCount, rowColumnCount, _columns.Length));

            for (var i = 0; i < rowColumnCount; i++)
            {
                _rowData[_columns[i]] = _buffer[i + 1];
            }
        }
    }
}
