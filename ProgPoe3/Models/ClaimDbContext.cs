using Microsoft.EntityFrameworkCore;
using ProgPoe3.Models;
using System.Collections.Generic;

namespace ProgPoe3.Data
{
    public class ClaimDbContext : DbContext
    {
        public ClaimDbContext(DbContextOptions<ClaimDbContext> options)
            : base(options)
        {
        }

        public DbSet<Claim> Claims { get; set; }
        public DbSet<ApprovedClaim> ApprovedClaims { get; set; } // New DbSet for approved claims
    }
}