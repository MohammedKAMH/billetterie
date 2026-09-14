using System;
using System.Collections.Generic;
using System.Text;
using Billetterie.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Billetterie.Infrastructure.Data
{
    public class BilletterieDbContext : DbContext
    {
        public BilletterieDbContext(DbContextOptions<BilletterieDbContext> options)
            : base(options)
        {
        }

        public DbSet<Utilisateur> Utilisateurs => Set<Utilisateur>();
        public DbSet<Organisateur> Organisateurs => Set<Organisateur>();
        public DbSet<Evenement> Evenements => Set<Evenement>();
        public DbSet<TypeBillet> TypeBillets => Set<TypeBillet>();
        public DbSet<Reservation> Reservations => Set<Reservation>();
        public DbSet<Billet> Billets => Set<Billet>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }


    }
}
