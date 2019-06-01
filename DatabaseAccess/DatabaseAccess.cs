using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Reflection;
using DatabaseAccess.Attributes;

namespace DatabaseAccess
{
    /// <summary>
    /// 
    /// </summary>
    public class DatabaseAccess : FieldEvaluator
    {
        private Provider _provider;
        private string _connectionString;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="provider"></param>
        /// <param name="connectionString"></param>
        public DatabaseAccess(Provider provider, string connectionString)
        {
            _provider = provider;
            if (string.IsNullOrEmpty(connectionString)) throw new ArgumentNullException(nameof(connectionString));
            else _connectionString = connectionString;
            //Driver={Microsoft Access Driver (*.mdb)};Dbq=C:\mydatabase.mdb;Uid=Admin;Pwd=
        }

        public List<string> ExecuteReader(string sqlCommand, params object[] parameters)
        {
            return ExecuteReaderObject<string>(sqlCommand, new ExecuteMappingDelegate<string>(this.ExecuteString), parameters);
        }

        public delegate void ExecuteMappingDelegate<ReturnType>(IDataReader reader, List<ReturnType> list);

        public List<DataType> ExecuteReaderObject<DataType>(string sqlCommand, ExecuteMappingDelegate<DataType> function, params object[] parameters)
        {
            List<DataType> returnList = new List<DataType>();

            using (IDbConnection connection = GetConnection())
            {
                connection.Open();
                using (IDbCommand command = connection.CreateCommand())
                {
                    PrepareCommand(command, sqlCommand, parameters);

                    using (IDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            function(reader, returnList);
                        }
                    }
                }
            }
            return returnList;
        }

        public List<DataType> ExecuteReaderMapping<DataType>(string sqlCommand, params object[] parameters)
        {
            return ExecuteReaderObject<DataType>(sqlCommand, new ExecuteMappingDelegate<DataType>(this.ExecuteMapping), parameters);
        }

        public void ExecuteNonQueryInsert<DataType>(List<DataType> records, string tableName)
        {
            foreach (DataType record in records)
            {
                List<string> fieldList = new List<string>();
                List<string> parameterList = new List<string>();
                List<object> valueList = new List<object>();

                ProcessProperties<DataType>((fieldName, propInfo, defaultValue, propertyType) =>
                {
                    fieldList.Add($"[{fieldName}]");
                    parameterList.Add("?");
                    valueList.Add(propInfo.GetValue(record));
                });

                ExecuteNonQuery($"INSERT INTO {tableName} ({string.Join(",", fieldList.ToArray())}) VALUES ({string.Join(",", parameterList.ToArray())})", valueList.ToArray());
            }
        }

        /// <summary>
        /// UPDATE Table SET {fieldList} WHERE y = 3
        /// </summary>
        /// <typeparam name="DataType"></typeparam>
        /// <param name="records"></param>
        /// <param name="updateCommand"></param>
        /// <param name="parameters"></param>
        public void ExecuteNonQueryUpdate<DataType>(List<DataType> records, string updateCommand, params object[] parameters)
        {
            foreach (DataType record in records)
            {
                Dictionary<string, string> fieldList = new Dictionary<string, string>();
                List<object> valueList = new List<object>();

                ProcessProperties<DataType>((fieldName, propInfo, defaultValue, propertyType) =>
                {
                    fieldList.Add(fieldName, "?");
                    valueList.Add(propInfo.GetValue(record));
                });
                valueList.AddRange(parameters); // add where parameters

                ExecuteNonQuery(string.Format(updateCommand, string.Join(", ", fieldList.Select(x => $"[{x.Key}] = {x.Value}").ToArray())), valueList.ToArray());
            }
        }

        public bool RecordsExists(string sqlCommand, params object[] parameters)
        {
            using (IDbConnection connection = GetConnection())
            {
                connection.Open();
                using (IDbCommand command = connection.CreateCommand())
                {
                    PrepareCommand(command, sqlCommand, parameters);

                    using (IDataReader reader = command.ExecuteReader())
                    {
                        return reader.Read();
                    }
                }
            }
        }

        public void ExecuteNonQuery(string sqlCommand, params object[] parameters)
        {
            using (IDbConnection connection = GetConnection())
            {
                connection.Open();
                using (IDbCommand command = connection.CreateCommand())
                {
                    PrepareCommand(command, sqlCommand, parameters);

                    command.ExecuteNonQuery();
                }
            }
        }

        public object ExecuteScalar(string sqlCommand, params object[] parameters)
        {
            using (IDbConnection connection = GetConnection())
            {
                connection.Open();
                using (IDbCommand command = connection.CreateCommand())
                {
                    PrepareCommand(command, sqlCommand, parameters);

                    return command.ExecuteScalar();
                }
            }
        }

        private void ExecuteString(IDataReader reader, List<string> list)
        {
            list.Add(reader[0].ToString());
        }

        private void ExecuteMapping<DataType>(IDataReader reader, List<DataType> list)
        {
            Type t = typeof(DataType);
            DataType mapObject = Activator.CreateInstance<DataType>();

            ProcessProperties<DataType>((fieldName, propInfo, defaultValue, propertyType) =>
            {
                if (FieldExists(reader, fieldName))
                    propInfo.SetValue(mapObject, GetValue(reader.GetValue(reader.GetOrdinal(fieldName)), defaultValue, propertyType));
            });

            list.Add(mapObject);
        }

        public List<string> GetFieldList<DataType>()
        {
            List<string> fieldList = new List<string>();
            ProcessProperties<DataType>((fieldName, propInfo, defaultValue, propertyType) =>
            {
                fieldList.Add($"[{fieldName}]");
            });

            return fieldList;
        }

        public string GetFieldListString<DataType>()
        {
            return string.Join(", ", GetFieldList<DataType>());
        }

        private void ProcessProperties<DataType>(ProcessProperty func)
        {
            Type t = typeof(DataType);

            foreach (PropertyInfo propInfo in t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                DefaultValueAttribute defaultAttribute = propInfo.GetCustomAttribute<DefaultValueAttribute>();
                DatabaseNameAttribute nameAttribute = propInfo.GetCustomAttribute<DatabaseNameAttribute>();
                if (defaultAttribute != null)
                {
                    string propName = propInfo.Name;
                    if (nameAttribute != null && !string.IsNullOrEmpty(nameAttribute.Name)) propName = nameAttribute.Name;
                    Type propertyType = propInfo.PropertyType;

                    if (propertyType.GetTypeInfo().IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                        propertyType = propertyType.GenericTypeArguments[0];

                    func(propName, propInfo, defaultAttribute.Value, propertyType);
                }
            }
        }

        private delegate void ProcessProperty(string fieldName, PropertyInfo propInfo, object defaultValue, Type propertyType);

        private static TData GetValue<TData>(object value, TData defaultValue, Type propertyType)
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

        private bool FieldExists(IDataReader reader, string name)
        {
            int i;
            for (i = 0; i < reader.FieldCount; i++)
            {
                if (reader.GetName(i).ToUpper() == name.ToUpper())
                    return true;
            }
            return false;
        }

        private void PrepareCommand(IDbCommand command, string sqlCommand, params object[] parameters)
        {
            command.CommandText = sqlCommand;
            if (parameters != null && parameters.Length >= 0)
            {
                foreach (var parameter in parameters)
                    command.Parameters.Add(new System.Data.Odbc.OdbcParameter("1", parameter));
            }
        }

        private IDbConnection GetConnection()
        {
            if (_provider == Provider.MsAccess)
                return new System.Data.Odbc.OdbcConnection(_connectionString);
            throw new NotImplementedException();
        }

        public enum Provider
        {
            SqlClient = 0,
            SqLite = 1,
            MsAccess = 2
        }
    }
}
