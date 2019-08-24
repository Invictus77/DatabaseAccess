using System;
using System.Reflection;

namespace DatabaseAccess
{
    /// <summary>
    /// Fieldevaluator base class.
    /// </summary>
    public class FieldEvaluator
    {
        /// <summary>
        /// Returns the <paramref name="valueIfNull"/> value <paramref name="fieldvalue"/> is null and <paramref name="fieldvalue"/> if it is not null.
        /// </summary>
        /// <typeparam name="TType">The type of the returned value.</typeparam>
        /// <param name="fieldvalue">The fieldvalue to check if null.</param>
        /// <param name="valueIfNull">The value to be returned if <paramref name="fieldvalue"/> is null.</param>
        /// <returns>Returns the <paramref name="valueIfNull"/> value <paramref name="fieldvalue"/> is null and <paramref name="fieldvalue"/> if it is not null.</returns>
        public static TType IsNull<TType>(object fieldvalue, TType valueIfNull)
        {
            if (fieldvalue == null || fieldvalue is DBNull) return valueIfNull;
            return (TType)fieldvalue;
        }
        /// <summary>
        /// Returns the <paramref name="valueIfNull"/> value <paramref name="fieldvalue"/> is null and <paramref name="valueIfNotNull"/> if <paramref name="fieldvalue"/> is not null.
        /// </summary>
        /// <typeparam name="TType">The type of the returned value.</typeparam>
        /// <param name="fieldvalue">The fieldvalue to check if null.</param>
        /// <param name="valueIfNull">The value to be returned if <paramref name="fieldvalue"/> is null.</param>
        /// <param name="valueIfNotNull">The value to be returned if <paramref name="fieldvalue"/> is not null.</param>
        /// <returns>Returns the <paramref name="valueIfNull"/> value <paramref name="fieldvalue"/> is null and <paramref name="valueIfNotNull"/> if <paramref name="fieldvalue"/> is not null.</returns>
        public static TType IsNull<TType>(object fieldvalue, TType valueIfNull, TType valueIfNotNull)
        {
            if (fieldvalue == null || fieldvalue is DBNull) return valueIfNull;
            return valueIfNotNull;
        }
        /// <summary>
        /// Returns the appropiate type of the given value (if possible).
        /// </summary>
        /// <typeparam name="TType">The type of the value to be returned.</typeparam>
        /// <param name="value">The value to be converted.</param>
        /// <param name="defaultValue">The default value if <paramref name="value"/> is null or <see cref="DBNull"/>.</param>
        /// <param name="propertyType">The type of the <paramref name="value"/>.</param>
        /// <returns>Returns the appropiate type of the given value (if possible).</returns>
        public static TType GetValue<TType>(object value, TType defaultValue, Type propertyType)
        {
            if (value == null || value is DBNull) return defaultValue;
            if (typeof(TType).GetTypeInfo().IsEnum)
            {
                try
                {
                    return (TType)Enum.Parse(propertyType, value.ToString());
                }
                catch
                {
                    return defaultValue;
                }
            }

            if (propertyType == typeof(Guid))
            {
                Guid guid = new Guid(value.ToString());
                return (TType)Convert.ChangeType(guid, propertyType);
            }
            else if (propertyType.GetTypeInfo().IsEnum)
            {
                return (TType)Enum.Parse(propertyType, value.ToString());
            }
            else
                return (TType)Convert.ChangeType(value, propertyType);
        }
    }
}
