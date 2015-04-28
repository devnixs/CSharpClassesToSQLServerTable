using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ObjectScriptingExtensions
{
	public class Relation : ICloneable
	{
		public TableDefinition Table { get; set; }
		public TableDefinition ForeignTable { get; set; }
		public string Name { get; set; }
		public RelationshipType Type { get; set; }
		public string Override { get; set; }
		public string Grouping 
		{
			get
			{
				if (String.Compare(Table.Index,ForeignTable.Index) < 1)
					return Table.Index + ForeignTable.Index + Override.GetValueOrDefault(String.Empty);
				else
					return ForeignTable.Index + Table.Index + Override.GetValueOrDefault(String.Empty);
			}
		}

		public object Clone()
		{
			Relation relation = new Relation();
			relation.Type = this.Type;
			relation.ForeignTable = this.ForeignTable;
			relation.Name = this.Name;
			relation.Table = this.Table;
			relation.Override = this.Override;
			return relation;
		}
	}
}
