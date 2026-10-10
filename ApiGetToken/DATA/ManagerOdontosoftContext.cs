using System;
using System.Collections.Generic;
using ApiGetToken.Models;
using Microsoft.EntityFrameworkCore;

namespace ApiGetToken.DATA;

public partial class ManagerOdontosoftContext : DbContext
{
    public ManagerOdontosoftContext()
    {
    }

    public ManagerOdontosoftContext(DbContextOptions<ManagerOdontosoftContext> options)
        : base(options)
    {
    }

    public virtual DbSet<User> Users { get; set; }
    public virtual DbSet<Permiso> Permisos { get; set; }
    public virtual DbSet<Rol> Roles { get; set; }
    public virtual DbSet<RolPermiso> RolesPermisos { get; set; }
    public virtual DbSet<UsuarioRol> UsuariosRoles { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_dbo.User");

            entity.ToTable("User");

            entity.Property(e => e.ActivatedDate).HasColumnType("datetime");
            entity.Property(e => e.CreatedDate).HasColumnType("datetime");
            entity.Property(e => e.Email).HasMaxLength(120);
            entity.Property(e => e.LastName).HasMaxLength(100);
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.Password).HasMaxLength(60);
            entity.Property(e => e.PasswordExpirationDate).HasColumnType("datetime");
            entity.Property(e => e.UpdatedDate).HasColumnType("datetime");
            entity.Property(e => e.Username).HasMaxLength(50);
        });

        modelBuilder.Entity<Permiso>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_Permiso");

            entity.ToTable("Permiso");

            entity.Property(e => e.Codigo).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Modulo).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Nombre).HasMaxLength(150).IsRequired();
            entity.Property(e => e.Descripcion).HasMaxLength(500);
            entity.Property(e => e.CreatedDate).HasColumnType("datetime");
            entity.Property(e => e.UpdatedDate).HasColumnType("datetime");

            entity.HasIndex(e => e.Codigo).IsUnique().HasDatabaseName("UX_Permiso_Codigo");
            entity.HasIndex(e => e.Modulo).HasDatabaseName("IX_Permiso_Modulo");
        });

        modelBuilder.Entity<Rol>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_Rol");

            entity.ToTable("Rol");

            entity.Property(e => e.Codigo).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Nombre).HasMaxLength(150).IsRequired();
            entity.Property(e => e.Descripcion).HasMaxLength(500);
            entity.Property(e => e.CreatedDate).HasColumnType("datetime");
            entity.Property(e => e.UpdatedDate).HasColumnType("datetime");

            entity.HasIndex(e => e.Codigo).IsUnique().HasDatabaseName("UX_Rol_Codigo");
        });

        modelBuilder.Entity<RolPermiso>(entity =>
        {
            entity.HasKey(e => new { e.RolId, e.PermisoId }).HasName("PK_RolPermiso");

            entity.ToTable("RolPermiso");

            entity.Property(e => e.CreatedDate).HasColumnType("datetime");

            entity.HasOne(d => d.Rol)
                .WithMany(p => p.RolesPermisos)
                .HasForeignKey(d => d.RolId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_RolPermiso_Rol_RolId");

            entity.HasOne(d => d.Permiso)
                .WithMany(p => p.RolesPermisos)
                .HasForeignKey(d => d.PermisoId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_RolPermiso_Permiso_PermisoId");

            entity.HasIndex(e => new { e.RolId, e.PermisoId }).IsUnique().HasDatabaseName("UX_RolPermiso");
        });

        modelBuilder.Entity<UsuarioRol>(entity =>
        {
            entity.HasKey(e => new { e.UsuarioId, e.RolId }).HasName("PK_UsuarioRol");

            entity.ToTable("UsuarioRol");

            entity.Property(e => e.CreatedDate).HasColumnType("datetime");

            entity.HasOne(d => d.Usuario)
                .WithMany(p => p.UsuariosRoles)
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_UsuarioRol_User_UsuarioId");

            entity.HasOne(d => d.Rol)
                .WithMany(p => p.UsuariosRoles)
                .HasForeignKey(d => d.RolId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_UsuarioRol_Rol_RolId");

            entity.HasIndex(e => new { e.UsuarioId, e.RolId }).IsUnique().HasDatabaseName("UX_UsuarioRol");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
