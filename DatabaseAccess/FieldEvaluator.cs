using System;
using System.Reflection;

namespace DatabaseAccess
{
    /// <summary>
    /// 
    /// </summary>
    public class FieldEvaluator
    {
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TType"></typeparam>
        /// <param name="fieldvalue"></param>
        /// <param name="valueIfNull"></param>
        /// <returns></returns>
        public static TType IsNull<TType>(object fieldvalue, TType valueIfNull)
        {
            if (fieldvalue == null || fieldvalue is DBNull) return valueIfNull;
            return (TType)fieldvalue;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TType"></typeparam>
        /// <param name="fieldvalue"></param>
        /// <param name="valueIfNull"></param>
        /// <param name="valueIfNotNull"></param>
        /// <returns></returns>
        public static TType IsNull<TType>(object fieldvalue, TType valueIfNull, TType valueIfNotNull)
        {
            if (fieldvalue == null || fieldvalue is DBNull) return valueIfNull;
            return valueIfNotNull;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TData"></typeparam>
        /// <param name="value"></param>
        /// <param name="defaultValue"></param>
        /// <param name="propertyType"></param>
        /// <returns></returns>
        public static TData GetValue<TData>(object value, TData defaultValue, Type propertyType)
        {
            if (value == null || value is DBNull) return defaultValue;
            if (typeof(TData).GetTypeInfo().IsEnum)
            {
                try
                {
                    return (TData)Enum.Parse(propertyType, value.ToString());
                }
                catch
                {
                    return defaultValue;
                }
            }

            if (propertyType == typeof(Guid))
            {
                Guid guid = new Guid(value.ToString());
                return (TData)Convert.ChangeType(guid, propertyType);
            }
            else if (propertyType.GetTypeInfo().IsEnum)
            {
                return (TData)Enum.Parse(propertyType, value.ToString());
            }
            else
                return (TData)Convert.ChangeType(value, propertyType);
        }
    }
}
