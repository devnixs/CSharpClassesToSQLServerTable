using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ObjectScriptingExtensions
{

	[AttributeUsage(AttributeTargets.All)]
	public class RelationNameAttribute : Attribute 
	{
		public string Hint { get; set; }
		public RelationNameAttribute(string hint)  
		{
			this.Hint = hint;
		}
	}
}