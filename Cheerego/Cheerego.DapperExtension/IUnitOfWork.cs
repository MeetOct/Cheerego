using System;

namespace Cheerego.DapperExtension
{
	public interface IUnitOfWork : IDisposable
	{
		bool IsRollback { get; }
		void Commit();
		void Rollback();
	}
}
