using System;
using System.Globalization;
using System.Text;

namespace Tritone.Editor.Tables
{
    /// <summary>
    /// Implements one allocation-insensitive scalar field conversion used during Editor builds.
    /// </summary>
    internal sealed class PrimitiveTableFieldType : ITableFieldType
    {
        /// <summary>Stores the conversion strategy.</summary>
        private readonly Func<string, string> mConverter;

        /// <inheritdoc />
        public string Name { get; }

        /// <inheritdoc />
        public string CSharpTypeName { get; }

        /// <summary>Creates one primitive type handler.</summary>
        /// <param name="name">The schema type name.</param>
        /// <param name="cSharpTypeName">The generated C# type name.</param>
        /// <param name="converter">The raw-to-JSON conversion strategy.</param>
        internal PrimitiveTableFieldType(string name, string cSharpTypeName, Func<string, string> converter)
        {
            Name           = name;
            CSharpTypeName = cSharpTypeName;
            mConverter     = converter;
        }

        /// <inheritdoc />
        public bool TryConvert(string rawValue, out string jsonValue)
        {
            try
            {
                jsonValue = mConverter(rawValue);
                return jsonValue != null;
            }
            catch (FormatException)
            {
                jsonValue = null;
                return false;
            }
            catch (OverflowException)
            {
                jsonValue = null;
                return false;
            }
        }

        /// <summary>Creates the built-in Boolean handler.</summary>
        /// <returns>The Boolean handler.</returns>
        internal static PrimitiveTableFieldType CreateBool()
        {
            return new PrimitiveTableFieldType("bool", "bool", ConvertBool);
        }

        /// <summary>Creates the built-in Int32 handler.</summary>
        /// <returns>The Int32 handler.</returns>
        internal static PrimitiveTableFieldType CreateInt()
        {
            return new PrimitiveTableFieldType("int", "int", ConvertInt);
        }

        /// <summary>Creates the built-in Int64 handler.</summary>
        /// <returns>The Int64 handler.</returns>
        internal static PrimitiveTableFieldType CreateLong()
        {
            return new PrimitiveTableFieldType("long", "long", ConvertLong);
        }

        /// <summary>Creates the built-in Single handler.</summary>
        /// <returns>The Single handler.</returns>
        internal static PrimitiveTableFieldType CreateFloat()
        {
            return new PrimitiveTableFieldType("float", "float", ConvertFloat);
        }

        /// <summary>Creates the built-in Double handler.</summary>
        /// <returns>The Double handler.</returns>
        internal static PrimitiveTableFieldType CreateDouble()
        {
            return new PrimitiveTableFieldType("double", "double", ConvertDouble);
        }

        /// <summary>Creates the built-in String handler.</summary>
        /// <returns>The String handler.</returns>
        internal static PrimitiveTableFieldType CreateString()
        {
            return new PrimitiveTableFieldType("string", "string", ConvertString);
        }

        /// <summary>Converts one Boolean value.</summary>
        /// <param name="value">The raw value.</param>
        /// <returns>The normalized JSON value.</returns>
        private static string ConvertBool(string value)
        {
            return bool.Parse(value) ? "true" : "false";
        }

        /// <summary>Converts one Int32 value.</summary>
        /// <param name="value">The raw value.</param>
        /// <returns>The normalized JSON value.</returns>
        private static string ConvertInt(string value)
        {
            return int.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>Converts one Int64 value.</summary>
        /// <param name="value">The raw value.</param>
        /// <returns>The normalized JSON value.</returns>
        private static string ConvertLong(string value)
        {
            return long.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>Converts one Single value.</summary>
        /// <param name="value">The raw value.</param>
        /// <returns>The normalized JSON value.</returns>
        private static string ConvertFloat(string value)
        {
            var result = float.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);
            return float.IsNaN(result) || float.IsInfinity(result) ? null : result.ToString("R", CultureInfo.InvariantCulture);
        }

        /// <summary>Converts one Double value.</summary>
        /// <param name="value">The raw value.</param>
        /// <returns>The normalized JSON value.</returns>
        private static string ConvertDouble(string value)
        {
            var result = double.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);
            return double.IsNaN(result) || double.IsInfinity(result) ? null : result.ToString("R", CultureInfo.InvariantCulture);
        }

        /// <summary>Escapes one String value as a JSON token.</summary>
        /// <param name="value">The raw value.</param>
        /// <returns>The escaped JSON value.</returns>
        private static string ConvertString(string value)
        {
            var builder = new StringBuilder(value.Length + 2);
            builder.Append('"');
            foreach (var character in value)
            {
                switch (character)
                {
                    case '"': builder.Append("\\\""); break;
                    case '\\': builder.Append("\\\\"); break;
                    case '\n': builder.Append("\\n"); break;
                    case '\r': builder.Append("\\r"); break;
                    case '\t': builder.Append("\\t"); break;
                    default:
                        if (character < 32)
                        {
                            builder.Append("\\u");
                            builder.Append(((int)character).ToString("x4", CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            builder.Append(character);
                        }
                        break;
                }
            }
            builder.Append('"');
            return builder.ToString();
        }
    }
}
