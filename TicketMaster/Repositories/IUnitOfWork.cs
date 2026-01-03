namespace TicketMaster.Repositories
{
    public interface IUnitOfWork
    {
        Task<int> SaveChangesAsync();

    }
}
