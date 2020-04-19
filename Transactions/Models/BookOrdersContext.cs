using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.Entity;
using Npgsql;
using PropertyChanged;

namespace Transactions.Models {
    [AddINotifyPropertyChangedInterface]
    public partial class BookOrdersContext : DbContext {
        public BookOrdersContext()
            : base("name=BookOrdersContext") {
        }

        public virtual DbSet<Genre> Genres { get; set; }
        public virtual DbSet<LiteratureType> LiteratureTypes { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder) {
            modelBuilder.Entity<LiteratureType>()
                .HasMany(e => e.Genres)
                .WithRequired(e => e.LiteratureType)
                .HasForeignKey(e => e.LiteratureTypeId)
                .WillCascadeOnDelete(false);
        }

        public static event Action DatabaseChanged;

        public static void Refresh(object sender) {
            DatabaseChanged?.Invoke();
        }

        public static BookOrdersContext Instance;

        public static bool IsCorrupted = false;

        public static ObservableCollection<string> SavePoints { get; set; } = new ObservableCollection<string> {"None"};

        public static bool IsTransactionOpen => Instance?.Database.CurrentTransaction != null;
    }
}
