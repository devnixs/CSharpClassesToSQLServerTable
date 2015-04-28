using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace ObjectScriptingExtensions
{
	public static class MemberInfoExtensions
	{
		public static Type ExtractType(this MemberInfo item)
		{
			if (item.MemberType == MemberTypes.Property)
			{
				return ((PropertyInfo)item).PropertyType;
			}
			else if (item.MemberType == MemberTypes.Field)
			{
				return ((FieldInfo)item).FieldType;
			}
			else
			{
				return (Type)item;
			}
		}
	}
}
