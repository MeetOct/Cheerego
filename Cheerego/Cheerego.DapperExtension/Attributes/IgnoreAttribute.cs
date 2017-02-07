using System;

namespace Cheerego.DapperExtension.Attributes
{
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
	public class IgnoreAttribute : Attribute
	{
		private bool _ignore = true;

		public IgnoreAttribute(bool ignore = true)
		{
			this._ignore = ignore;
		}

		public bool IsIgnore
		{
			get { return _ignore; }
			set { _ignore = value; }
		}
	}
}
