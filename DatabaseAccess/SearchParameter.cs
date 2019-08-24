using System;
using System.Collections.Generic;
using System.Text;

namespace DatabaseAccess
{
    /// <summary>
    /// Represents a search parameter for an sql query.
    /// </summary>
    public class SearchParameter
    {
        #region Constructor
        /// <summary>
        /// Constructor.
        /// </summary>
        public SearchParameter() { }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="fieldName">The name of the field in the database.</param>
        /// <param name="name">The name of the parameter (without @).</param>
        /// <param name="searchOperator">The operator for the parameter.</param>
        /// <param name="value">The value of the parameter.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="name"/> or <paramref name="fieldName"/> are set to null or emptry string.</exception>
        /// <exception cref="ArgumentException">Thrown if the parameter name starts with an '@'.</exception>
        public SearchParameter(string fieldName, string name, SearchParameterOperator searchOperator, object value)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            if (string.IsNullOrEmpty(fieldName)) throw new ArgumentNullException(nameof(fieldName));
            if (name.Substring(0, 1) == "@") throw new ArgumentException("Parameter must not have '@' at the beginning.", nameof(name));

            Name = name;
            FieldName = fieldName;
            SearchOperator = searchOperator;
            Value = value;
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// The operator for the parameter.
        /// </summary>
        public SearchParameterOperator SearchOperator { get; set; }

        /// <summary>
        /// The name of the field for the expression.
        /// </summary>
        public string FieldName { get; set; }

        /// <summary>
        /// The name of the parameter.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The value of the parameter.
        /// </summary>
        public object Value { get; set; }

        /// <summary>
        /// The sql clause of this parameter
        /// </summary>
        public string SqlClause
        {
            get
            {
                if (Value == null)
                {
                    switch (SearchOperator)
                    {
                        case SearchParameterOperator.equal: return $"{FieldName} is null"; 
                        case SearchParameterOperator.like: return $"{FieldName} is null"; 
                        case SearchParameterOperator.notEqual: return $"{FieldName} is not null"; 
                        default: throw new InvalidOperationException($"SearchParameter option {SearchOperator} not supported.");
                    }
                }
                else
                {
                    switch(SearchOperator)
                    {
                        case SearchParameterOperator.equal: return $"{FieldName} = @{Name}";
                        case SearchParameterOperator.like: return $"{FieldName} like @{Name}"; 
                        case SearchParameterOperator.notEqual: return $"{FieldName} <> @{Name}"; 
                        case SearchParameterOperator.greater: return $"{FieldName} > @{Name}";
                        case SearchParameterOperator.greaterOrEqual: return $"{FieldName} >= @{Name}";
                        case SearchParameterOperator.smaller: return $"{FieldName} < @{Name}";
                        case SearchParameterOperator.smallerOrEqual: return $"{FieldName} <= @{Name}";
                        default: throw new InvalidOperationException($"SearchParameter option {SearchOperator} not supported.");
                    }
                }
            }
        }
        #endregion
    }
}
