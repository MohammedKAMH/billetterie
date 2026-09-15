using Billetterie.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Billetterie.Infrastructure.Data.Configurations
{
    public class BilletConfiguration : IEntityTypeConfiguration<Billet>
    {
        public void Configure(EntityTypeBuilder<Billet> builder)
        {
            builder.Property(b => b.CodeUnique)
                .IsRequired()
                .HasMaxLength(64);

            builder.HasIndex(b => b.CodeUnique)
                .IsUnique();
        }
    }
}