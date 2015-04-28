using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace ObjectScriptingExtensions
{
    public class ColumnDefinition
    {
		private System.Reflection.MemberInfo item;

		public System.Reflection.MemberInfo Item
		{
			get { return item; }
			set { item = value; }
		}

		public static ColumnDefinition CreateColumnDefinition(MemberInfo item, string name, string table, KeyType keyType, string referredTable = null)
		{
            ColumnDefinition definition = new ColumnDefinition();
            definition.Item = item;
            definition.Table = table;
			definition.Name = name;
            definition.ColumnType = keyType;
			switch (keyType)
			{
				case KeyType.None:
					definition.DataType = Mapper.GetDataType(item);
					break;
				case KeyType.Primary:
					{
						if (Engine.Instance.Settings.IdType == IdTypes.Identity)
						{
                            definition.DataType = new SqlDataType() { Type = System.Data.SqlDbType.Int, IsNullable = false, Name = "INT" };
                            definition.IsIdentity = true;
						}
						else
						{
                            definition.DataType = new SqlDataType() { Type = System.Data.SqlDbType.UniqueIdentifier, IsNullable = false, Name = "UNIQUEIDENTIFIER" };
                            definition.IsIdentity = false;
						}
					}
					break;
				case KeyType.Foreign:
					{
						if (string.IsNullOrWhiteSpace(referredTable))
							throw new NullReferenceException();
                        definition.ReferencedTable = referredTable;
						//definition.ReferencedColumn = Engine.Instance.ColumnDefinitions.Where(s => s.Value.Table == referredTable && s.Value.ColumnType == KeyType.Primary).First().Value.Name;

						if (Engine.Instance.Settings.IdType == IdTypes.Identity)
						{
                            definition.DataType = new SqlDataType() { Type = System.Data.SqlDbType.Int, IsNullable = false, Name = "INT" };
						}
						else
						{
                            definition.DataType = new SqlDataType() { Type = System.Data.SqlDbType.UniqueIdentifier, IsNullable  = false, Name = "UNIQUEIDENTIFIER" };
						}
					}
					break;
				case KeyType.Enum:
					{
						if (string.IsNullOrWhiteSpace(referredTable))
							throw new NullReferenceException();
						definition.ReferencedTable = referredTable;
						//definition.ReferencedColumn = Engine.Instance.ColumnDefinitions.Where(s => s.Value.Table == referredTable && s.Value.ColumnType == KeyType.Primary).First().Value.Name;

						if (Engine.Instance.Settings.IdType == IdTypes.Identity)
						{
							definition.DataType = new SqlDataType() { Type = System.Data.SqlDbType.Int, IsNullable  = false, Name = "INT" };
						}
						else
						{
							definition.DataType = new SqlDataType() { Type = System.Data.SqlDbType.UniqueIdentifier, IsNullable = false, Name = "UNIQUEIDENTIFIER" };
						}
					}
					break;
				default:
					break;
			}
 			return definition;
		}
		internal static ColumnDefinition InsertColumnDefinition(ColumnDefinition definition, Dictionary<string, ColumnDefinition> dictionary)
		{
			definition.Index = GetUniqueIndex(definition);
			definition.Name = GetUniqueName(definition, dictionary);
			dictionary.Add(definition.Index, definition);
			return definition;
		}

		private static string GetUniqueName(ColumnDefinition definition, Dictionary<string, ColumnDefinition> dictionary )
		{
			string name = definition.Name;
			int i = 1;
			while(dictionary.Values.Where(s=>s.Table == definition.Table && s.Name == name).Count() > 0)
			{
				name = definition.Name + i.ToString();
				i = i + 1;
			}
			return name;
		}

		private static string GetUniqueIndex(ColumnDefinition definition)
		{
			return Engine.Instance.ColumnDefinitions.MergeLeft(Engine.Instance.Constraints).FirstUnique<ColumnDefinition>(Engine.GenerateIndex(definition.Table, definition.Name));
		}
		private bool isIdentity = false;

		public bool IsIdentity
		{
			get { return isIdentity; }
			set { isIdentity = value; }
		}
        public string Name { get; set; }
        public string Table { get; set; }
		public string Index { get; set; }
        public SqlDataType DataType { get; set; }
		private KeyType columnType = KeyType.None;

		public KeyType ColumnType
		{
			get { return columnType; }
			set { columnType = value; }
		}

        public string ReferencedTable { get; set; }


		internal ColumnDefinition CleanForeignKey()
		{
			ColumnDefinition definition = new ColumnDefinition();
			definition.Index = GetUniqueIndex(this);
			definition.ColumnType = KeyType.None;
			definition.DataType = this.DataType;
			definition.IsIdentity = false;
			definition.Item = null;
			definition.Name = this.Name;
			definition.ReferencedTable = null;
			definition.Table = this.Table;
			return definition;
		}
	}
}
