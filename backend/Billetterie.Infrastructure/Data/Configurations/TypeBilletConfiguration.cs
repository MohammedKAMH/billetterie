using Billetterie.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Billetterie.Infrastructure.Data.Configurations
{
    public class TypeBilletConfiguration : IEntityTypeConfiguration<TypeBillet>
    {
        public void Configure(EntityTypeBuilder<TypeBillet> builder)
        {
            builder.Property(t => t.Nom)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(t => t.Prix)
                .HasPrecision(10, 2);

            builder.ToTable(t => t.HasCheckConstraint(
                "CK_TypeBillet_QuantiteVendue",
                "\"QuantiteVendue\" <= \"QuantiteTotale\""));
        }
    }
}
