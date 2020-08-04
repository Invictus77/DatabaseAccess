using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using DatabaseAccess.Attributes;

namespace DatabaseAccess
{
    /// <summary>
    /// Generic data access class.
    /// </summary>
    /// <remarks>Support databases: MSACCESS (ODBC), SQL Server</remarks>
    public class DatabaseAccess : FieldEvaluator
    {
        #region Variable declaration
        private Provider _provider;
        private string _connectionString;
        private DbConnection _connection;
        #endregion

        #region Constructor
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="provider">The <see cref="Provider"/> enum to define which type of database will be connected.</param>
        /// <param name="connectionString">A string representing the connection string to access the database.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="connectionString"/> is set to null or empty string.</exception>
        public DatabaseAccess(Provider provider, string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString)) throw new ArgumentNullException(nameof(connectionString));
            else _connectionString = connectionString;

            _SetProvider(provider);
        }
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="provider">The <see cref="Provider"/> enum to define which type of database will be connected.</param>
        /// <param name="databaseFilepath"></param<param name="connectionString">A string representing the connection string to access the database.</param>
        /// <param name="exclusive">True if the database should be opened in an exclusive way (restricted access); otherwise false.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="databaseFilepath"/> parameter is set to null or empty string.</exception>
        /// <exception cref="System.IO.FileNotFoundException">Thrown if the given database (<paramref name="databaseFilepath"/>) does not exist.</exception>
        /// <exception cref="InvalidOperationException">Thrown if given provider matches <see cref="Provider.SqlClient"/>. This is not supported.</exception>
        public DatabaseAccess(Provider provider, string databaseFilepath, bool exclusive)
        {
            if (_provider == Provider.SqlClient) throw new InvalidOperationException("SqlClient does not support databaseFilePath.");
            if (string.IsNullOrEmpty(databaseFilepath)) throw new ArgumentNullException(nameof(databaseFilepath));
            if (!System.IO.File.Exists(databaseFilepath)) throw new System.IO.FileNotFoundException("Database does not exist.", databaseFilepath);

            string useExclusive  = "";
            if (exclusive)
                useExclusive = "Exclusive=1;Uid=Admin;Pwd=;";

            if (_provider == Provider.MsAccess)
                _connectionString = string.Concat(@"Driver={Microsoft Access Driver (*.mdb, *.accdb)};", $"Dbq={databaseFilepath};", useExclusive);
            else if (_provider == Provider.SqLite)
                throw new NotSupportedException();

            _SetProvider(provider);
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="provider">The <see cref="Provider"/> enum to define which type of database will be connected.</param>
        /// <param name="connection">An object derived from <see cref="DbConnection"/> representing the connection to use.</param>
        public DatabaseAccess(Provider provider, DbConnection connection)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _provider = provider;
        }

        /// <summary>
        /// Sets the provider internal var.
        /// </summary>
        /// <param name="provider"></param>
        private void _SetProvider(Provider provider)
        {
            switch(provider)
            {
                case Provider.MsAccess: _provider = provider; break;
                case Provider.SqlClient: _provider = provider; break;
                case Provider.SqLite: throw new NotSupportedException();
            }
        }
        #endregion

        #region Public properties
        /// <summary>
        /// Returns the db Provider for this class.
        /// </summary>
        public Provider DbProvider
        {
            get => _provider;
        }
        #endregion

        #region Public delegates
        /// <summary>
        /// Function which is called to execute code when command and surrounding objects (connection, transaction, etc.) are prepared.
        /// </summary>
        /// <param name="command">An <see cref="IDbCommand"/> object representing the command to execute.</param>
        public delegate TType StatementExecuter<TType>(IDbCommand command);
        #endregion

        #region Public Sql Helper methods (to build correct sql syntax)
        /// <summary>
        /// Returns the proper sql notation for DateSerial method.
        /// </summary>
        /// <param name="year">The year of the date.</param>
        /// <param name="month">The month of the date.</param>
        /// <param name="day">The day of the date.</param>
        /// <returns>A string representing the proper sql notation to represent this as a date.</returns>
        public string DateSerial(int year, int month, int day)
        {
            switch (_provider)
            {
                case Provider.MsAccess: return $"DateSerial({year},{month},{day})";
                case Provider.SqlClient: return $"CONVERT(DATETIME, '{year}{month:00}{day:00})";
                default: throw new NotImplementedException();
            }
        }
        /// <summary>
        /// Returns proper sql notation for Now() method.
        /// </summary>
        /// <returns>A string representing the proper sql notation for Now() method.</returns>
        public string Now()
        {
            switch (_provider)
            {
                case Provider.MsAccess: return "Now()";
                case Provider.SqlClient: return "GETDATE()";
                default: throw new NotImplementedException();
            }
        }
        /// <summary>
        /// Returns the property IsNull function.
        /// </summary>
        /// <param name="fieldName">The fieldname to check.</param>
        /// <param name="isNullValue">The value if the field is null.</param>
        /// <returns>A string representing the proper null notation for sql.</returns>
        public string IsNull(string fieldName, string isNullValue)
        {
            switch (_provider)
            {
                case Provider.MsAccess: return $"IIF(IsNull({fieldName}), {isNullValue}, {fieldName})";
                case Provider.SqlClient: return $"IsNull({fieldName}, {isNullValue})";
                default: throw new NotImplementedException();
            }
        }
        #endregion  

        #region Public methods
        /// <summary>
        /// Starts a connection and returns the transaction object.
        /// </summary>
        /// <param name="connection">The connection to the database.</param>
        /// <returns>The transaction object.</returns>
        public IDbTransaction BeginTransaction(IDbConnection connection)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));

            return connection.BeginTransaction(IsolationLevel.ReadCommitted);
        }

        /// <summary>
        /// Starts a connection and returns the transaction object.
        /// </summary>
        /// <param name="connection">The connection to the database.</param>
        /// <param name="il">The isolation level for the transaction.</param>
        /// <returns>The transaction object.</returns>
        public IDbTransaction BeginTransaction(IDbConnection connection, IsolationLevel il)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));

            return connection.BeginTransaction(il);
        }
        /// <summary>
        /// Rolls back the given transaction.
        /// </summary>
        /// <param name="transaction">The transaction to roll back.</param>
        public void RollBackTransaction(IDbTransaction transaction)
        {
            transaction.Rollback();
            transaction.Dispose();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="transaction"></param>
        public void CommitTransaction(IDbTransaction transaction)
        {
            transaction.Commit();
            transaction.Dispose();
        }

        [Obsolete]
        public List<string> ExecuteReader(string sqlCommand, params object[] parameters)
        {
            return ExecuteReaderObject<string>(sqlCommand, new ExecuteMappingDelegate<string>(this.ExecuteString), parameters);
        }

        [Obsolete]
        public List<string> ExecuteReader(DbTransaction transaction, string sqlCommand, params object[] parameters)
        {
            return ExecuteReaderObject<string>(transaction, sqlCommand, new ExecuteMappingDelegate<string>(this.ExecuteString), parameters);
        }

        public delegate void ExecuteMappingDelegate<ReturnType>(IDataReader reader, List<ReturnType> list);

        public List<DataType> ExecuteReaderObject<DataType>(string sqlCommand, ExecuteMappingDelegate<DataType> function, params object[] parameters)
        {
            List<DataType> returnList = new List<DataType>();

            _prepareExecuteConnection<object>(sqlCommand, (command) =>
            {
                using (IDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        function(reader, returnList);
                    }
                }
                return null;
            }, parameters);

            return returnList;
        }

        public List<DataType> ExecuteReaderObject<DataType>(DbTransaction transaction, string sqlCommand, ExecuteMappingDelegate<DataType> function, params object[] parameters)
        {
            List<DataType> returnList = new List<DataType>();

            _prepareExecute<object>(sqlCommand, transaction, (command) =>
            {
                using (IDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        function(reader, returnList);
                    }
                }
                return null;
            }, parameters);

            return returnList;
        }

        public List<DataType> ExecuteReaderMapping<DataType>(string sqlCommand, params object[] parameters)
        {
            return ExecuteReaderObject<DataType>(sqlCommand, new ExecuteMappingDelegate<DataType>(this.ExecuteMapping), parameters);
        }

        public List<DataType> ExecuteReaderMapping<DataType>(DbTransaction transaction, string sqlCommand, params object[] parameters)
        {
            return ExecuteReaderObject<DataType>(transaction, sqlCommand, new ExecuteMappingDelegate<DataType>(this.ExecuteMapping), parameters);
        }

        /// <summary>
        /// Inserts the given objects into the given table.
        /// </summary>
        /// <typeparam name="DataType">The type of the object to be inserted in the table.</typeparam>
        /// <param name="records">A list of objects to insert into the database.</param>
        /// <param name="tableName">A string representing the name of the table where the records should be inserted.</param>
        public void ExecuteNonQueryInsert<DataType>(List<DataType> records, string tableName)
        {
            ExecuteNonQueryInsert(null, records, tableName);
        }

        /// <summary>
        /// Inserts the given objects into the given table.
        /// </summary>
        /// <typeparam name="DataType">The type of the object to be inserted in the table.</typeparam>
        /// <param name="transaction">A <see cref="DbTransaction"/> object representing the transaction for this query.</param>
        /// <param name="records">A list of objects to insert into the database.</param>
        /// <param name="tableName">A string representing the name of the table where the records should be inserted.</param>
        public void ExecuteNonQueryInsert<DataType>(DbTransaction transaction, List<DataType> records, string tableName)
        {
            foreach (DataType record in records)
                ExecuteNonQueryInsert(transaction, record, tableName);
        }
        /// <summary>
        /// Inserts the given object into the given table.
        /// </summary>
        /// <typeparam name="DataType">The type of the object to be inserted in the table.</typeparam>
        /// <param name="transaction">A <see cref="DbTransaction"/> object representing the transaction for this query.</param>
        /// <param name="record">The object to insert.</param>
        /// <param name="tableName">A string representing the name of the table where the records should be inserted.</param>
        /// <returns>An integer representing the generated auto value for this record (if applicable). If there is no auto value 0 is returned.</returns>
        public int ExecuteNonQueryInsert<DataType>(DbTransaction transaction, DataType record, string tableName)
        {
            List<string> fieldList = new List<string>();
            List<string> parameterList = new List<string>();
            List<object> valueList = new List<object>();

            _ProcessProperties<DataType>((fieldName, propInfo, defaultValue, propertyType) =>
            {
                fieldList.Add($"[{fieldName}]");
                parameterList.Add("?");
                valueList.Add(propInfo.GetValue(record));
            }, true);
            if (transaction == null)
            {
                using (DbConnection connection = GetConnection())
                {
                    connection.Open();
                    using (DbTransaction scopeTransaction = connection.BeginTransaction())
                    {
                        ExecuteNonQuery(scopeTransaction, $"INSERT INTO {tableName} ({string.Join(",", fieldList.ToArray())}) VALUES ({string.Join(",", parameterList.ToArray())})", valueList.ToArray());
                        int identity = ExecuteScalar(scopeTransaction, "SELECT @@IDENTITY", 0);
                        scopeTransaction.Commit();
                        return identity;
                    }
                }
            }
            else
            {
                ExecuteNonQuery(transaction, $"INSERT INTO {tableName} ({string.Join(",", fieldList.ToArray())}) VALUES ({string.Join(",", parameterList.ToArray())})", valueList.ToArray());
                return ExecuteScalar(transaction, "SELECT @@IDENTITY", 0);
            }
        }
        /// <summary>
        /// Inserts the given object into the given table.
        /// </summary>
        /// <typeparam name="DataType">The type of the object to be inserted in the table.</typeparam>
        /// <param name="record">The object to insert.</param>
        /// <param name="tableName">A string representing the name of the table where the records should be inserted.</param>
        /// <returns>An integer representing the generated auto value for this record (if applicable). If there is no auto value 0 is returned.</returns>
        public int ExecuteNonQueryInsert<DataType>(DataType record, string tableName)
        {
            return ExecuteNonQueryInsert(null, record, tableName);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="DataType"></typeparam>
        /// <param name="records"></param>
        /// <param name="updateCommand">A string representing the update command in the following notation: UPDATE [TableName] SET {0} WHERE [yourParameters].</param>
        /// <param name="parameters"></param>
        public void ExecuteNonQueryUpdate<DataType>(List<DataType> records, string updateCommand, params object[] parameters)
        {
            ExecuteNonQueryUpdate(null, records, updateCommand, parameters);
        }
        /// <summary>
        /// UPDATE Table SET {fieldList} WHERE y = 3
        /// </summary>
        /// <typeparam name="DataType"></typeparam>
        /// <param name="records"></param>
        /// <param name="updateCommand">A string representing the update command in the following notation: UPDATE [TableName] SET {0} WHERE [yourParameters].</param>
        /// <param name="parameters"></param>
        public void ExecuteNonQueryUpdate<DataType>(DbTransaction transaction, List<DataType> records, string updateCommand, params object[] parameters)
        {
            foreach (DataType record in records)
                ExecuteNonQueryUpdate(transaction, record, updateCommand, parameters);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="DataType"></typeparam>
        /// <param name="record"></param>
        /// <param name="updateCommand">A string representing the update command in the following notation: UPDATE [TableName] SET {0} WHERE [yourParameters].</param>
        /// <param name="parameters"></param>
        public void ExecuteNonQueryUpdate<DataType>(DataType record, string updateCommand, params object[] parameters)
        {
            ExecuteNonQueryUpdate(null, record, updateCommand, parameters);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="DataType"></typeparam>
        /// <param name="transaction"></param>
        /// <param name="record"></param>
        /// <param name="updateCommand">A string representing the update command in the following notation: UPDATE [TableName] SET {0} WHERE [yourParameters].</param>
        /// <param name="parameters"></param>
        public void ExecuteNonQueryUpdate<DataType>(DbTransaction transaction, DataType record, string updateCommand, params object[] parameters)
        {
            Dictionary<string, string> fieldList = new Dictionary<string, string>();
            List<object> valueList = new List<object>();

            _ProcessProperties<DataType>((fieldName, propInfo, defaultValue, propertyType) =>
            {
                fieldList.Add(fieldName, "?");
                valueList.Add(propInfo.GetValue(record));
            }, true);
            valueList.AddRange(parameters); // add where parameters
            if (transaction == null)
                ExecuteNonQuery(string.Format(updateCommand, string.Join(", ", fieldList.Select(x => $"[{x.Key}] = {x.Value}").ToArray())), valueList.ToArray());
            else
                ExecuteNonQuery(transaction, string.Format(updateCommand, string.Join(", ", fieldList.Select(x => $"[{x.Key}] = {x.Value}").ToArray())), valueList.ToArray());
        }
        /// <summary>
        /// Checks wether a record exists in the database or not (with the given sql statement).
        /// </summary>
        /// <param name="sqlCommand">A string representing an sql statement to check if a record exists in the database. At least one record must be returned by the query to set the return value to true.</param>
        /// <param name="parameters">An array of parameters for the sql statement.</param>
        /// <returns>True if the record exists in the table; otherwise false.</returns>
        public bool RecordsExists(string sqlCommand, params object[] parameters)
        {
            return _prepareExecuteConnection<bool>(sqlCommand, (command) =>
            {
                using (IDataReader reader = command.ExecuteReader())
                {
                    return reader.Read();
                }
            }, parameters);
        }
        /// <summary>
        /// Checks wether a record exists in the database or not (with the given sql statement).
        /// </summary>
        /// <param name="transaction">A <see cref="DbTransaction"/> object representing the transaction for the query.</param>
        /// <param name="sqlCommand">A string representing an sql statement to check if a record exists in the database. At least one record must be returned by the query to set the return value to true.</param>
        /// <param name="parameters">An array of parameters for the sql statement.</param>
        /// <returns>True if the record exists in the table; otherwise false.</returns>
        public bool RecordsExists(DbTransaction transaction, string sqlCommand, params object[] parameters)
        {
            return _prepareExecute<bool>(sqlCommand, transaction, (command) =>
            {
                using (IDataReader reader = command.ExecuteReader())
                {
                    return reader.Read();
                }
            }, parameters);
        }

        public void ExecuteNonQuery(DbTransaction transaction, string sqlCommand, params object[] parameters)
        {
            _prepareExecute<object>(sqlCommand, transaction, (command) =>
            {
                command.ExecuteNonQuery();
                return null;
            }, parameters);
        }

        public void ExecuteNonQuery(string sqlCommand, params object[] parameters)
        {
            _prepareExecuteConnection<object>(sqlCommand, (command) =>
            {
                command.ExecuteNonQuery();
                return null;
            }, parameters);
        }

        /// <summary>
        /// Executes the given query and returns the first column first row value.
        /// </summary>
        /// <param name="sqlCommand">A string representing the query to execute.</param>
        /// <param name="parameters">A list of objects representing the query parameters.</param>
        /// <returns>The requested value by the query.</returns>
        public object ExecuteScalar(string sqlCommand, params object[] parameters)
        {
            return _prepareExecuteConnection(sqlCommand, (command) =>
            {
                return command.ExecuteScalar();
            }, parameters);
        }

        /// <summary>
        /// Executes the given query and returns the first column first row value.
        /// </summary>
        /// <param name="transaction">A <see cref="DbTransaction"/> object representing the transaction for the query.</param>
        /// <param name="sqlCommand">A string representing the query to execute.</param>
        /// <param name="parameters">A list of objects representing the query parameters.</param>
        /// <returns>The requested value by the query.</returns>
        public object ExecuteScalar(DbTransaction transaction, string sqlCommand, params object[] parameters)
        {
            return _prepareExecute(sqlCommand, transaction, (command) =>
            {
                return command.ExecuteScalar();
            }, parameters);
        }

        /// <summary>
        /// Executes the given query and returns the first column first row value.
        /// </summary>
        /// <typeparam name="DataType">The type of the value to be returned.</typeparam>
        /// <param name="sqlCommand">A string representing the query to execute.</param>
        /// <param name="valueIfNull">An object of type <typeparamref name="DataType"/> representing the return value if null or no record is returned.</param>
        /// <param name="parameters">A list of objects representing the query parameters.</param>
        /// <returns>The requested value by the query or the value in valueIfNull if null or no record was returned.</returns>

        public DataType ExecuteScalar<DataType>(string sqlCommand, DataType valueIfNull, params object[] parameters)
        {
            return _prepareExecuteConnection(sqlCommand, (command) =>
            {
                return IsNull(command.ExecuteScalar(), valueIfNull);
            }, parameters);
        }

        /// <summary>
        /// Executes the given query and returns the first column first row value.
        /// </summary>
        /// <typeparam name="DataType">The type of the value to be returned.</typeparam>
        /// <param name="transaction">A <see cref="DbTransaction"/> object representing the transaction for the query.</param>
        /// <param name="sqlCommand">A string representing the query to execute.</param>
        /// <param name="valueIfNull">An object of type <typeparamref name="DataType"/> representing the return value if null or no record is returned.</param>
        /// <param name="parameters">A list of objects representing the query parameters.</param>
        /// <returns>The requested value by the query or the value in valueIfNull if null or no record was returned.</returns>
        public DataType ExecuteScalar<DataType>(DbTransaction transaction, string sqlCommand, DataType valueIfNull, params object[] parameters)
        {
            return _prepareExecute(sqlCommand, transaction, (command) =>
            {
                return IsNull(command.ExecuteScalar(), valueIfNull);
            }, parameters);
        }

        /// <summary>
        /// Returns all fields which will be evaluated by the object mapper.
        /// </summary>
        /// <typeparam name="DataType">The type of the object to be analyzed.</typeparam>
        /// <param name="noReadOnly">True if readonly properties should be ommitted; otherwise false. Default is false.</param>
        /// <returns>A <see cref="List{T}"/> containing all fields which will be evaluated by the object mapper separated with comma.</returns>
        public List<string> GetFieldList<DataType>(bool noReadOnly = false)
        {
            List<string> fieldList = new List<string>();
            _ProcessProperties<DataType>((fieldName, propInfo, defaultValue, propertyType) =>
            {
                fieldList.Add($"[{fieldName}]");
            }, noReadOnly);

            return fieldList;
        }
        /// <summary>
        /// Returns all fields which will be evaluated by the object mapper separated with comma.
        /// </summary>
        /// <typeparam name="DataType">The type of the object to be analyzed.</typeparam>
        /// <param name="noReadOnly">True if readonly properties should be ommitted; otherwise false. Default is false.</param>
        /// <returns>A string representing all fields which will be evaluated by the object mapper separated with comma.</returns>
        public string GetFieldListString<DataType>(bool noReadOnly = false)
        {
            return string.Join(", ", GetFieldList<DataType>(noReadOnly));
        }
        /// <summary>
        /// Returns the current connection. If no current connection exists a new one will be created.
        /// </summary>
        /// <returns>An object derived from <see cref="DbConnection"/> representing the current or a new connection.</returns>
        public DbConnection GetConnection()
        {
            if (_connection != null) return _connection;

            if (_provider == Provider.MsAccess)
                return new System.Data.Odbc.OdbcConnection(_connectionString);
            else if (_provider == Provider.SqlClient)
                return new System.Data.SqlClient.SqlConnection(_connectionString);
            throw new NotImplementedException();
        }
        /// <summary>
        /// Returns an emptry string if object is null or the string representation of the object. 
        /// </summary>
        /// <param name="value">An object representing the value to convert to string.</param>
        /// <returns>A string representing the string representation of <paramref name="value"/></returns>
        public static string ToString(object value)
        {
            if (value == null) return "";
            return value.ToString();
        }
        /// <summary>
        /// Creates a parameter array out of an array of object parameters.
        /// </summary>
        /// <param name="command">A <see cref="DBCommand"/> object representing the DB Command which should be executed against the database.</param>
        /// <param name="commandText">A string representing the command text which should be executed against the database.</param>
        /// <param name="parameters">An object array representing the parameters for the query.</param>
        /// <returns>An array of <see cref="DbParameter"/> objects to add to the <see cref="DbCommand"/> object.</returns>
        /// <remarks>It is important, that the sort order of the parameters collections exactly matched the sort order of the parameters in the sql string.</remarks>
        public DbParameter[] CreateParameterArray(DbCommand command, string commandText, params object[] parameters)
        {
            if (parameters == null || parameters.Length == 0) return null;
            List<DbParameter> parameterList = new List<DbParameter>();
            Regex parameterResolver = new Regex(string.Concat(@"\@([^=<>\s\',\)]+)", _provider == Provider.MsAccess ? @"|\?" : "")); // for access allow also ? parameters
            List<string> parameterNames = new List<string>();
            foreach (Match match in parameterResolver.Matches(commandText))
                parameterNames.Add(match.Value);
            if (parameterNames.Count < parameters.Length) throw new InvalidOperationException("Not enough parameters in sql string");
            for (int i = 0; i < parameters.Length; i++)
            {
                DbParameter parameter = command.CreateParameter();
                parameter.ParameterName = parameterNames[i];
                ProcessPlatformSpecificParameter(parameter, parameters[i]);
                parameterList.Add(parameter);
            }
            return parameterList.ToArray();
        }

        /// <summary>
        /// This method can be used to reevaluate parameters setting --> access cannot work with Decimal in OleDBConnection. Can only be solved in .net Framework.
        /// </summary>
        /// <param name="parameter">A DBParameter object representing the parameter to set.</param>
        /// <param name="value">The value to be set to the parameter.</param>
        public virtual void ProcessPlatformSpecificParameter(DbParameter parameter, object value)
        {
            if (_provider == Provider.SqlClient && value == null)
            {
                parameter.Value = DBNull.Value;
                return;
            }
            parameter.Value = value;
        }
        #endregion

        #region Private methods
        /// <summary>
        /// 
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="list"></param>
        private void ExecuteString(IDataReader reader, List<string> list)
        {
            list.Add(reader[0].ToString());
        }
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="DataType"></typeparam>
        /// <param name="reader"></param>
        /// <param name="list"></param>
        private void ExecuteMapping<DataType>(IDataReader reader, List<DataType> list)
        {
            Type t = typeof(DataType);
            DataType mapObject = Activator.CreateInstance<DataType>();

            _ProcessProperties<DataType>((fieldName, propInfo, defaultValue, propertyType) =>
            {
                if (FieldExists(reader, fieldName))
                    propInfo.SetValue(mapObject, GetValue(reader.GetValue(reader.GetOrdinal(fieldName)), defaultValue, propertyType));
            });

            list.Add(mapObject);
        }
        /// <summary>
        /// Processes all properties of type <typeparamref name="DataType"/> and call the delegate <paramref name="func"/>.
        /// </summary>
        /// <typeparam name="DataType">The type of the object which properties should be processed.</typeparam>
        /// <param name="func">A delegate to the to be called function.</param>
        /// <param name="noReadOnly">True if readonly properties should be ommitted; otherwise false.</param>
        private void _ProcessProperties<DataType>(ProcessProperty func, bool noReadOnly = false)
        {
            Type t = typeof(DataType);

            foreach (PropertyInfo propInfo in t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                DefaultValueAttribute defaultAttribute = propInfo.GetCustomAttribute<DefaultValueAttribute>();
                DatabaseNameAttribute nameAttribute = propInfo.GetCustomAttribute<DatabaseNameAttribute>();
                ReadOnlyAttribute readonlyAttribute = propInfo.GetCustomAttribute<ReadOnlyAttribute>();
                if (defaultAttribute != null)
                {
                    string propName = propInfo.Name;
                    if (nameAttribute != null && !string.IsNullOrEmpty(nameAttribute.Name)) propName = nameAttribute.Name;
                    Type propertyType = propInfo.PropertyType;

                    if (propertyType.GetTypeInfo().IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                        propertyType = propertyType.GenericTypeArguments[0];

                    if (!(noReadOnly && readonlyAttribute != null && readonlyAttribute.IsReadOnly))
                        func(propName, propInfo, defaultAttribute.Value, propertyType);
                }
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="propInfo"></param>
        /// <param name="defaultValue"></param>
        /// <param name="propertyType"></param>
        private delegate void ProcessProperty(string fieldName, PropertyInfo propInfo, object defaultValue, Type propertyType);

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

        private TType _prepareExecuteConnection<TType>(string commandText, StatementExecuter<TType> func, params object[] parameter)
        {
            if (string.IsNullOrEmpty(commandText)) throw new ArgumentNullException(nameof(commandText));
            if (func == null) throw new ArgumentNullException(nameof(func));

            if (_connection != null)
            {
                bool closeConnection = false;
                if (closeConnection = _connection.State != ConnectionState.Open) _connection.Open();
                using (DbTransaction transaction = _connection.BeginTransaction())
                {
                    TType returnValue = default(TType);
                    try
                    {
                        returnValue = _prepareExecute<TType>(commandText, transaction, func, parameter);
                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                    finally
                    {
                        if (closeConnection) _connection.Close();
                    }
                    return returnValue;
                }
            }
            else
            {
                using (DbConnection connection = GetConnection())
                {
                    connection.Open();
                    using (DbTransaction transaction = connection.BeginTransaction())
                    {
                        TType returnValue = default(TType);
                        try
                        {
                            returnValue = _prepareExecute<TType>(commandText, transaction, func, parameter);
                            transaction.Commit();
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                        return returnValue;
                    }
                }
            }
        }

        /// <summary>
        /// Prepares connection and command for execution of query. 
        /// </summary>
        /// <param name="commandText">A string representing the query.</param>
        /// <param name="func">A <see cref="StatementExecuter"/> function pointer representing the function to call with the command object.</param>
        /// <param name="parameters">An object array representing the parameters for the query.</param>
        /// <remarks>
        /// Does not execute the query. The execution has to be done in StatementExecuter function. 
        ///
        /// Does not rollback transaction and no error handling.
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown if commandText is an emptry string or null and if func is null.</exception>
        private TType _prepareExecute<TType>(string commandText, DbTransaction transaction, StatementExecuter<TType> func, params object[] parameters)
        {
            if (string.IsNullOrEmpty(commandText)) throw new ArgumentNullException(nameof(commandText));
            if (func == null) throw new ArgumentNullException(nameof(func));

            using (DbCommand command = transaction.Connection.CreateCommand())
            {
                
                if (transaction != null) command.Transaction = transaction;
                if (parameters != null && parameters.Length > 0) command.Parameters.AddRange(CreateParameterArray(command, commandText, parameters));

                if (_provider == Provider.MsAccess) // Access over odbc connection does not accept named parameters --> replace all named parameters with ?
                    foreach (DbParameter parameter in command.Parameters)
                        commandText = commandText.Replace(parameter.ParameterName, "?");

                command.CommandText = commandText;

                _LogCommandAndParameters(command, parameters);

                return func(command);
            }
        }  

        /// <summary>
        /// Logs all sql queries to the debug window. 
        /// </summary>
        /// <param name="command">A <see cref="DBCommand"/> object representing the command to execute.</param>
        /// <param name="parameters"></param>
        private void _LogCommandAndParameters(IDbCommand command, params object[] parameters)
        {
            List<DbParameter> parameterList = new List<DbParameter>();
            foreach (DbParameter parameter in command.Parameters)
                parameterList.Add(parameter);

            if (parameterList.Count == 0)
                Debug.WriteLine(command.CommandText);
            else
                Debug.WriteLine("{0}, parameters: {1}", command.CommandText, string.Join(", ", parameterList.Select(x => $"[{x.ParameterName}]: {ToString(x.Value)}").ToList()));
        }
        #endregion

    }
}
