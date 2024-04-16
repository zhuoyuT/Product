using Product.Model;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Product.Infra.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<ProductDetail> ProductDetails { get; set; }
        public DbSet<ApprovalQueue> ApprovalQueues { get; set; }
    }
}
