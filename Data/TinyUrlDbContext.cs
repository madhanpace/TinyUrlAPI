using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using TinyUrlAPI.Models;


namespace TinyUrlAPI.Data
{
    public class TinyUrlDbContext : DbContext
    {
        public TinyUrlDbContext(DbContextOptions<TinyUrlDbContext> options) : base(options) { }
        public DbSet<TinyUrl> TinyUrls { get; set; }
    }
}
