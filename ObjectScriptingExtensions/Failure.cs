using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace ObjectScriptingExtensions
{
    public class Failure
    {
		private System.Reflection.MemberInfo item;

		public System.Reflection.MemberInfo Item
		{
			get { return item; }
			set { item = value; }
		}

        public static void CreateFailure(MemberInfo item, string name, string table)
        {
            Failure failure = new Failure();
            failure.Index = Engine.Instance.Failures.FirstUnique<Failure>(Engine.GenerateIndex(table, name));
            failure.Name = name;
            failure.Item = item;
            Engine.Instance.Failures.Add(failure.Index, failure);
        }
        public string Name { get; set; }
        public string Index { get; set; }

    }
}
