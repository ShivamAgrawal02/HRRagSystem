using HRRagSystem.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HRRagSystem.Database
{
    public class HRRagSystemSQLiteDbContext:DbContext
    {
        public HRRagSystemSQLiteDbContext(DbContextOptions<HRRagSystemSQLiteDbContext> options):base(options)
        {
            
        }
        public DbSet<DocumentInfo> Documents { get; set; }
        protected override void OnModelCreating(
            ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<DocumentInfo>(entity =>
            {
                entity.HasKey(x => x.DocumentId);

                entity.HasIndex(x => x.RelativePath)
                    .IsUnique();

                entity.Property(x => x.FileName)
                    .IsRequired();

                entity.Property(x => x.RelativePath)
                    .IsRequired();

                entity.Property(x => x.FileHash)
                    .IsRequired();

                entity.Property(x => x.IngestionVersion)
                    .IsRequired();
            });
        }
    }
}
