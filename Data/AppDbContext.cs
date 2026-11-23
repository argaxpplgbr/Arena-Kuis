
using ArenaKuis.API.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ArenaKuis.API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    public DbSet<Question> Questions => Set<Question>();

    public DbSet<Quiz> Quizzes => Set<Quiz>();

    public DbSet<Classroom> Classrooms => Set<Classroom>();

    public DbSet<QuizAttempt> QuizAttempts => Set<QuizAttempt>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .HasIndex(user => user.Username)
            .IsUnique();

        modelBuilder.Entity<Quiz>()
            .Property(quiz => quiz.QuestionIds)
            .HasConversion(
                ids => JsonSerializer.Serialize(ids, (JsonSerializerOptions?)null),
                json => JsonSerializer.Deserialize<List<int>>(
                    json,
                    (JsonSerializerOptions?)null
                ) ?? new List<int>()
            );

        modelBuilder.Entity<Classroom>()
            .HasIndex(classroom => classroom.ClassCode)
            .IsUnique();

        modelBuilder.Entity<Classroom>()
            .Property(classroom => classroom.StudentsIds)
            .HasConversion(
                ids => JsonSerializer.Serialize(ids, (JsonSerializerOptions?)null),
                json => JsonSerializer.Deserialize<List<int>>(
                    json,
                    (JsonSerializerOptions?)null
                ) ?? new List<int>()
            );
    }
}
