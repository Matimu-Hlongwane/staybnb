
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Staybnb.Models;

namespace Staybnb.Data;

public class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext(options)
{
    public DbSet<HostApplication> HostApplications =>
        Set<HostApplication>();

    public DbSet<PropertyImage> PropertyImages =>
        Set<PropertyImage>();

    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<StaybnbMessage> Messages => Set<StaybnbMessage>();
    public DbSet<StaybnbNotification> Notifications => Set<StaybnbNotification>();
    public DbSet<StaybnbActivityLog> ActivityLogs => Set<StaybnbActivityLog>();
    public DbSet<CheckInRecord> CheckInRecords => Set<CheckInRecord>();


    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<HostApplication>()
            .HasOne(application => application.User)
            .WithMany()
            .HasForeignKey(application => application.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<PropertyImage>()
            .HasOne(image => image.HostApplication)
            .WithMany(application => application.Images)
            .HasForeignKey(image => image.HostApplicationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<HostApplication>()
            .Property(application => application.PricePerNight)
            .HasPrecision(18, 2);

        builder.Entity<Booking>()
            .HasOne(booking => booking.Property)
            .WithMany()
            .HasForeignKey(booking => booking.HostApplicationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Booking>()
            .HasOne(booking => booking.GuestUser)
            .WithMany()
            .HasForeignKey(booking => booking.GuestUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Booking>()
            .Property(booking => booking.TotalPrice)
            .HasPrecision(18, 2);

        builder.Entity<CheckInRecord>()
            .HasOne(record => record.Booking)
            .WithOne()
            .HasForeignKey<CheckInRecord>(record => record.BookingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<CheckInRecord>()
            .HasIndex(record => record.BookingId)
            .IsUnique();
    }
}
