using Microsoft.EntityFrameworkCore;
using NotificationsAPI.Domain.Entities;
using NotificationsAPI.Domain.Enums;

namespace NotificationsAPI.Data;

public class NotificationDbContext : DbContext
{
    public NotificationDbContext(DbContextOptions<NotificationDbContext> options) : base(options)
    {
    }

    public DbSet<Notification> Notifications { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.ToTable("Notifications");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .IsRequired()
                .ValueGeneratedNever();

            entity.Property(e => e.UsuarioId)
                .IsRequired();

            entity.Property(e => e.Tipo)
                .IsRequired()
                .HasConversion<int>();

            entity.Property(e => e.Destinatario)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(e => e.Assunto)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(e => e.Corpo)
                .IsRequired();

            entity.Property(e => e.Status)
                .IsRequired()
                .HasConversion<int>()
                .HasDefaultValue(StatusNotificacao.Pendente);

            entity.Property(e => e.DataCriacao)
                .IsRequired();

            entity.Property(e => e.DataEnvio)
                .IsRequired(false);

            entity.Property(e => e.TentativasEnvio)
                .IsRequired()
                .HasDefaultValue(0);

            entity.Property(e => e.ErroMensagem)
                .HasMaxLength(2000)
                .IsRequired(false);

            // Índices
            entity.HasIndex(e => e.UsuarioId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.DataCriacao);
        });
    }
}