using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace ObjectScriptingExtensions
{
	public class TableDefinition
	{
		private Type type;

		public Type Type
		{
			get { return type; }
			set { type = value; }
		}

		public string Index { get; set; }
		public string SchemaName { get; set; }
		public string Name { get; set; }
		public string SuperTable { get; set; }
		private static string GenerateTableName(string name, bool isLookup)
		{
			string generatedName = string.Empty;
			if (isLookup)
				generatedName = Engine.Instance.Settings.LookupPrefix + name;
			else
				generatedName = Engine.Instance.Settings.TablePrefix + name;
			if(generatedName.Length > 127)
				return(generatedName.Substring(0,127));
			else 
				return generatedName;
		}


		private static void PopulateItems(Type type, List<MemberInfo> items, string superTable)
		{
			BindingFlags filter = BindingFlags.Instance | BindingFlags.Public;
			//always include superclasses if keep classhierarchies or top level objects
			if (Engine.Instance.Settings.Strategy == InheritanceStrategy.SplitClassHierarchies && superTable != null)
				filter = filter | BindingFlags.DeclaredOnly;

			IEnumerable<MemberInfo> props;
			props = type.GetProperties(filter).Cast<MemberInfo>(); 
			items.AddRange(props);

			if (Engine.Instance.Settings.IncludeFields)
			{
				IEnumerable<MemberInfo> fields;
				fields = type.GetFields(filter).Cast<MemberInfo>();
				items.AddRange(fields);
			}
		}
		public static TableDefinition CreateTableDefinition(Type type, string superTable, bool isLookup, string overrideName = null)
		{
			string name = type.Name;
			TableDefinition definition = null;
			if (!string.IsNullOrWhiteSpace(overrideName))
				name = overrideName;
			if (!Engine.Instance.TableDefinitions.ContainsKey(GenerateTableName(name, isLookup)))
			{
				definition = new TableDefinition();
				if (superTable.IgnoreEmpty() != null)
					definition.SuperTable = superTable;
				definition.type = type;
				definition.SchemaName = Engine.Instance.Settings.SchemaName;
				definition.Name = type.Name;
				definition.Index = GenerateTableName(name, isLookup);
				Engine.Instance.TableDefinitions.Add(definition.Index, definition);

				List<MemberInfo> items = new List<MemberInfo>();
				PopulateItems(type, items, superTable);
				ColumnDefinition columnDefinition;

			    bool HasBuiltInKey = items.Any(i=>i.GetCustomAttributes(false).Any(j=>j.GetType().Name.Contains("Key")));

			    if (!HasBuiltInKey)
                {
                //Create ID-column
                columnDefinition = ColumnDefinition.CreateColumnDefinition(null, Engine.Instance.Settings.IdName, definition.Index, KeyType.Primary);
                columnDefinition = ColumnDefinition.InsertColumnDefinition(columnDefinition, Engine.Instance.ColumnDefinitions);
                }
			        
				//Create foreign key for superclass
				if (!string.IsNullOrWhiteSpace(superTable))
				{
					columnDefinition = ColumnDefinition.CreateColumnDefinition(null, Engine.Instance.Settings.FkPrefix + superTable, definition.Index, KeyType.Foreign, superTable);
					columnDefinition = ColumnDefinition.InsertColumnDefinition(columnDefinition, Engine.Instance.ColumnDefinitions);
				}

				foreach (MemberInfo item in items)
				{
                    if(item.GetCustomAttributes(false).Any(j=>j.GetType().Name.Contains("Key")))
                    {
                        //Create ID-column
                        columnDefinition = ColumnDefinition.CreateColumnDefinition(item, item.Name, definition.Index, KeyType.Primary);
                        columnDefinition = ColumnDefinition.InsertColumnDefinition(columnDefinition, Engine.Instance.ColumnDefinitions);
				    }
                    else if (Mapper.IsSerializable(item))
					{
                        //create column for direct types
						columnDefinition = ColumnDefinition.CreateColumnDefinition(item, item.Name, definition.Index, KeyType.None);
						columnDefinition = ColumnDefinition.InsertColumnDefinition(columnDefinition, Engine.Instance.ColumnDefinitions);
					}
					//create lookuptable
					else if (item.ExtractType().IsEnum)
					{
						TableDefinition lookupDefinition;
						if (!Engine.Instance.TableDefinitions.ContainsKey(GenerateTableName(item.ExtractType().Name, true)))
							lookupDefinition = TableDefinition.CreateTableDefinition(typeof(EnumSkeleton),  null, true, item.ExtractType().Name);
						else
							lookupDefinition = Engine.Instance.TableDefinitions[GenerateTableName(item.ExtractType().Name, true)];

						Engine.Instance.Relations.Add(new Relation() { Table = definition , Type = RelationshipType.OneToOne, Name = item.Name, ForeignTable = lookupDefinition });
					}
					//create subtable, collection (only typed)
					else if (GetGenericListType(item.ExtractType()) != null)
					{
						Type containedType = GetGenericListType(item.ExtractType());
						if(Mapper.IsSerializable(containedType)) //basetype create skeleton table
						{
							TableDefinition tableDefinition;
							string skeletonName = GenerateTableName(definition.Name + "_" + item.Name, false);
							if (!Engine.Instance.TableDefinitions.ContainsKey(skeletonName))
								tableDefinition = TableDefinition.CreateTableDefinition(Mapper.GetSkeleton(containedType), null, false, skeletonName);
							else
								tableDefinition = Engine.Instance.TableDefinitions[skeletonName];
							string OverrideAttribute = GetOverrideAttribute(item);
							Engine.Instance.Relations.Add(new Relation() { Override = OverrideAttribute, Table = definition, Type = RelationshipType.ManyToMany, Name = item.Name, ForeignTable = tableDefinition });

						}
						else
						{
							//If in different assembly add to failures
							if (Engine.Instance.Settings.RestrictToAssembly && containedType.Assembly.FullName  != Engine.Instance.RestrictedAssembly)
							{
								Failure.CreateFailure(containedType, containedType.Name, definition.Name);
							}
							else
							{
								TableDefinition tableDefinition;
								if (!Engine.Instance.TableDefinitions.ContainsKey(GenerateTableName(containedType.Name, false)))
									tableDefinition = TableDefinition.CreateTableDefinition(containedType, null, false);
								else
									tableDefinition = Engine.Instance.TableDefinitions[GenerateTableName(containedType.Name, false)];
								string OverrideAttribute = GetOverrideAttribute(item);
								Relation relation = new Relation() { Override = OverrideAttribute, Table = definition, Type = RelationshipType.ManyToMany, Name = item.Name, ForeignTable = tableDefinition };
								Engine.Instance.Relations.Add(relation);
							}
						}

					}
					//create subtable, direct object
					else if (item.ExtractType() != null && item.ExtractType().IsClass)
					{
						//If in different assembly add to failures
						if (Engine.Instance.Settings.RestrictToAssembly && item.ExtractType().Assembly.FullName != Engine.Instance.RestrictedAssembly)
						{
							Failure.CreateFailure(item.ExtractType(), item.ExtractType().Name, definition.Name);
						}
						else
						{
							TableDefinition tableDefinition;
							if (!Engine.Instance.TableDefinitions.ContainsKey(GenerateTableName(item.ExtractType().Name, false)))
								tableDefinition = TableDefinition.CreateTableDefinition(item.ExtractType(), null, false);
							else
								tableDefinition = Engine.Instance.TableDefinitions[GenerateTableName(item.ExtractType().Name, false)];
							string OverrideAttribute = GetOverrideAttribute(item);
							Engine.Instance.Relations.Add(new Relation() {Override = OverrideAttribute, Table = definition, Type = RelationshipType.OneToOne, Name = item.Name, ForeignTable = tableDefinition });
						}
					}
					//could not serialize
					else
					{
						Failure.CreateFailure(item, item.Name, definition.Index);
					}

				}
				//Get all subclasses
				foreach (Type subclassType in type.Assembly.GetTypes().Where(t => t.IsSubclassOf(type)))
				{
					TableDefinition.CreateTableDefinition(subclassType, definition.Index, false);
				}
			}
			return definition;
		}

		private static string GetOverrideAttribute(MemberInfo type)
		{
			object[] overrideAttributes = type.GetCustomAttributes(typeof(RelationNameAttribute), true);
			if (overrideAttributes.Count() > 0)
				return ((RelationNameAttribute)overrideAttributes.First()).Hint;
			else
				return null;
		}
		private static Type GetGenericListType(Type type)
		{
			if (type == null)
				return null;
			foreach (Type interfaceType in type.GetInterfaces())
			{
				if (interfaceType.IsGenericType &&
					interfaceType.GetGenericTypeDefinition()
					== typeof(IList<>))
				{
					Type itemType = type.GetGenericArguments()[0];
					return itemType;
				}
			}
			return null;
		}


	}
}
