using System;
using System.Collections.ObjectModel;
using System.Data.Entity;
using System.Data.Entity.Core;
using System.Data.Entity.Infrastructure;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Transactions.Models;

namespace Transactions.ViewModels {
    public class LiteratureTypesViewModel : BaseViewModel {
        public ObservableCollection<LiteratureType> LiteratureTypes { get; set; }

        public LiteratureTypesViewModel() {
            using (var context = new BookOrdersContext()) {
                LiteratureTypes = new ObservableCollection<LiteratureType>(context.LiteratureTypes);
            }

            foreach (LiteratureType literatureType in LiteratureTypes) {
                literatureType.InitializeValidator(LiteratureTypes);
            }

            DataGridAddingNewItemCommand = new DelegateCommand<AddingNewItemEventArgs>(DataGridAddingNewItem);

            PreviewKeyDownCommand = new DelegateCommand<object>(PreviewKeyDown);

            DataGridRowEditEndingCommand = new DelegateCommand<object>(DataGridRowEditEnding);

            BookOrdersContext.DatabaseChanged += RefreshPage;
        }

        public ICommand DataGridAddingNewItemCommand { get; set; }

        public ICommand PreviewKeyDownCommand { get; set; }

        public ICommand DataGridRowEditEndingCommand { get; set; }

        private void DataGridAddingNewItem(AddingNewItemEventArgs e) {
            e.NewItem = new LiteratureType();
            ((LiteratureType) e.NewItem).InitializeValidator(LiteratureTypes);
        }

        private static void PreviewKeyDown(object eventArgs) {
            var tempEventArgs = (object[]) eventArgs;
            if (!(tempEventArgs[0] is KeyEventArgs e && tempEventArgs[1] is DataGrid sender)) {
                throw new ArgumentException("Bad arguments in PreviewKeyDown");
            }

            switch (e.Key) {
                case Key.Escape:
                    sender.CancelEdit();
                    sender.CancelEdit();
                    return;
                case Key.Delete:
                    if (!(sender.SelectedItem is LiteratureType literatureType)) return;
                    if (!BookOrdersContext.IsTransactionOpen) {
                        MessageBox.Show("There is no open transaction!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        e.Handled = true;
                        return;
                    }

                    BookOrdersContext context = BookOrdersContext.Instance;
                    try {
                        context.LiteratureTypes.Attach(literatureType);
                        context.LiteratureTypes.Remove(literatureType);
                        context.SaveChanges();
                    } catch (DbUpdateException exception) {
                        if (!(exception.InnerException is UpdateException innerException)) throw;
                        if (!(innerException.InnerException is Npgsql.PostgresException innerInnerException))
                            throw;
                        if (innerInnerException.SqlState == "23503") {
                            MessageBox.Show("There are some genres that link to this literature type. Delete them first.",
                                            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            e.Handled = true;
                        } else {
                            throw;
                        }
                    }

                    return;
            }
        }

        private static void DataGridRowEditEnding(object eventArgs) {
            var tempEventArgs = (object[]) eventArgs;
            if (!(tempEventArgs[0] is DataGridRowEditEndingEventArgs e && tempEventArgs[1] is DataGrid sender)) {
                throw new ArgumentException("Bad arguments in DataGridRowEditEnding");
            }

            if (e.EditAction == DataGridEditAction.Cancel || !(e.Row.Item is LiteratureType literatureType) ||
                !literatureType.Validator.IsValid ||
                !literatureType.IsDirty)
                return;

            if (!BookOrdersContext.IsTransactionOpen) {
                MessageBox.Show("There is no open transaction!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                sender.CancelEdit();
                e.Cancel = true;
                return;
            }

            BookOrdersContext context = BookOrdersContext.Instance;
            if (e.Row.IsNewItem) {
                context.LiteratureTypes.Add(literatureType);
            } else {
                context.Entry(literatureType).State = EntityState.Modified;
            }

            try {
                context.SaveChanges();
                context.Entry(literatureType).State = EntityState.Detached;
            } catch (DbUpdateException exception) {
                if (!(exception.InnerException is UpdateException innerException)) throw;
                if (!(innerException.InnerException is Npgsql.PostgresException innerInnerException)) throw;
                if (innerInnerException.SqlState == "40001") {
                    MessageBox.Show(innerInnerException.Message,
                                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    sender.CancelEdit();
                    e.Cancel = true;
                    BookOrdersContext.IsCorrupted = true;
                } else {
                    throw;
                }
            }
        }

        private void RefreshPage() {
            BookOrdersContext context = BookOrdersContext.Instance;
            foreach (DbEntityEntry entity in context.ChangeTracker.Entries()) {
                entity.Reload();
            }
            LiteratureTypes = new ObservableCollection<LiteratureType>(context.LiteratureTypes);

            foreach (LiteratureType literatureType in LiteratureTypes) {
                literatureType.InitializeValidator(LiteratureTypes);
            }
        }
    }
}