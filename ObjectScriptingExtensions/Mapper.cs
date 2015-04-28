using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Reflection;

namespace ObjectScriptingExtensions
{
	public static class Mapper
	{

		internal static bool IsSerializable(MemberInfo memberInfo)
		{
			Type type = memberInfo.ExtractType();
			return Engine.Instance.Mappings.ContainsKey(type);
		}

		internal static SqlDataType GetDataType(MemberInfo memberInfo)
		{
			Type type = memberInfo.ExtractType();
			return Engine.Instance.Mappings[type].DataType;
		}
		internal static Type GetSkeleton(Type type)
		{
			return Engine.Instance.Mappings[type].Skeleton;
		}
	}
}

