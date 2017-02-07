using System;
using System.Collections.Generic;

namespace Cheerego.DapperExtension
{
	public interface IDbContext : IDisposable
	{
		void EndTran();
		void Commit();
		void Rollback();

		dynamic Insert<TEntity>(TEntity entity) where TEntity : class, new();

		void InsertList<TEntity>(IEnumerable<TEntity> entities) where TEntity : class, new();

		bool Delete<TEntity>(TEntity entity) where TEntity : class, new();

		bool Update<TEntity>(TEntity entity) where TEntity : class, new();

		bool Update<TEntity>(TEntity entity, List<string> field) where TEntity : class, new();

		TEntity Get<TEntity>(object id) where TEntity : class, new();

		IEnumerable<TEntity> GetList<TEntity>(object predicate = null, List<Tuple<bool, string>> sort = null) where TEntity : class, new();

		Tuple<int, IEnumerable<TEntity>> GetPage<TEntity>(object predicate, List<Tuple<bool, string>> sort, int pageSize, int pageIndex) where TEntity : class, new();

		bool Execute(string sql, dynamic param = null);

		IEnumerable<TEntity> QueryList<TEntity>(string sql, dynamic param = null);

		IEnumerable<TReturn> QueryList<TFirst, TSecond, TReturn>(string sql, Func<TFirst, TSecond, TReturn> map, string splitOn = "Id", dynamic param = null);

		Tuple<int, IEnumerable<TEntity>> QueryPage<TEntity>(string sql, int pageSize, int pageIndex, string orderBy, object param = null, string cte = null);

		Tuple<int, IEnumerable<TReturn>> QueryPage<TFirst, TSecond, TReturn>(string sql, int pageSize, int pageIndex, string orderBy, Func<TFirst, TSecond, TReturn> map, string splitOn = "Id", object param = null, string cte = null);

		Tuple<int, IEnumerable<TReturn>> QueryPage<TFirst, TSecond, TThird, TReturn>(string sql, int pageSize, int pageIndex, string orderBy, Func<TFirst, TSecond, TThird, TReturn> map, string splitOn = "Id", object param = null, string cte = null);
	}
}
