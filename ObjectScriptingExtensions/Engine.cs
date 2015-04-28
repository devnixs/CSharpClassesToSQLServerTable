using System;
using System.Collections.Generic;
using System.Linq;
using System.Configuration;
using System.Text;
using System.IO;
using System.Reflection;
using System.Data;
using System.Resources;

namespace ObjectScriptingExtensions
{
    public sealed class Engine
    {
		public List<Relation> Relations { get; set; }
		public string RestrictedAssembly { get; set; }
        private static readonly Engine instance = new Engine();
        private string createTableTemplate;
		public GenerateProperties Settings { get; set; }
		public Dictionary<Type, TypeHolder> Mappings { get; set; }
        public string CreateTableTemplate
        {
            get { return createTableTemplate; }
            set { createTableTemplate = value; }
        }
		private string alterTableTemplate;

		public string AlterTableTemplate
		{
		  get { return alterTableTemplate; }
		  set { alterTableTemplate = value; }
		}

		private string failureTemplate;

		public string FailureTemplate
		{
			get { return failureTemplate; }
			set { failureTemplate = value; }
		}

        private string columnDefinitionTemplate;

        public string ColumnDefinitionTemplate
        {
            get { return columnDefinitionTemplate; }
            set { columnDefinitionTemplate = value; }
        }

        static Engine()
        {

        }
        public Engine() : this(new GenerateProperties())
        {
          
        }


		public Engine(GenerateProperties settings)
		{
			if (settings == null)
				throw new NullReferenceException("Cannot initialize engine with empty settings");
			Settings = settings;
			TableDefinitions = new Dictionary<string, TableDefinition>();
			Relations = new List<Relation>();
			Tables = new Dictionary<string, Table>();
			ColumnDefinitions = new Dictionary<string, ColumnDefinition>();
			Constraints = new Dictionary<string, ColumnDefinition>();
			Failures = new Dictionary<string, Failure>();
			string path = new FileInfo(Assembly.GetAssembly(typeof(Engine)).Location).DirectoryName;
			ResourceManager resources = new ResourceManager("ObjectScriptingExtensions.Templates", Assembly.GetExecutingAssembly());
			CreateTableTemplate = resources.GetString("CreateTableTemplate");
			AlterTableTemplate = resources.GetString("AlterTableTemplate");
			FailureTemplate = resources.GetString("FailureTemplate");
			Mappings = InitializeMappings();
		}

		private byte[] ReadBytesFromStream(string streamName)
		{
			using (System.IO.Stream stream = this.GetType().Assembly.GetManifestResourceStream(streamName))
			{
				byte[] result = new byte[stream.Length];
				stream.Read(result, 0, (int)stream.Length);
				return result;
			}
		}

        public static Engine Instance
        {
            get
            {
                return instance;
            }
        }

        public Dictionary<string, TableDefinition> TableDefinitions { get; set; }
        public Dictionary<string, Table> Tables { get; set; }
		public Dictionary<string, ColumnDefinition> ColumnDefinitions { get; set; }
		public Dictionary<string, ColumnDefinition> Constraints { get; set; }
		public Dictionary<string, Failure> Failures { get; set; }

		internal void CreateSchema(object source, GenerateProperties settings)
		{
			Settings = settings;
			CreateSchema(source);
		}

        internal void CreateSchema(object source)
        {
			RestrictedAssembly = source.GetType().Assembly.FullName;
            TableDefinition.CreateTableDefinition(source.GetType(), null,  false);
			//resolve relations as a second step to see relationship types.
			ResolveRelations();
        }

		private void ResolveRelations()
		{
			//Transform all relations according to type
			List<Relation> resolvedRelations = new List<Relation>();
			foreach (var item in Relations.GroupBy(s=>s.Grouping))
			{
				//in case of count 1 it is simple
				if (item.Count() == 1)
				{
					resolvedRelations.Add(item.First().Clone() as Relation);
				}
				//If all directed the same way generate separate relations
				else if(item.Count() == item.Where(s=>s.Table == item.First().Table).Count())
				{
					foreach (Relation relation in item)
					{
						resolvedRelations.Add(relation.Clone() as Relation);
					}
				}
				//if two opposite relations handle as one composite
				else if (item.Count() == 2)
				{
					//Two one to many = one many to many
					if (item.Where(s => s.Type == RelationshipType.OneToMany).Count() == 2)
					{
						Relation manyToMany = item.First().Clone() as Relation;
						manyToMany.Type = RelationshipType.ManyToMany;
						manyToMany.Name = item.First().Name + "_" + item.Last().Name;
						resolvedRelations.Add(manyToMany);
					}
					
					//Two one one = one one one;
					else if (item.Where(s => s.Type == RelationshipType.OneToOne).Count() == 2)
					{
						resolvedRelations.Add(item.First().Clone() as Relation);
					}
					//else use the one to many relation
					else
					{
						Relation relation = (Relation)item.First().Clone();
						relation.Type = RelationshipType.OneToMany;
						resolvedRelations.Add(relation);
					}
				}
				//generate separate
				else
				{
					foreach (Relation relation in item)
					{
						resolvedRelations.Add(relation.Clone() as Relation);
					}
				}
			}
			//Now go through all relations and generate FKs and Tables
			foreach (Relation resolved in resolvedRelations)
			{
				ColumnDefinition definition;
				switch (resolved.Type)
				{
					case RelationshipType.OneToOne:
						definition = ColumnDefinition.CreateColumnDefinition( null, Engine.instance.Settings.FkPrefix + resolved.Name, resolved.Table.Index, KeyType.Foreign, resolved.ForeignTable.Index);
						definition = ColumnDefinition.InsertColumnDefinition(definition, Constraints);
						definition = ColumnDefinition.InsertColumnDefinition(definition.CleanForeignKey(), ColumnDefinitions);
						break;
					case RelationshipType.OneToMany:
						definition = ColumnDefinition.CreateColumnDefinition(null, Engine.instance.Settings.FkPrefix + resolved.Name, resolved.Table.Index, KeyType.Foreign, resolved.ForeignTable.Index);
						definition = ColumnDefinition.InsertColumnDefinition(definition, Constraints);
						definition = ColumnDefinition.InsertColumnDefinition(definition.CleanForeignKey(), ColumnDefinitions);
						break;
					case RelationshipType.ManyToMany:
						TableDefinition relationTable = TableDefinition.CreateTableDefinition(typeof(RelationSkeleton), null, false, resolved.Table.Index + "_" + resolved.ForeignTable.Index + resolved.Override);
						definition = ColumnDefinition.CreateColumnDefinition(null, Engine.instance.Settings.FkPrefix + resolved.ForeignTable.Name + "_" + resolved.Table.Name, relationTable.Index, KeyType.Foreign, resolved.Table.Index);
						definition = ColumnDefinition.InsertColumnDefinition(definition, Constraints);
						definition = ColumnDefinition.InsertColumnDefinition(definition.CleanForeignKey(), ColumnDefinitions);
						definition = ColumnDefinition.CreateColumnDefinition(null, Engine.instance.Settings.FkPrefix + resolved.Table.Name + "_" + resolved.ForeignTable.Name, relationTable.Index, KeyType.Foreign, resolved.ForeignTable.Index);
						definition = ColumnDefinition.InsertColumnDefinition(definition, Constraints);
						definition = ColumnDefinition.InsertColumnDefinition(definition.CleanForeignKey(), ColumnDefinitions);
						break;
					default:
						break;
				}
			}
		}

        internal string GenerateSchema()
        {
            List<string> completed = new List<string>();
            string script = string.Empty;
            int loopCount = 0;
            //Loop through tables until all are processed or count > 1000 (probable error)
            while(completed.Count < TableDefinitions.Count && loopCount <1000)
            {
                loopCount = loopCount + 1;
                foreach(TableDefinition table in TableDefinitions.Values)
                {
                    if (!completed.Contains(table.Index) && !HasDependencyToIncomplete(completed, table.Index))
                    {
                        script = script + GenerateScript(table, ColumnDefinitions.Values.Where(s => s.Table == table.Index).ToList<ColumnDefinition>()) + "\r\n";
                        completed.Add(table.Index);
                    }
                }
            }
			//Add constraints for foreign keys
			script = script + "\r\n" + GenerateConstraints();
			script = script + "\r\n" + GenerateFailures();
			return script;
        }

		private string GenerateFailures()
		{
			string fullScript = string.Empty;
			fullScript = FailureTemplate;
			fullScript = fullScript.Replace("|scriptDate|", DateTime.Now.ToShortDateString());
			fullScript = fullScript.Replace("|failures|", GenerateFailureItems());
			return fullScript;

		}

		private string GenerateFailureItems()
		{
			string script = string.Empty;
			foreach (Failure  item in Failures.Values)
			{
				if(item.Item.MemberType == MemberTypes.TypeInfo)
					script = script + ((Type)(item.Item)).FullName + " " + item.Item.Name + " in " + item.Item.Module.Name + "\r\n";
				else
					script = script + item.Item.ExtractType().FullName  + " " + item.Item.Name + " in " + item.Item.Module.Name + "\r\n";
			}
			return script;
		}

		private string GenerateConstraints()
		{
			string fullScript = string.Empty;
			foreach (ColumnDefinition  definition in Constraints.Values)
			{
				string script = alterTableTemplate;
				script = script.Replace("|constraint|", definition.Index);
				script = script.Replace("|scriptDate|", DateTime.Now.ToShortDateString());
				script = script.Replace("|schema|", Settings.SchemaName);
				script = script.Replace("|tableName|", definition.Table);
				script = script.Replace("|tableName2|", definition.ReferencedTable);
				script = script.Replace("|foreignKey|", Engine.Instance.Settings.IdName);
				script = script.Replace("|primaryKey|", definition.Name);
				fullScript = fullScript + script + "\r\n";
			}
			return fullScript;
		}

        private string GenerateScript(TableDefinition table, List<ColumnDefinition> list)
        {
            string script = CreateTableTemplate;
            script = script.Replace("|scriptDate|", DateTime.Now.ToShortDateString());
			script = script.Replace("|schema|", Settings.SchemaName);
            script = script.Replace("|tableName|", table.Index);
            script = script.Replace("|columnDefinitions|", GenerateColumnDefinitions(list));
            return script;
        }

        private string GenerateColumnDefinitions(List<ColumnDefinition> columns)
        {
            string fullScript = string.Empty;
			int index = 0;
            foreach (ColumnDefinition column in columns)
            {
				index = index + 1;
                string script = "[" + column.Name.Trim() + "] " + column.DataType.Name;
                if (column.IsIdentity)
                    script = script + " IDENTITY";
                if (column.DataType.IsNullable)
                    script = script + " NULL";
                else
                    script = script + " NOT NULL";
                if (column.ColumnType == KeyType.Primary)
                    script = script + " PRIMARY KEY";
                if (column.ColumnType == KeyType.Foreign)
                    script = script + " FOREIGN KEY REFERENCES [" + column.ReferencedTable + "]([" + Engine.Instance.Settings.IdName + "])";
				if (index < columns.Count)
					script = script + ",";
                fullScript = fullScript + script + "\r\n";
            }
            return fullScript;
        }

        private bool HasDependencyToIncomplete(List<string> completed, string table)
        {
            foreach (ColumnDefinition column in ColumnDefinitions.Values.Where(s=>s.Table == table))
            {
                if (!string.IsNullOrWhiteSpace(column.ReferencedTable) && !completed.Contains(column.ReferencedTable) && table != column.ReferencedTable)
                    return true;
            }
            return false;
        }
        public static string GenerateIndex(string tableName, string name)
        {
            return tableName + "_" + name;
        }
		private Dictionary<Type, TypeHolder> InitializeMappings()
		{
			Dictionary<Type, TypeHolder> maps = new Dictionary<Type, TypeHolder>();
			SqlDataType varCharType = new SqlDataType() {  MaxLength = 8000, Type = SqlDbType.NVarChar, Name = "NVARCHAR(4000)", IsNullable = true };
			SqlDataType varChar1Type = new SqlDataType() { MaxLength = 1, Type = SqlDbType.NVarChar, Name = "NVARCHAR(1)", IsNullable =	false };
			SqlDataType varBinaryType = new SqlDataType() {  MaxLength = 8000, Type = SqlDbType.VarBinary, Name = "VARBINARY(8000)", IsNullable = true };
			SqlDataType intType = new SqlDataType() {  Type = SqlDbType.Int, Name = "INT", IsNullable = false };
			SqlDataType smallIntType = new SqlDataType() { Type = SqlDbType.SmallInt, Name = "SMALLINT", IsNullable = false };
			SqlDataType tinyIntType = new SqlDataType() {  Type = SqlDbType.TinyInt, Name = "TINYINT", IsNullable = false };
			SqlDataType decimalType = new SqlDataType() {  Type = SqlDbType.Decimal, Name = "DECIMAL(18,2)", IsNullable = false };
			SqlDataType bigIntType = new SqlDataType() {  Type = SqlDbType.BigInt, Name = "BIGINT", IsNullable = false };
			SqlDataType floatType = new SqlDataType() {  Type = SqlDbType.Float, Name = "FLOAT", IsNullable = false };
			SqlDataType realType = new SqlDataType() {  Type = SqlDbType.Real, Name = "REAL", IsNullable = false };
			SqlDataType bitType = new SqlDataType() {  Type = SqlDbType.Bit, Name = "BIT", IsNullable = false };
			SqlDataType guidType = new SqlDataType() {  Type = SqlDbType.UniqueIdentifier, Name = "UNIQUEIDENTIFIER", IsNullable = false };
			SqlDataType dateType = new SqlDataType() {  Type = SqlDbType.DateTime, Name = "DATETIME", IsNullable = false };
			SqlDataType timeType = new SqlDataType() {  Type = SqlDbType.Time, Name = "TIME", IsNullable = false };
			SqlDataType nullableDecimalType = new SqlDataType() {  Type = SqlDbType.Decimal, Name = "DECIMAL(18,2)", IsNullable = true };
			SqlDataType nullableVarChar1Type = new SqlDataType() {  MaxLength = 1, Type = SqlDbType.NVarChar, Name = "NVARCHAR(1)", IsNullable = true };
			SqlDataType nullableIntType = new SqlDataType() {  Type = SqlDbType.Int, Name = "INT", IsNullable = true };
			SqlDataType nullableBigIntType = new SqlDataType() {  Type = SqlDbType.BigInt, Name = "BIGINT", IsNullable = true };
			SqlDataType nullableSmallIntType = new SqlDataType() {  Type = SqlDbType.SmallInt, Name = "SMALLINT", IsNullable = true };
			SqlDataType nullableTinyIntType = new SqlDataType() {  Type = SqlDbType.TinyInt, Name = "TINYINT", IsNullable = true };
			SqlDataType nullableFloatType = new SqlDataType() {  Type = SqlDbType.Float, Name = "FLOAT", IsNullable = true };
			SqlDataType nullableRealType = new SqlDataType() {  Type = SqlDbType.Real, Name = "REAL", IsNullable = true };
			SqlDataType nullableBitType = new SqlDataType() {  Type = SqlDbType.Bit, Name = "BIT", IsNullable = true };
			SqlDataType nullableDateType = new SqlDataType() {  Type = SqlDbType.DateTime, Name = "DATETIME", IsNullable = true };
			SqlDataType nullableTimeType = new SqlDataType() {  Type = SqlDbType.Time, Name = "TIME", IsNullable = true };
			SqlDataType nullableGuidType = new SqlDataType() {  Type = SqlDbType.UniqueIdentifier, Name = "UNIQUEIDENTIFIER", IsNullable = true };

			maps.Add(typeof(string), new TypeHolder() { DataType = varCharType, Skeleton = typeof(StringSkeleton) });
			maps.Add(typeof(char), new TypeHolder() { DataType = varChar1Type, Skeleton = typeof(CharSkeleton) });
			maps.Add(typeof(char?), new TypeHolder() { DataType = nullableVarChar1Type, Skeleton = typeof(NullableCharSkeleton) });
			maps.Add(typeof(Byte[]), new TypeHolder() { DataType = varBinaryType, Skeleton = typeof(ByteArraySkeleton) });
			maps.Add(typeof(int), new TypeHolder() { DataType = intType, Skeleton = typeof(IntSkeleton) });
			maps.Add(typeof(int?), new TypeHolder() { DataType = nullableIntType, Skeleton = typeof(NullableIntSkeleton) });
			maps.Add(typeof(short), new TypeHolder() { DataType = smallIntType, Skeleton = typeof(ShortSkeleton) });
			maps.Add(typeof(short?), new TypeHolder() { DataType = nullableSmallIntType, Skeleton = typeof(NullableShortSkeleton) });
			maps.Add(typeof(decimal), new TypeHolder() { DataType = decimalType, Skeleton = typeof(DecimalSkeleton) });
			maps.Add(typeof(decimal?), new TypeHolder() { DataType = nullableDecimalType, Skeleton = typeof(NullableDecimalSkeleton) });
			maps.Add(typeof(long), new TypeHolder() { DataType = bigIntType, Skeleton = typeof(LongSkeleton) });
			maps.Add(typeof(long?), new TypeHolder() { DataType = nullableBigIntType, Skeleton = typeof(NullableLongSkeleton) });
			maps.Add(typeof(double), new TypeHolder() { DataType = floatType, Skeleton = typeof(FloatSkeleton) });
			maps.Add(typeof(double?), new TypeHolder() { DataType = nullableFloatType, Skeleton = typeof(NullableFloatSkeleton) });
			maps.Add(typeof(Single), new TypeHolder() { DataType = realType, Skeleton = typeof(SingleSkeleton) });
			maps.Add(typeof(Single?), new TypeHolder() { DataType = nullableRealType, Skeleton = typeof(NullableSingleSkeleton) });
			maps.Add(typeof(bool), new TypeHolder() { DataType = bitType, Skeleton = typeof(BoolSkeleton) });
			maps.Add(typeof(bool?), new TypeHolder() { DataType = nullableBitType, Skeleton = typeof(NullableBoolSkeleton) });
			maps.Add(typeof(Guid), new TypeHolder() { DataType = guidType, Skeleton = typeof(GuidSkeleton) });
			maps.Add(typeof(Guid?), new TypeHolder() { DataType = nullableGuidType, Skeleton = typeof(NullableGuidSkeleton) });
			maps.Add(typeof(DateTime), new TypeHolder() { DataType = dateType, Skeleton = typeof(DateTimeSkeleton) });
			maps.Add(typeof(DateTime?), new TypeHolder() { DataType = nullableDateType, Skeleton = typeof(NullableDateTimeSkeleton) });
			maps.Add(typeof(TimeSpan), new TypeHolder() { DataType = timeType, Skeleton = typeof(TimeSpanSkeleton) });
			maps.Add(typeof(TimeSpan?), new TypeHolder() { DataType = nullableTimeType, Skeleton = typeof(NullableTimeSpanSkeleton) });

			return maps;
		}
    }
}
