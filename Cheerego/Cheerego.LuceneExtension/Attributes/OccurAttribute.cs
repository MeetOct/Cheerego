using Lucene.Net.Search;
using System;

namespace Cheerego.LuceneExtension.Attributes
{
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = true, Inherited = true)]
	public class OccurAttribute : Attribute
	{
		private Occur _occur;
		public Occur Occur
		{
			get { return _occur; }
		}

		public OccurAttribute(Occur occur = Occur.MUST_NOT)
		{
			this._occur = occur;
		}
	}
}
