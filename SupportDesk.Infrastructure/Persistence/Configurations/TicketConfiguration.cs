using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SupportDesk.Domain.Entities;

public class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        builder.ToTable("Tickets");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();   // الـ Id بيتولّد في الـ Domain

        builder.Property(t => t.Title).IsRequired().HasMaxLength(200);
        builder.Property(t => t.Description).IsRequired().HasMaxLength(4000);

        // الـ enums كنص مقروء في الداتابيز
        builder.Property(t => t.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(t => t.Priority).HasConversion<string>().HasMaxLength(20);
        builder.Property(t => t.RowVersion).IsRowVersion();

        // Category: علاقة حقيقية (Foreign Key)
        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(t => t.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // CustomerId و AgentId: أعمدة Guid من غير FK لـ User لحد المرحلة 5 (الخيار B)

        // Backing Fields: EF يقرأ ويكتب في _comments و _history مباشرة
        builder.HasMany(t => t.Comments)
            .WithOne()
            .HasForeignKey(c => c.TicketId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(t => t.Comments)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(t => t.History)
            .WithOne()
            .HasForeignKey(h => h.TicketId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(t => t.History)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Indexes
        builder.HasIndex(t => t.CustomerId);                       // تذاكر عميل
        builder.HasIndex(t => t.AgentId);                          // تذاكر agent
        builder.HasIndex(t => new { t.Status, t.ResolutionDeadline }); // البحث عن المتأخرة (المرحلة 7)
    }
}