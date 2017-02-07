namespace Cheerego.DapperExtension
{
	public class UnitOfWork : IUnitOfWork
	{
		private IDbContext _context;

		public bool IsRollback { get; private set; }

		public UnitOfWork(IDbContext context)
		{
			_context = context;
		}

		public void Commit()
		{
			if (!IsRollback)
			{
				_context.Commit();
			}
		}

		public void Rollback()
		{
			_context.Rollback();
			IsRollback = true;
		}

		public void Dispose()
		{
			_context.Dispose();
		}
	}
}
