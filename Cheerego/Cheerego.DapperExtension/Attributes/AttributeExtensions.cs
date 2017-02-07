using System;
using System.Reflection;

namespace Cheerego.DapperExtension.Attributes
{
	public static class AttributeExtensions
	{
		public static string GetTableName(this Type type)
		{
			var attr = type.GetCustomAttribute(typeof(Attributes.TableAttribute), true) as Attributes.TableAttribute;
			return attr != null ? attr.TableName : type.Name;
		}
	}
}
