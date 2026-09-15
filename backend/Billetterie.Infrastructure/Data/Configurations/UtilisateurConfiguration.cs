using Billetterie.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Billetterie.Infrastructure.Data.Configurations
{
    public class UtilisateurConfiguration : IEntityTypeConfiguration<Utilisateur>
    {
        public void Configure(EntityTypeBuilder<Utilisateur> builder)
        {
            builder.Property(t => t.Email)
                .IsRequired()
                .HasMaxLength(254);

            builder.HasIndex(t => t.Email)
                .IsUnique();

            builder.Property(t => t.KeycloakId)
                .IsRequired()
                .HasMaxLength(64);

            builder.HasIndex(t => t.KeycloakId)
                .IsUnique();

            builder.Property(t => t.Nom)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(t => t.Prenom)
                .IsRequired()
                .HasMaxLength(100);

        }
    }
}
