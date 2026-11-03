using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using QuizMasterDomain.Identity;
using Microsoft.EntityFrameworkCore;
using QuizMasterDomain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace QuizMasterInfrastructure.Data
{
    public class QuizMasterDbContext : IdentityDbContext<ApplicationUser>
    {
        public QuizMasterDbContext(DbContextOptions<QuizMasterDbContext> options) : base(options)
        {
        }
        public DbSet<Quiz> Quizzes { get; set; } = null!;
        public DbSet<Question> Questions { get; set; } = null!;
        public DbSet<AnswerOption> AnswerOptions { get; set; } = null!;
        public DbSet<QuizAttempt> QuizAttempts { get; set; } = null!;
        public DbSet<StudentAnswer> StudentAnswers { get; set; } = null!;
        public DbSet<QuizResult> QuizResults { get; set; } = null!;
    
        protected override void OnModelCreating (ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
      

            // Quiz and Questions
            modelBuilder.Entity<Quiz>()
                .HasMany(quiz => quiz.Questions)
                .WithOne(question => question.Quiz)
                .HasForeignKey(question => question.QuizId)
                .OnDelete(DeleteBehavior.Cascade);

            // Question and AnswerOptions
            modelBuilder.Entity<Question>()
                .HasMany(question => question.AnswerOptions)
                .WithOne(a => a.Question)
                .HasForeignKey(a => a.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Question and StudentAnswers
            modelBuilder.Entity<Question>()
                .HasMany(question => question.StudentAnswers)
                .WithOne(sa => sa.Question)
                .HasForeignKey(sa => sa.QuestionId)
                .OnDelete(DeleteBehavior.NoAction);

            // StudentAnswer and QuizAttempt
            modelBuilder.Entity<StudentAnswer>()
                .HasOne(sa => sa.QuizAttempt)
                .WithMany(qa => qa.StudentAnswers)
                .HasForeignKey(sa => sa.QuizAttemptId)
                .OnDelete(DeleteBehavior.Cascade);

            // StudentAnswer and AnswerOption
            modelBuilder.Entity<StudentAnswer>()
                .HasOne(sa => sa.AnswerOption)
                .WithMany(ao => ao.StudentAnswers)
                .HasForeignKey(sa => sa.AnswerOptionId)
                .OnDelete(DeleteBehavior.NoAction);

            // QuizAttempt and QuizResult 
            modelBuilder.Entity<QuizAttempt>()
                .HasOne(qa => qa.QuizResult)
                .WithOne(qr => qr.QuizAttempt)
                .HasForeignKey<QuizResult>(qr => qr.QuizAttemptId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<QuizAttempt>()
            .HasOne(qa => qa.User)
            .WithMany()
            .HasForeignKey(qa => qa.UserId);

            modelBuilder.Entity<QuizAttempt>() 
            .HasOne(attempt => attempt.Quiz)
            .WithMany()
            .HasForeignKey(attempt => attempt.QuizId)
            .OnDelete(DeleteBehavior.NoAction);

            // ვალიდაციები:

            modelBuilder.Entity<Quiz>(entity =>
            {
                entity.Property(q => q.Title).IsRequired().HasMaxLength(100);
                entity.ToTable(table => table.HasCheckConstraint("CK_Quiz_TimeLimitMinutes_Positive", "[TimeLimitMinutes] > 0"));
            });
            modelBuilder.Entity<Question>(entity =>
            {
                entity.Property(q => q.Text).IsRequired().HasMaxLength(1000);
                entity.ToTable(table => table.HasCheckConstraint("CK_Question_Points_Positive", "[Points] > 0"));
            });
            modelBuilder.Entity<AnswerOption>(entity =>
            {
                entity.Property(a => a.Text).IsRequired().HasMaxLength(500);
            });
            modelBuilder.Entity<QuizAttempt>(entity =>
            {
                entity.ToTable(table => table.HasCheckConstraint("CK_QuizAttempts_FinishedAt_After_StartedAt","[FinishedAt] IS NULL OR [FinishedAt] >= [StartedAt]"));
            });
            modelBuilder.Entity<StudentAnswer>().HasIndex(sa => new { sa.QuestionId, sa.QuizAttemptId, sa.AnswerOptionId }).IsUnique();
            modelBuilder.Entity<QuizResult>(entity =>
            {
                entity.ToTable(table => table.HasCheckConstraint("CK_QuizResults_Score_Within_Maximum","[Score] >= 0 AND [MaxScore] >= 0 AND [Score] <= [MaxScore]"));
            });
            modelBuilder.Entity<ApplicationUser>().HasIndex(user => user.NormalizedEmail).IsUnique().HasFilter("[NormalizedEmail] IS NOT NULL");
        }
    }
}
