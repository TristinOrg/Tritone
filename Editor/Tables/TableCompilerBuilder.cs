using System;
using System.Collections.Generic;

namespace Tritone.Editor.Tables
{
    /// <summary>
    /// Composes one immutable table compiler through explicit ordered dependencies.
    /// </summary>
    public sealed class TableCompilerBuilder
    {
        /// <summary>Stores source readers in deterministic resolution order.</summary>
        private readonly List<ITableSourceReader> mSourceReaders = new();

        /// <summary>Stores field types by schema name.</summary>
        private readonly Dictionary<string, ITableFieldType> mFieldTypes = new(StringComparer.Ordinal);

        /// <summary>Stores validators in deterministic execution order.</summary>
        private readonly List<ITableValidator> mValidators = new();

        /// <summary>Stores the generated code strategy.</summary>
        private ITableCodeGenerator mCodeGenerator;

        /// <summary>Stores the generated data strategy.</summary>
        private ITableDataWriter mDataWriter;

        /// <summary>Adds one source reader.</summary>
        /// <param name="reader">The source reader.</param>
        /// <returns>This builder.</returns>
        public TableCompilerBuilder AddSourceReader(ITableSourceReader reader)
        {
            mSourceReaders.Add(reader ?? throw new ArgumentNullException(nameof(reader)));
            return this;
        }

        /// <summary>Adds one uniquely named field type.</summary>
        /// <param name="fieldType">The field type.</param>
        /// <returns>This builder.</returns>
        public TableCompilerBuilder AddFieldType(ITableFieldType fieldType)
        {
            if (fieldType == null)
            {
                throw new ArgumentNullException(nameof(fieldType));
            }
            if (string.IsNullOrWhiteSpace(fieldType.Name))
            {
                throw new ArgumentException("Table field type name is required.", nameof(fieldType));
            }
            if (!mFieldTypes.TryAdd(fieldType.Name, fieldType))
            {
                throw new InvalidOperationException($"Table field type '{fieldType.Name}' is already registered.");
            }
            return this;
        }

        /// <summary>Adds the six built-in scalar field types.</summary>
        /// <returns>This builder.</returns>
        public TableCompilerBuilder AddDefaultFieldTypes()
        {
            return AddFieldType(PrimitiveTableFieldType.CreateBool())
                .AddFieldType(PrimitiveTableFieldType.CreateInt())
                .AddFieldType(PrimitiveTableFieldType.CreateLong())
                .AddFieldType(PrimitiveTableFieldType.CreateFloat())
                .AddFieldType(PrimitiveTableFieldType.CreateDouble())
                .AddFieldType(PrimitiveTableFieldType.CreateString());
        }

        /// <summary>Adds one post-conversion validator.</summary>
        /// <param name="validator">The validator.</param>
        /// <returns>This builder.</returns>
        public TableCompilerBuilder AddValidator(ITableValidator validator)
        {
            mValidators.Add(validator ?? throw new ArgumentNullException(nameof(validator)));
            return this;
        }

        /// <summary>Sets the generated code strategy.</summary>
        /// <param name="generator">The code generator.</param>
        /// <returns>This builder.</returns>
        public TableCompilerBuilder UseCodeGenerator(ITableCodeGenerator generator)
        {
            mCodeGenerator = generator ?? throw new ArgumentNullException(nameof(generator));
            return this;
        }

        /// <summary>Sets the generated data strategy.</summary>
        /// <param name="writer">The data writer.</param>
        /// <returns>This builder.</returns>
        public TableCompilerBuilder UseDataWriter(ITableDataWriter writer)
        {
            mDataWriter = writer ?? throw new ArgumentNullException(nameof(writer));
            return this;
        }

        /// <summary>Builds one immutable compiler.</summary>
        /// <returns>The configured compiler.</returns>
        public TableCompiler Build()
        {
            if (mCodeGenerator == null)
            {
                throw new InvalidOperationException("A table code generator is required.");
            }
            if (mDataWriter == null)
            {
                throw new InvalidOperationException("A table data writer is required.");
            }
            return new TableCompiler(mSourceReaders.ToArray(), new Dictionary<string, ITableFieldType>(mFieldTypes, StringComparer.Ordinal), mValidators.ToArray(), mCodeGenerator, mDataWriter);
        }
    }
}
