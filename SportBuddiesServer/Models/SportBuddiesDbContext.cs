// SportBuddiesDbContext.cs - Fixed version
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace SportBuddiesServer.Models;

public partial class SportBuddiesDbContext : DbContext
{
    public SportBuddiesDbContext()
    {
    }

    public SportBuddiesDbContext(DbContextOptions<SportBuddiesDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<GameDetail> GameDetails { get; set; }
    public virtual DbSet<GameRole> GameRoles { get; set; }
    public virtual DbSet<GameType> GameTypes { get; set; }
    public virtual DbSet<GameUser> GameUsers { get; set; }
    public virtual DbSet<Photo> Photos { get; set; }
    public virtual DbSet<User> Users { get; set; }

    // Add the new chat-related DbSets
    public virtual DbSet<GameChat> GameChats { get; set; }
    public virtual DbSet<ChatMessage> ChatMessages { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server = (localdb)\\MSSQLLocalDB;Initial Catalog=SportBuddiesDB;User ID=SportBuddiesAdminLogin;Password=thePassword;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Chat-related entities
        modelBuilder.Entity<GameChat>(entity =>
        {
            entity.HasKey(e => e.ChatId).HasName("PK__GameChat__A22F0CC07F3E1234");
            entity.HasOne(d => d.Game).WithMany(p => p.GameChats).HasConstraintName("FK__GameChat__GameID__123456789");
        });

        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.HasKey(e => e.MessageId).HasName("PK__ChatMess__C87C037C1234ABCD");
            entity.HasOne(d => d.Chat).WithMany(p => p.ChatMessages).HasConstraintName("FK__ChatMess__ChatID__987654321");
            entity.HasOne(d => d.Sender).WithMany(p => p.ChatMessages).HasConstraintName("FK__ChatMess__Sender__567891234");
        });

        // Existing entities
        modelBuilder.Entity<GameDetail>(entity =>
        {
            entity.HasKey(e => e.GameId).HasName("PK__GameDeta__2AB897DD98FE9E60");
            entity.HasOne(d => d.Creator).WithMany(p => p.GameDetails).HasConstraintName("FK__GameDetai__Creat__2B3F6F97");
            entity.HasOne(d => d.GameTypeNavigation).WithMany(p => p.GameDetails).HasConstraintName("FK__GameDetai__GameT__2C3393D0");
        });

        modelBuilder.Entity<GameRole>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PK__GameRole__8AFACE3ACB6444B4");
            entity.HasOne(d => d.GameType).WithMany(p => p.GameRoles).HasConstraintName("FK__GameRoles__GameT__35BCFE0A");
        });

        modelBuilder.Entity<GameType>(entity =>
        {
            entity.HasKey(e => e.IdType).HasName("PK__GameType__9A39EABCB8086F57");
        });

        modelBuilder.Entity<GameUser>(entity =>
        {
            entity.HasKey(e => new { e.GameId, e.RoleId, e.UserId }).HasName("PK__GameUser__DF00B3D038BE90C9");
            entity.HasOne(d => d.Game).WithMany(p => p.GameUsers)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__GameUsers__GameI__38996AB5");
            entity.HasOne(d => d.Role).WithMany(p => p.GameUsers)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__GameUsers__RoleI__398D8EEE");
            entity.HasOne(d => d.User).WithMany(p => p.GameUsers)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__GameUsers__UserI__3A81B327");
        });


        modelBuilder.Entity<Photo>(entity =>
        {
            entity.HasKey(e => e.PhotoId).HasName("PK__Photo__21B7B582F3B04B2B");
            entity.HasOne(d => d.Game).WithMany(p => p.Photos).HasConstraintName("FK__Photo__GameID__2F10007B");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__User__1788CCAC2D7E06EA");
            entity.HasOne(d => d.FavoriteSportNavigation).WithMany(p => p.Users).HasConstraintName("FK__User__FavoriteSp__276EDEB3");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}