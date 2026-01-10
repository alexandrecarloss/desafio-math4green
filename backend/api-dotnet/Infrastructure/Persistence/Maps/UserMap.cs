using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace api_dotnet.Infrastructure.Persistence.Configurations
{
    public class UserMap : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("Users");

            builder.HasKey(u => u.Id);

            builder.Property(u => u.Name)
                   .IsRequired()
                   .HasMaxLength(200);

            builder.HasMany(u => u.Tasks)
                   .WithOne(t => t.AssignedUser)
                   .HasForeignKey(t => t.AssignedUserId);
        }
    }
}
