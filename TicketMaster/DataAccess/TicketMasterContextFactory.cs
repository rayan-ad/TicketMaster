using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TicketMaster.DataAccess
{
    public class TicketMasterContextFactory : IDesignTimeDbContextFactory<TicketMasterContext>
    {
        public TicketMasterContext CreateDbContext(string[] args)
        {
            var configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json")
                .Build();

            var optionsBuilder = new DbContextOptionsBuilder<TicketMasterContext>();
            optionsBuilder.UseSqlServer(configuration["ConnectionStrings:DefaultConnection"]);

            return new TicketMasterContext(optionsBuilder.Options);
        }
    }
}
