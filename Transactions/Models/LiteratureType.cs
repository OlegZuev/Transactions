using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using PropertyChanged;
using ReactiveValidation;
using ReactiveValidation.Extensions;

namespace Transactions.Models {
    [AddINotifyPropertyChangedInterface]
    [Table("public.literature_types")]
    public class LiteratureType : ValidatableObject, IEditableObject {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage",
            "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public LiteratureType() {
            Genres = new HashSet<Genre>();
        }

        [Column("id")] public int Id { get; set; }

        [Required]
        [StringLength(8000)]
        [Column("name")]
        public string Name { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage",
            "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Genre> Genres { get; set; }

        public bool IsDirty => _backupCopy == null || Name != _backupCopy.Name;

        private LiteratureType _backupCopy;

        private bool _inEdit;

        public void BeginEdit() {
            if (_inEdit) return;
            _inEdit = true;
            _backupCopy = MemberwiseClone() as LiteratureType;
        }

        public void CancelEdit() {
            if (!_inEdit) return;
            _inEdit = false;
            Id = _backupCopy.Id;
            Name = _backupCopy.Name;
        }

        public void EndEdit() {
            if (!_inEdit) return;
            _inEdit = false;
            _backupCopy = null;
        }

        public void InitializeValidator(IList<LiteratureType> genres = null) {
            Validator = GetValidator(genres);
        }

        private IObjectValidator GetValidator(IList<LiteratureType> literatureTypes = null) {
            var builder = new ValidationBuilder<LiteratureType>();

            builder.RuleFor(literatureType => literatureType.Name)
                   .NotEmpty()
                   .WithMessage("Name can't be empty");
            builder.RuleFor(literatureType => literatureType.Name)
                   .Must(name => literatureTypes == null ||
                                 !literatureTypes.Any(literatureType =>
                                                          literatureType.Id != Id && literatureType.Name == name))
                   .WithMessage("Name should be unique");

            return builder.Build(this);
        }
    }
}