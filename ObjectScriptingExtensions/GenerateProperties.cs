using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ObjectScriptingExtensions
{
	public class GenerateProperties
	{
		public string LookupPrefix { get; set; }

		public string TablePrefix { get; set; }

		public string SchemaName { get; set; }

		public string IdName { get; set; }

		public string FkPrefix { get; set; }

		public IdTypes IdType { get; set; }

		public InheritanceStrategy Strategy { get; set; }

		public bool IncludeFields { get; set; }

		public bool RestrictToAssembly { get; set; }

		public GenerateProperties(string lookupPrefix = "", string tablePrefix = "", string schemaName = "dbo", string idName = "ID", string fkPrefix = "FK", 
			IdTypes idType = IdTypes.Identity, InheritanceStrategy strategy = InheritanceStrategy.SplitClassHierarchies, bool serializeFields = false,
			bool restrictToAssembly = true)
		{
			this.LookupPrefix = lookupPrefix;
			this.TablePrefix = tablePrefix;
			this.SchemaName = schemaName;
			this.IdName = idName;
			this.FkPrefix = fkPrefix;
			this.IdType = idType;
			this.Strategy = strategy;
			this.IncludeFields = serializeFields;
			this.RestrictToAssembly = restrictToAssembly;
		}
	}
}
