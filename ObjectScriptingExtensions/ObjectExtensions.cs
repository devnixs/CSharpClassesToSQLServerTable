using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace ObjectScriptingExtensions
{
    public static class ObjectExtensions
    {
        public static string CreateSchema(this object source)
        {
            Engine.Instance.CreateSchema(source);
            return Engine.Instance.GenerateSchema();
        }

		public static string CreateSchema(this object source, GenerateProperties settings)
		{
			Engine.Instance.CreateSchema(source, settings);
			return Engine.Instance.GenerateSchema();
		}

		public static string IgnoreEmpty(this string value)
		{
			if (string.IsNullOrWhiteSpace(value))
				return null;
			else
				return value;
		}

        public static object GetValueOrDbNull(this string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return DBNull.Value;
            else
                return value;
        }
    }

}
