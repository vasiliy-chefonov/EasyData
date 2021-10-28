using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;

namespace EasyData.Services
{
    /// <summary>
    /// Define methods to validate entity instance.
    /// </summary>
    public class EntityValidationService<T>
    {
        private readonly T _entity;
        private readonly IServiceProvider _serviceProvider;
        private readonly List<string> _exceptionMessages = new List<string>();

        /// <summary>
        /// Create new validation service instance.
        /// </summary>
        /// <param name="entity">Entity instance</param>
        /// <param name="serviceProvider">Service provider.</param>
        public EntityValidationService(T entity, IServiceProvider serviceProvider)
        {
            _entity = entity;
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Validate entity instance.
        /// </summary>
        /// <param name="exceptionMessages">Collection to receive failure messages.</param>
        /// <param name="validator">Optional <see cref="Validator{T}" /> instance.</param>
        /// <returns>If there are failures while validating.</returns>
        public bool TryValidate(out IEnumerable<string> exceptionMessages, Validator<T> validator = null)
        {
            try {
                validator?.Validate(_entity);
            }
            catch (Exception exception) {
                _exceptionMessages.Add(exception.Message);
            }

            // Validate IValidatable object
            var validationContext = new ValidationContext(_entity, _serviceProvider, items: null);
            ValidateIValidatableObject(validationContext);
            ValidateWithEntityValidatorAnnotations(validationContext);

            exceptionMessages = _exceptionMessages;
            return !exceptionMessages.Any();
        }

        /// <summary>
        /// Validate IValidatable instance.
        /// </summary>
        /// <param name="validationContext">Validation context.</param>
        private void ValidateIValidatableObject(ValidationContext validationContext)
        {
            if (!(_entity is IValidatableObject validatableObject)) {
                return;
            }

            var validationErrors = validatableObject.Validate(validationContext)
                .Where(vr => vr != ValidationResult.Success)
                .ToList();

            if (!validationErrors.Any()) {
                return;
            }

            foreach (var error in validationErrors) {
                _exceptionMessages.Add(error.ErrorMessage);
            }
        }

        /// <summary>
        /// Validate with <see cref="Validator"/> annotations.
        /// </summary>
        /// <param name="validationContext">Validation context.</param>
        private void ValidateWithEntityValidatorAnnotations( ValidationContext validationContext)
        {
            var validationErrors = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(_entity, validationContext, validationErrors, true);

            if (isValid) {
                return;
            }

            foreach (var error in validationErrors) {
                _exceptionMessages.Add(error.ErrorMessage);
            }
        }
    }
}
