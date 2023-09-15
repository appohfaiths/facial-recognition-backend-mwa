using Microsoft.EntityFrameworkCore;
using FacialRecognition.Models;
using System.Security.Cryptography.X509Certificates;

namespace FacialRecognition.Data
{
    public class FacialRecognitionContext : DbContext
    {
        public FacialRecognitionContext(DbContextOptions options) : base(options)  {}
        public DbSet<Photo> Photos { get; set; }
        public DbSet<RecognizedFace> RecognizedFaces { get; set; }

         protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Photo>()
                .HasMany(p => p.RecognizedFaces)
                .WithOne(rf => rf.Photo)
                .HasForeignKey(rf => rf.PhotoId);
                
         modelBuilder.Entity<RecognizedFace>()
                .HasOne(rf => rf.Photo)
                .WithMany(p => p.RecognizedFaces)
                .HasForeignKey(rf => rf.PhotoId);
    }
    }
}