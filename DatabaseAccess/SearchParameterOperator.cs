using System;
using System.Collections.Generic;
using System.Text;

namespace DatabaseAccess
{
    /// <summary>
    /// SQL parameter operator. 
    /// </summary>
    public enum SearchParameterOperator
    {
        equal = 0,
        notEqual = 1,
        like = 2,
        greater = 3,
        greaterOrEqual = 4,
        smaller = 5,
        smallerOrEqual = 6
    }
}
