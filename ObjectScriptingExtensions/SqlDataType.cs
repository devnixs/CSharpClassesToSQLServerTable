using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

namespace ObjectScriptingExtensions
{
    public class SqlDataType
    {
        public SqlDbType  Type { get; set; }
        public int MaxLength { get; set; }
        public string DefaultValue { get; set; }
		public string Name { get; set; }
		public bool IsNullable { get; set; }

    }
}
