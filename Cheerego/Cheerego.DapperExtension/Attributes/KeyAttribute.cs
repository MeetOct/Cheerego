using System;

namespace Cheerego.DapperExtension.Attributes
{
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
	public class KeyAttribute : Attribute
	{
		//private bool _isKey = true;

		////不需要iskey啊
		//public KeyAttribute(bool isKey = true)
		//{
		//	this._isKey = isKey;
		//}

		//public bool IsKey
		//{
		//	get { return _isKey; }
		//	set { _isKey = value; }
		//}
	}
}
