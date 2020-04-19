using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Core;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using Npgsql;
using Transactions.Models;

namespace Transactions.ViewModels {
    public class MainWindowViewModel : BaseViewModel {
        public ObservableCollection<string> IsolationLevels { get; set; }

        public string SavePointName { get; set; }

        public string CurrentSavePoint { get; set; }

        public string CurrentIsolationLevel { get; set; }

        public MainWindowViewModel() {
            IsolationLevels = new ObservableCollection<string> {
                "Read Committed",
                "Repeatable Read",
                "Serializable"
            };
            CurrentIsolationLevel = IsolationLevels[0];
            CurrentSavePoint = BookOrdersContext.SavePoints[0];

            BeginTransactionCommand =
                new DelegateCommand<object>(BeginTransaction, sender => !BookOrdersContext.IsTransactionOpen);

            CommitCommand = new DelegateCommand<object>(Commit, sender => BookOrdersContext.IsTransactionOpen);

            RefreshCommand = new DelegateCommand<object>(BookOrdersContext.Refresh,
                                                         sender => BookOrdersContext.IsTransactionOpen &&
                                                                   !BookOrdersContext.IsCorrupted);

            AddNewGenreWithAvgPopularityMinus10Command = new DelegateCommand<object>(
                AddNewGenreWithAvgPopularityMinus10,
                sender => BookOrdersContext.IsTransactionOpen && !BookOrdersContext.IsCorrupted);

            SetSavePointCommand = new DelegateCommand<object>(SetSavePoint,
                                                              sender => BookOrdersContext.IsTransactionOpen &&
                                                                        !BookOrdersContext.IsCorrupted);

            RollbackCommand = new DelegateCommand<object>(RollbackTo, sender => BookOrdersContext.IsTransactionOpen);
        }

        public ICommand BeginTransactionCommand { get; set; }

        public ICommand CommitCommand { get; set; }

        public ICommand RefreshCommand { get; set; }

        public ICommand AddNewGenreWithAvgPopularityMinus10Command { get; set; }

        public ICommand SetSavePointCommand { get; set; }

        public ICommand RollbackCommand { get; set; }

        private void BeginTransaction(object sender) {
            BookOrdersContext.Instance = new BookOrdersContext();
            BookOrdersContext context = BookOrdersContext.Instance;
            context.Database.Log = s => System.Diagnostics.Debug.Write(s);
            switch (CurrentIsolationLevel) {
                case "Read Committed":
                    context.Database.BeginTransaction(IsolationLevel.ReadCommitted);
                    break;
                case "Repeatable Read":
                    context.Database.BeginTransaction(IsolationLevel.RepeatableRead);
                    break;
                case "Serializable":
                    context.Database.BeginTransaction(IsolationLevel.Serializable);
                    break;
            }

            if (BookOrdersContext.IsTransactionOpen && !(sender is bool)) {
                MessageBox.Show("Transaction is open", "Notification", MessageBoxButton.OK,
                                MessageBoxImage.Information);
            }
        }

        private static void Commit(object sender) {
            try {
                BookOrdersContext.Instance.Database.CurrentTransaction.Commit();
            } catch (CommitFailedException exception) {
                if (!(exception.InnerException is Npgsql.PostgresException innerException)) throw;
                if (innerException.SqlState == "40001") {
                    MessageBox.Show(innerException.Message,
                                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                } else {
                    throw;
                }
            }
            BookOrdersContext.Instance.Dispose();
            BookOrdersContext.Instance = null;
            BookOrdersContext.IsCorrupted = false;
            for (int i = BookOrdersContext.SavePoints.Count - 1; i >= 1; i--) {
                BookOrdersContext.SavePoints.RemoveAt(i);
            }
        }

        private static void AddNewGenreWithAvgPopularityMinus10(object sender) {
            BookOrdersContext context = BookOrdersContext.Instance;
            try {
                context.Database.ExecuteSqlCommand(
                    "WITH avg_popularity AS (SELECT ceil(avg(popularity)) AS avg_value FROM genres) " +
                    $"INSERT INTO genres (name, popularity, literature_type_id) SELECT 'Жанр{context.Genres.ToList().Count + 1}', avg_value - 10, 1 FROM avg_popularity;");
            } catch (Npgsql.PostgresException exception) {
                if (exception.SqlState == "40001") {
                    MessageBox.Show(exception.Message,
                                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    BookOrdersContext.IsCorrupted = true;
                } else {
                    throw;
                }
            }

            context.SaveChanges();
        }

        private void SetSavePoint(object sender) {
            if (Regex.IsMatch(SavePointName, @"^[\w\d]+$")) {
                BookOrdersContext.Instance.Database.ExecuteSqlCommand($"SAVEPOINT \"{SavePointName}\";");
                BookOrdersContext.SavePoints.Add(SavePointName);
            }
            SavePointName = "";
        }

        private void RollbackTo(object sender) {
            if (CurrentSavePoint == "None") {
                BookOrdersContext.Instance.Database.ExecuteSqlCommand("ROLLBACK");
                BookOrdersContext.Instance.Dispose();
                BookOrdersContext.Instance = null;
                BookOrdersContext.IsCorrupted = false;
                BeginTransaction(true);
                BookOrdersContext.Refresh(null);
                Commit(null);
                return;
            }
            BookOrdersContext.Instance.Database.ExecuteSqlCommand($"ROLLBACK TO \"{CurrentSavePoint}\";");
            BookOrdersContext.IsCorrupted = false;
            for (int i = BookOrdersContext.SavePoints.Count - 1; i >= 0; i--) {
                if (BookOrdersContext.SavePoints[i] != CurrentSavePoint) {
                    BookOrdersContext.SavePoints.RemoveAt(i);
                } else {
                    BookOrdersContext.SavePoints.RemoveAt(i);
                    break;
                }
            }

            BookOrdersContext.Refresh(null);
        }
    }
}