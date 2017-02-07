using Cheerego.DapperExtension.Attributes;
using Dapper;
using DapperExtensions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

namespace Cheerego.DapperExtension
{
	public class DbContext : IDbContext
	{
		private readonly IDbConnection _conn;
		private IDbTransaction _tran;
		//private static readonly string connString = ConfigurationManager.ConnectionStrings["SqlConnectionString"].ConnectionString;

		public DbContext(string conn)
		{
			_conn = new SqlConnection(conn);
			_conn.Open();
		}

		private void BeginTran()
		{
			if (_tran == null)
			{
				_tran = _conn.BeginTransaction();
			}
		}
		public void EndTran()
		{
			_tran.Dispose();
		}
		public void Commit()
		{
			if (_tran != null && _tran.Connection != null && _tran.Connection.State == ConnectionState.Open)
			{
				_tran.Commit();
			}
		}

		public void Rollback()
		{
			if (_tran != null && _tran.Connection != null && _tran.Connection.State == ConnectionState.Open)
			{
				_tran.Rollback();
			}
		}

		public void Dispose()
		{
			_conn.Dispose();
		}

		#region DapperExtensions

		/// <summary>
		/// 新增
		/// </summary>
		/// <typeparam name="TEntity"></typeparam>
		/// <param name="entity"></param>
		/// <returns></returns>
		public virtual dynamic Insert<TEntity>(TEntity entity) where TEntity : class, new()
		{
			BeginTran();
			DapperExtensions.DapperExtensions.GetMap<TEntity>();
			return _conn.Insert(entity, _tran);
		}

		/// <summary>
		/// 批量新增
		/// </summary>
		/// <typeparam name="TEntity"></typeparam>
		/// <param name="entities"></param>
		/// <returns></returns>
		public virtual void InsertList<TEntity>(IEnumerable<TEntity> entities) where TEntity : class, new()
		{
			BeginTran();
			_conn.Insert(entities, _tran);
		}

		public virtual bool Delete<TEntity>(TEntity entity) where TEntity : class, new()
		{
			BeginTran();
			return _conn.Delete(entity, _tran);
		}

		/// <summary>
		/// 更新
		/// </summary>
		/// <typeparam name="TEntity"></typeparam>
		/// <param name="entity"></param>
		/// <returns></returns>
		public virtual bool Update<TEntity>(TEntity entity) where TEntity : class, new()
		{
			BeginTran();
			return _conn.Update(entity, _tran);
		}

		/// <summary>
		/// 更新
		/// </summary>
		/// <typeparam name="TEntity"></typeparam>
		/// <param name="entity"></param>
		/// <param name="field"></param>
		/// <returns></returns>
		public virtual bool Update<TEntity>(TEntity entity, List<string> field) where TEntity : class, new()
		{
			BeginTran();

			var strSql = new StringBuilder();
			var strUpdate = new StringBuilder();
			var param = new DynamicParameters();
			var t = entity.GetType();
			var properties = t.GetProperties();
			var keyName = string.Empty;

			foreach (var property in properties)
			{
				var attr = Attribute.GetCustomAttribute(property, typeof(IgnoreAttribute), false) as IgnoreAttribute;
				if (attr != null && attr.IsIgnore)
				{
					continue;
				}
				var key = Attribute.GetCustomAttribute(property, typeof(KeyAttribute), false) as KeyAttribute;
				if (key != null )
				{
					keyName = property.Name;
				}
				param.Add(property.Name, property.GetValue(entity));
			}

			if (string.IsNullOrWhiteSpace(keyName))
			{
				throw new Exception("请配置主键");
			}

			strSql.AppendFormat("UPDATE {0} SET ", typeof(TEntity).GetTableName());
			foreach (var item in field)
			{
				strUpdate.AppendFormat(" [{0}]=@{0},", item);
			}
			strSql.Append(strUpdate.ToString().TrimEnd(','));
			strSql.AppendFormat(" WHERE [{0}]=@{0} ", keyName);

			return _conn.Execute(strSql.ToString(), param, _tran) > 0;
		}

		#endregion

		#region Dapper

		/// <summary>
		/// SQL执行
		/// </summary>
		/// <param name="sql"></param>
		/// <param name="param"></param>
		/// <returns></returns>
		public bool Execute(string sql, dynamic param = null)
		{
			BeginTran();
			return SqlMapper.Execute(_conn, sql, param, _tran) > 0;
		}


		#endregion

		#region 查询

		/// <summary>
		/// 查询
		/// </summary>
		/// <typeparam name="TEntity"></typeparam>
		/// <param name="id"></param>
		/// <returns></returns>
		public virtual TEntity Get<TEntity>(object id) where TEntity : class, new()
		{
			return _conn.Get<TEntity>(id, _tran);
		}

		/// <summary>
		/// 查询
		/// </summary>
		/// <typeparam name="TEntity"></typeparam>
		/// <param name="predicate"></param>
		/// <param name="sort"></param>
		/// <returns></returns>
		public virtual IEnumerable<TEntity> GetList<TEntity>(object predicate = null, List<Tuple<bool, string>> sort = null) where TEntity : class, new()
		{
			if (sort == null)
			{
				return _conn.GetList<TEntity>(predicate, null, _tran);
			}
			IList<ISort> sortList = new List<ISort>();
			sort.ForEach(s => sortList.Add(new Sort() { Ascending = s.Item1, PropertyName = s.Item2 }));
			return _conn.GetList<TEntity>(predicate, sortList, _tran);
		}

		/// <summary>
		/// 分页查询
		/// </summary>
		/// <typeparam name="TEntity"></typeparam>
		/// <param name="predicate"></param>
		/// <param name="sort"></param>
		/// <param name="pageSize"></param>
		/// <param name="pageIndex"></param>
		/// <returns></returns>
		public virtual Tuple<int, IEnumerable<TEntity>> GetPage<TEntity>(object predicate, List<Tuple<bool, string>> sort, int pageSize, int pageIndex) where TEntity : class, new()
		{
			var count = _conn.Count<TEntity>(predicate, _tran);
			IList<ISort> sortList = new List<ISort>();
			sort.ForEach(s => sortList.Add(new Sort() { Ascending = s.Item1, PropertyName = s.Item2 }));
			return new Tuple<int, IEnumerable<TEntity>>(count, _conn.GetPage<TEntity>(predicate, sortList, pageIndex - 1, pageSize, _tran));
		}

		/// <summary>
		/// SQL列表
		/// </summary>
		/// <typeparam name="TEntity"></typeparam>
		/// <param name="sql"></param>
		/// <param name="param"></param>
		/// <returns></returns>
		public IEnumerable<TEntity> QueryList<TEntity>(string sql, dynamic param = null)
		{
			return SqlMapper.Query<TEntity>(_conn, sql, param, _tran);
		}

		/// <summary>
		/// SQL列表
		/// </summary>
		/// <typeparam name="TEntity"></typeparam>
		/// <param name="sql"></param>
		/// <param name="param"></param>
		/// <returns></returns>
		public IEnumerable<TReturn> QueryList<TFirst, TSecond, TReturn>(string sql, Func<TFirst, TSecond, TReturn> map, string splitOn = "Id", dynamic param = null)
		{
			return SqlMapper.Query<TFirst, TSecond, TReturn>(_conn, sql, map, param, _tran, true, splitOn);
		}

		/// <summary>
		/// SQL列表
		/// </summary>
		/// <typeparam name="TEntity"></typeparam>
		/// <param name="sql"></param>
		/// <param name="param"></param>
		/// <returns></returns>
		public IEnumerable<TReturn> QueryList<TFirst, TSecond, TThird, TReturn>(string sql, Func<TFirst, TSecond, TThird, TReturn> map, string splitOn = "Id", dynamic param = null)
		{
			return SqlMapper.Query<TFirst, TSecond, TThird, TReturn>(_conn, sql, map, param, _tran, true, splitOn);
		}

		/// <summary>
		/// SQL分页
		/// </summary>
		/// <typeparam name="TEntity"></typeparam>
		/// <param name="sql"></param>
		/// <param name="param"></param>
		/// <returns></returns>
		public Tuple<int, IEnumerable<TEntity>> QueryPage<TEntity>(string sql, int pageSize, int pageIndex, string orderBy, object param = null, string cte = null)
		{
			var countSql = string.Format(@"select count(1) as Num from(" + sql + ") as T", @"1 as N");

			var pageSql = string.Format(@"TOP {0} * FROM ( SELECT ROW_NUMBER() OVER ({1}) AS RowNum", pageSize, orderBy);
			sql = string.Format(sql + ") AS T WHERE RowNum BETWEEN ({1}-1)*{2}+1 AND {1}*{2} ORDER BY RowNum", pageSql, pageIndex, pageSize);

			var count = _conn.ExecuteScalar<int>(string.Format("{0} {1}", cte ?? "", countSql), param, _tran);
			return new Tuple<int, IEnumerable<TEntity>>(count, SqlMapper.Query<TEntity>(_conn, string.Format(@"{0} {1}", cte ?? "", sql), param, _tran));
		}

		/// <summary>
		/// SQL分页
		/// </summary>
		/// <typeparam name="TEntity"></typeparam>
		/// <param name="sql"></param>
		/// <param name="param"></param>
		/// <returns></returns>
		public Tuple<int, IEnumerable<TReturn>> QueryPage<TFirst, TSecond, TReturn>(string sql, int pageSize, int pageIndex, string orderBy, Func<TFirst, TSecond, TReturn> map, string splitOn = "Id", object param = null, string cte = null)
		{
			var countSql = string.Format(@"select count(1) as Num from(" + sql + ") as T", @"1 as N");

			var pageSql = string.Format(@"TOP {0} * FROM ( SELECT ROW_NUMBER() OVER ({1}) AS RowNum", pageSize, orderBy);
			sql = string.Format(sql + ") AS T WHERE RowNum BETWEEN ({1}-1)*{2}+1 AND {1}*{2} ORDER BY RowNum", pageSql, pageIndex, pageSize);

			var count = _conn.ExecuteScalar<int>(string.Format("{0} {1}", cte ?? "", countSql), param, _tran);
			return new Tuple<int, IEnumerable<TReturn>>(count, SqlMapper.Query<TFirst, TSecond, TReturn>(_conn, string.Format(@"{0} {1}", cte ?? "", sql), map, param, _tran, true, splitOn));
		}

		/// <summary>
		/// SQL分页
		/// </summary>
		/// <typeparam name="TEntity"></typeparam>
		/// <param name="sql"></param>
		/// <param name="param"></param>
		/// <returns></returns>
		public Tuple<int, IEnumerable<TReturn>> QueryPage<TFirst, TSecond, TThird, TReturn>(string sql, int pageSize, int pageIndex, string orderBy, Func<TFirst, TSecond, TThird, TReturn> map, string splitOn = "Id", object param = null, string cte = null)
		{
			var countSql = string.Format(@"select count(1) as Num from(" + sql + ") as T", @"1 as N");

			var pageSql = string.Format(@"TOP {0} * FROM ( SELECT ROW_NUMBER() OVER ({1}) AS RowNum", pageSize, orderBy);
			sql = string.Format(sql + ") AS T WHERE RowNum BETWEEN ({1}-1)*{2}+1 AND {1}*{2} ORDER BY RowNum", pageSql, pageIndex, pageSize);

			var count = _conn.ExecuteScalar<int>(string.Format("{0} {1}", cte ?? "", countSql), param, _tran);
			return new Tuple<int, IEnumerable<TReturn>>(count, SqlMapper.Query<TFirst, TSecond, TThird, TReturn>(_conn, string.Format(@"{0} {1}", cte ?? "", sql), map, param, _tran, true, splitOn));
		}

		#endregion

	}
}
