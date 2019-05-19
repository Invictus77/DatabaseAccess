using System;
using System.Collections.Generic;
using System.Text;

namespace DataAccess.Attributes
{
    public class DatabaseNameAttribute : Attribute
    {
        private string _dbFieldname;

        public DatabaseNameAttribute(string dbFieldname)
        {
            _dbFieldname = dbFieldname;
        }

        public string Name
        {
            get { return _dbFieldname; }
        }
    }
}
