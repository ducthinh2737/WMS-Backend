using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

namespace Wms.Infrastructure.Persistence.Context
{
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

            // Connection string MySQL
            optionsBuilder.UseMySql(
                "server=localhost;port=3306;database=WmsDb222;user=testuser;password=123456",
                new MySqlServerVersion(new Version(8, 0, 32)) // phiến bản MySQL của bạn
            );

            return new AppDbContext(optionsBuilder.Options);
        }
    }
}
