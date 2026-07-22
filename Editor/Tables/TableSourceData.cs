namespace Tritone.Editor.Tables
{
    /// <summary>
    /// Stores raw source headers and rows before schema type conversion.
    /// </summary>
    public sealed class TableSourceData
    {
        /// <summary>Gets the source path.</summary>
        public string Source { get; }

        /// <summary>Gets source headers in column order.</summary>
        public string[] Headers { get; }

        /// <summary>Gets raw rows in source order.</summary>
        public TableSourceRow[] Rows { get; }

        /// <summary>Creates one immutable raw table source.</summary>
        /// <param name="source">The source path.</param>
        /// <param name="headers">The source headers.</param>
        /// <param name="rows">The raw rows.</param>
        public TableSourceData(string source, string[] headers, TableSourceRow[] rows)
        {
            Source  = source;
            Headers = headers;
            Rows    = rows;
        }
    }
}
