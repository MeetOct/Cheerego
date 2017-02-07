using Lucene.Net.Documents;
using System;

namespace Cheerego.LuceneExtension.Attributes
{
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
	public class FieldAttribute : Attribute
	{
		private Field.Index _index;
		public Field.Index Index
		{
			get { return _index; }
		}

		private Field.Store _store;
		public Field.Store Store
		{
			get { return _store; }
		}

		private Field.TermVector _termVector;
		public Field.TermVector TermVector
		{
			get { return _termVector; }
		}

		public FieldAttribute(Field.Index index = Field.Index.NO, Field.Store store = Field.Store.NO, Field.TermVector termVector = Field.TermVector.NO)
		{
			this._index = index;
			this._store = store;
			this._termVector = termVector;
		}
	}
}
