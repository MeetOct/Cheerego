using Cheerego.Core.Reflection;
using Cheerego.LuceneExtension.Attributes;
using Dos.PanGu;
using Dos.PanGu.HighLight;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.PanGu;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Lucene.Net.Store;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;

namespace Cheerego.LuceneExtension
{
	public class IndexContext
	{
		private FSDirectory directory;
		private Analyzer analyzer;
		private static IndexContext indexHelper;

		private static readonly object instanceLocker = new object();
		private static readonly object functionLocker = new object();

		public static string IndexPath { get; set; }

		private IndexContext()
		{
			//索引库存放在这个文件夹里
			string indexPath = AppDomain.CurrentDomain.BaseDirectory.ToString() + (!string.IsNullOrEmpty(IndexPath) ? IndexPath : string.Empty);
			directory = FSDirectory.Open(new DirectoryInfo(indexPath));
			//盘古分词（需要配置词库）
			analyzer = new PanGuAnalyzer();
		}

		public static IndexContext GetInstance()
		{
			if (indexHelper == null)
			{
				lock (instanceLocker)
				{
					if (indexHelper == null)
					{
						indexHelper = new IndexContext();
					}
				}
			}
			return indexHelper;
		}


		public void Write<T>(T value, bool init = false) where T : class
		{
			lock (functionLocker)
			{
				using (IndexWriter writer = new IndexWriter(directory, analyzer, init, IndexWriter.MaxFieldLength.UNLIMITED))
				{
					try
					{
						writer.AddDocument(CreateDocument(value));
						writer.Commit();
						writer.Optimize();
					}
					catch (Exception ex)
					{
						writer.Rollback();
					}
				}
			}
		}

		public void Write<T>(IEnumerable<T> list, bool init = false) where T : class
		{
			lock (functionLocker)
			{
				using (IndexWriter writer = new IndexWriter(directory, analyzer, init, IndexWriter.MaxFieldLength.UNLIMITED))
				{
					try
					{
						foreach (var value in list)
						{
							writer.AddDocument(CreateDocument(value));
						}
						writer.Commit();
						writer.Optimize();
					}
					catch (Exception ex)
					{
						writer.Rollback();
					}
				}
			}
		}

		private Document CreateDocument<T>(T value) where T : class
		{
			Document doc = new Document();
			PropertyDescriptorCollection pdc = TypeDescriptor.GetProperties(typeof(T));
			foreach (PropertyDescriptor property in pdc)
			{
				property.GetChildProperties();
			}
			PropertyInfo[] properties = typeof(T).GetProperties();
			foreach (var property in properties)
			{
				if (!property.IsDefined(typeof(FieldAttribute), false)) continue;

				FieldAttribute attribute = property.GetCustomAttribute(typeof(FieldAttribute)) as FieldAttribute;

				Field.Index index = attribute.Index;
				Field.Store store = attribute.Store;
				Field.TermVector termVector = attribute.TermVector;

				Func<object, object> getValue = ReappearMember.CreatePropertyGetter(property);
				string strPropVal = getValue(value)?.ToString();
				doc.Add(new Field(property.Name, strPropVal ?? string.Empty, store, index, termVector));
			}
			return doc;
		}

		public IList<T> Retrieve<T>(string keyword, out int totalCount, int pageIndex = 1, int pageSize = 10) where T : class, new()
		{
			using (IndexSearcher searcher = new IndexSearcher(directory, true))
			{
				List<string> queries = new List<string>();
				List<string> fields = new List<string>();
				List<Occur> flags = new List<Occur>();
				List<SortField> sortFields = new List<SortField>();
				PropertyInfo[] properties = typeof(T).GetProperties();

				foreach (var property in properties)
				{
					if (property.IsDefined(typeof(OccurAttribute), false))
					{
						OccurAttribute attribute = property.GetCustomAttribute(typeof(OccurAttribute)) as OccurAttribute;
						Occur occur = attribute.Occur;
						if (!occur.Equals(Occur.MUST_NOT))
						{
							///这里queries,fields，flags一一对应，见MultiFieldQueryParser.Parse方法说明
							queries.Add(keyword);
							fields.Add(property.Name);
							flags.Add(occur);
						}
					}
					if (property.IsDefined(typeof(SortAttribute), false))
					{
						SortAttribute attribute = property.GetCustomAttribute(typeof(SortAttribute)) as SortAttribute;
						int sortField = attribute.Type;
						bool reverse = attribute.Reverse;
						sortFields.Add(new SortField(property.Name, sortField, reverse));
					}
				}

				Query query = MultiFieldQueryParser.Parse(Lucene.Net.Util.Version.LUCENE_30, queries?.ToArray(), fields?.ToArray(), flags?.ToArray(), analyzer);

				//Query queryR= new TermRangeQuery()

				TopDocs tds;
				int startRowIndex = (pageIndex - 1) * pageSize;  //分页
				if (sortFields.Count > 0)
				{
					Sort sort = new Sort(sortFields?.ToArray());
					TopFieldCollector collector = TopFieldCollector.Create(sort, pageIndex * pageSize, false, false, false, false);
					searcher.Search(query, collector); //返回结果
					tds = collector.TopDocs(startRowIndex, pageSize);
				}
				else
				{
					TopScoreDocCollector collector = TopScoreDocCollector.Create(pageIndex * pageSize, false);
					searcher.Search(query, collector);
					tds = collector.TopDocs(startRowIndex, pageSize);
				}
				totalCount = tds.TotalHits;

				IList<T> list = new List<T>();
				foreach (ScoreDoc sd in tds.ScoreDocs)
				{
					Document doc = searcher.Doc(sd.Doc);
					T searchResult = new T();
					foreach (var property in properties)
					{
						string value = doc.Get(property.Name);
						if (!string.IsNullOrEmpty(value))
						{
							Action<object, object> setValue = ReappearMember.CreatePropertySetter(property);
							if (property.IsDefined(typeof(OccurAttribute), false))
								setValue(searchResult, Preview(value, keyword));
							else
								setValue(searchResult, value);
						}
					}
					list.Add(searchResult);
				}
				return list;
			}
		}

		private string Preview(string body, string keyword)
		{
			SimpleHTMLFormatter simpleHTMLFormatter = new SimpleHTMLFormatter("<font color=\"Red\">", "</font>");
			Highlighter highlighter = new Highlighter(simpleHTMLFormatter, new Segment());
			highlighter.FragmentSize = 50; //大小
			string bodyPreview = highlighter.GetBestFragment(keyword, body); //最佳匹配摘要
			if (string.IsNullOrEmpty(bodyPreview))
			{
				return body;
			}
			else
			{
				return bodyPreview;
			}
		}
	}
}
