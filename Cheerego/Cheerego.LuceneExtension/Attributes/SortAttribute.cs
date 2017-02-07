using Lucene.Net.Search;
using System;

namespace Cheerego.LuceneExtension.Attributes
{
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
	public class SortAttribute : Attribute
	{
		public SortAttribute()
		{
			this._type = SortField.SCORE;
			this._reverse = false;
		}

		private int _type;
		public int Type
		{
			get { return _type; }
			set { _type = value; }
		}

		private bool _reverse;
		public bool Reverse
		{
			get { return _reverse; }
			set { _reverse = value; }
		}

	}
}
