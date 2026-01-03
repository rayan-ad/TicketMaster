using TicketMaster.DataAccess;

namespace TicketMaster.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly TicketMasterContext _db;
        public UnitOfWork(TicketMasterContext db) => _db = db;
        public Task<int> SaveChangesAsync() => _db.SaveChangesAsync();
    }
}
