using System;
using System.Text;

namespace Tritone.Editor.CodeGeneration
{
    /// <summary>
    /// Builds deterministic indented C# source without template dependencies.
    /// </summary>
    internal sealed class CodeTextBuilder
    {
        // Stores generated source text.
        private readonly StringBuilder mBuilder = new(2048);

        // Stores the active indentation depth.
        private int mIndent;

        /// <summary>
        /// Appends one indented source line.
        /// </summary>
        /// <param name="text">The source line without indentation.</param>
        internal void Line(string text = "")
        {
            for (int i = 0; i < mIndent; i++)
                mBuilder.Append("    ");
            mBuilder.Append(text);
            mBuilder.Append('\n');
        }

        /// <summary>
        /// Opens one brace-delimited source block.
        /// </summary>
        /// <param name="declaration">The declaration preceding the opening brace.</param>
        internal void Open(string declaration)
        {
            Line(declaration);
            Line("{");
            mIndent++;
        }

        /// <summary>
        /// Closes the active source block.
        /// </summary>
        /// <param name="suffix">Optional text appended after the closing brace.</param>
        internal void Close(string suffix = "")
        {
            if (mIndent == 0)
                throw new InvalidOperationException("No generated source block is open.");
            mIndent--;
            Line("}" + suffix);
        }

        /// <summary>
        /// Returns the deterministic generated source.
        /// </summary>
        /// <returns>The complete source text with LF line endings.</returns>
        public override string ToString()
        {
            if (mIndent != 0)
                throw new InvalidOperationException("Generated source contains an unclosed block.");
            return mBuilder.ToString();
        }
    }
}
