using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ObjectScriptingExtensions
{
    public class Table
    {
        public Table()
        {
            Rows = new Dictionary<int, Row>();
        }
        public string DefinitionKey { get; set; }
        public Dictionary<int, Row> Rows { get; set; }
    }
}
