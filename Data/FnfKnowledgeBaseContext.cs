using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Project_Version1.Data;

public partial class FnfKnowledgeBaseContext : DbContext
{
    public FnfKnowledgeBaseContext()
    {
    }

    public FnfKnowledgeBaseContext(DbContextOptions<FnfKnowledgeBaseContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Attachment> Attachments { get; set; }

    public virtual DbSet<Comment> Comments { get; set; }

    public virtual DbSet<Commit> Commits { get; set; }

    public virtual DbSet<Department> Departments { get; set; }

    public virtual DbSet<Manager> Managers { get; set; }

    public virtual DbSet<Post> Posts { get; set; }

    public virtual DbSet<PostTag> PostTags { get; set; }

    public virtual DbSet<Repost> Reposts { get; set; }

    public virtual DbSet<Tag> Tags { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<Vote> Votes { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=.\\SQLEXPRESS;Database=FNF_KnowledgeBase;Trusted_Connection=True;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Attachment>(entity =>
        {
            entity.HasKey(e => e.AttachmentId).HasName("PK__Attachme__442C64BE7F6BA044");

            entity.Property(e => e.FileName).HasMaxLength(200);
            entity.Property(e => e.FilePath).HasMaxLength(500);
            entity.Property(e => e.FileType).HasMaxLength(20);
            entity.Property(e => e.UploadedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Comment).WithMany(p => p.Attachments)
                .HasForeignKey(d => d.CommentId)
                .HasConstraintName("FK__Attachmen__Comme__75A278F5");

            entity.HasOne(d => d.Post).WithMany(p => p.Attachments)
                .HasForeignKey(d => d.PostId)
                .HasConstraintName("FK__Attachmen__PostI__74AE54BC");
        });

        modelBuilder.Entity<Comment>(entity =>
        {
            entity.HasKey(e => e.CommentId).HasName("PK__Comments__C3B4DFCAC0191789");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

            entity.HasOne(d => d.ParentComment).WithMany(p => p.InverseParentComment)
                .HasForeignKey(d => d.ParentCommentId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__Comments__Parent__6477ECF3");

            entity.HasOne(d => d.Post).WithMany(p => p.Comments)
                .HasForeignKey(d => d.PostId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__Comments__PostId__628FA481");

            entity.HasOne(d => d.User).WithMany(p => p.Comments)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__Comments__UserId__6383C8BA");
        });

        modelBuilder.Entity<Commit>(entity =>
        {
            entity.HasKey(e => e.CommitId).HasName("PK__Commits__73748B723E9CC70B");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Manager).WithMany(p => p.Commits)
                .HasForeignKey(d => d.ManagerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Commits__Manager__7F2BE32F");

            // Changed to SetNull so deleting a Post sets Commits.PostId = NULL (keeps commits as audit)
            modelBuilder.Entity<Commit>()
                .HasOne(d => d.Post)
                .WithMany(p => p.Commits)
                .HasForeignKey(d => d.PostId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK__Commits__PostId__7E37BEF6");
        });

        modelBuilder.Entity<Department>(entity =>
        {
            entity.HasKey(e => e.DeptId).HasName("PK__Departme__014881AE3B5D8EBA");

            entity.HasIndex(e => e.DeptName, "UQ__Departme__5E508265B18B703C").IsUnique();

            entity.Property(e => e.DeptName).HasMaxLength(200);
        });

        modelBuilder.Entity<Manager>(entity =>
        {
            entity.HasKey(e => e.ManagerId).HasName("PK__Managers__3BA2AAE1B438647C");

            entity.HasIndex(e => e.UserId, "UQ__Managers__1788CC4D04D01832").IsUnique();

            entity.HasOne(d => d.Dept).WithMany(p => p.Managers)
                .HasForeignKey(d => d.DeptId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Managers__DeptId__6FE99F9F");

            entity.HasOne(d => d.User).WithOne(p => p.Manager)
                .HasForeignKey<Manager>(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Managers__UserId__6EF57B66");
        });

        modelBuilder.Entity<Post>(entity =>
        {
            entity.HasKey(e => e.PostId).HasName("PK__Posts__AA1260186D489321");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Title).HasMaxLength(500);
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

            entity.HasOne(d => d.Dept).WithMany(p => p.Posts)
                .HasForeignKey(d => d.DeptId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Posts__DeptId__5AEE82B9");

            entity.HasOne(d => d.User).WithMany(p => p.Posts)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Posts__UserId__59FA5E80");
        });

        modelBuilder.Entity<PostTag>(entity =>
        {
            entity.HasKey(e => e.PostTagId).HasName("PK__PostTags__325724FD54FB2080");

            entity.HasOne(d => d.Post).WithMany(p => p.PostTags)
                .HasForeignKey(d => d.PostId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__PostTags__PostId__5DCAEF64");

            entity.HasOne(d => d.Tag).WithMany(p => p.PostTags)
                .HasForeignKey(d => d.TagId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__PostTags__TagId__5EBF139D");
        });

        modelBuilder.Entity<Repost>(entity =>
        {
            entity.HasKey(e => e.RepostId).HasName("PK__Reposts__5E7F921EF22863DD");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Post).WithMany(p => p.Reposts)
                .HasForeignKey(d => d.PostId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Reposts__PostId__797309D9");

            entity.HasOne(d => d.User).WithMany(p => p.Reposts)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Reposts__UserId__7A672E12");
        });

        modelBuilder.Entity<Tag>(entity =>
        {
            entity.HasKey(e => e.TagId).HasName("PK__Tags__657CF9AC12C6A582");

            entity.HasIndex(e => e.TagName, "UQ__Tags__BDE0FD1D98DD81BD").IsUnique();

            entity.Property(e => e.TagName).HasMaxLength(200);

            entity.HasOne(d => d.Dept).WithMany(p => p.Tags)
                .HasForeignKey(d => d.DeptId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Tags__DeptId__534D60F1");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Users__1788CC4C7501CCAC");

            entity.HasIndex(e => e.Email, "UQ__Users__A9D10534CB51E588").IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Email).HasMaxLength(200);
            entity.Property(e => e.FullName).HasMaxLength(200);
            entity.Property(e => e.PasswordHash).HasMaxLength(500);
            entity.Property(e => e.ProfilePicture).HasMaxLength(500);
            entity.Property(e => e.Role).HasMaxLength(20);

            entity.HasOne(d => d.Department).WithMany(p => p.Users)
                .HasForeignKey(d => d.DepartmentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Users__Departmen__4F7CD00D");
        });

        modelBuilder.Entity<Vote>(entity =>
        {
            entity.HasKey(e => e.VoteId).HasName("PK__Votes__52F015C2708B1D4E");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.VoteType).HasMaxLength(20);

            entity.HasOne(d => d.Comment).WithMany(p => p.Votes)
                .HasForeignKey(d => d.CommentId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__Votes__CommentId__6A30C649");

            entity.HasOne(d => d.Post).WithMany(p => p.Votes)
                .HasForeignKey(d => d.PostId)
                .OnDelete(DeleteBehavior.Cascade)

                .HasConstraintName("FK__Votes__PostId__693CA210");

            entity.HasOne(d => d.User).WithMany(p => p.Votes)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__Votes__UserId__6B24EA82");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
