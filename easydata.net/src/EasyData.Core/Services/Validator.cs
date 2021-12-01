using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using EasyData.Exceptions;

namespace EasyData.Services
{
    /// <summary>
    /// Entity validator abstract class.
    /// </summary>
    /// <typeparam name="T">Type of the entity to validate.</typeparam>
    public abstract class Validator<T>
    {
        /// <summary>
        /// Validate entity.
        /// Throw an exception if there is a failure while validating.
        /// </summary>
        /// <param name="instance">Instance to validate.</param>
        public abstract void Validate(T instance);
    }

    /// <summary>
    /// Default validator class that takes a validation action. 
    /// </summary>
    /// <typeparam name="T">Type of the entity to validate.</typeparam>
    public class PredefinedValidator<T> : Validator<T>
    {
        private readonly Action<T> _validate;

        /// <summary>
        /// Set validation action.
        /// </summary>
        /// <param name="validationAction">Action to validate instance.</param>
        public PredefinedValidator(Action<T> validationAction)
        {
            _validate = validationAction ?? throw new ArgumentNullException(nameof(validationAction));
        }

        /// <inheritdoc/>
        public override void Validate(T instance) => _validate(instance);
    }

    /// <summary>
    /// Service to validate entity instance.
    /// </summary>
    /// <typeparam name="T">Type of the entity to validate.</typeparam>
    public class EntityValidationService<T>
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly List<ValidationErrorInfo> _validationErrors = new List<ValidationErrorInfo>();
        private readonly Validator<T> _validator;

        /// <summary>
        /// Create new validation service instance.
        /// </summary>
        /// <param name="serviceProvider">Service provider.</param>
        /// <param name="validator">Optional <see cref="Validator{T}" /> instance.</param>
        public EntityValidationService(IServiceProvider serviceProvider, Validator<T> validator = null)
        {
            _serviceProvider = serviceProvider;
            _validator = validator;
        }

        /// <summary>
        /// Validate entity instance.
        /// </summary>
        /// <param name="instance">Entity instance to validate.</param>
        /// <param name="validationErrors">Collection to receive validation errors.</param>
        /// <returns>If there are failures during validation.</returns>
        public bool TryValidate(T instance, out IEnumerable<ValidationErrorInfo> validationErrors)
        {
            // Validate Validator object.
            try {
                _validator?.Validate(instance);
            }
            catch (Exception exception) {
                if (exception is AggregateException aggregateException) {
                    foreach (var innerException in aggregateException.InnerExceptions) {
                        _validationErrors.Add(new ValidationErrorInfo(innerException.Message));
                    }
                }
                else if (exception is EntityValidationException entityValidationException) {
                    _validationErrors.AddRange(entityValidationException.Errors);
                }
                else {
                    _validationErrors.Add(new ValidationErrorInfo(exception.Message));
                }
            }

            var validationContext = new ValidationContext(instance, _serviceProvider, items: null);
            ValidateWithValidator(instance, validationContext);

            validationErrors = _validationErrors;
            return !validationErrors.Any();
        }

        /// <summary>
        /// Check both <see cref="ValidationAttribute"/> attached to the entity and the Validate method.
        /// </summary>
        /// <param name="instance">Instance to validate.</param>
        /// <param name="context">Validation context.</param>
        private void ValidateWithValidator(T instance, ValidationContext context)
        {
            var validationResults = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(instance, context, validationResults, true);

            if (isValid) {
                return;
            }

            MergeErrorsWithValidationResults(validationResults.Where(result => result != ValidationResult.Success));
        }

        /// <summary>
        /// Get errors from validation results and put them to the storage.
        /// </summary>
        /// <param name="validationResults">Instance validation results.</param>
        private void MergeErrorsWithValidationResults(IEnumerable<ValidationResult> validationResults)
        {
            foreach (var validationResult in validationResults) {
                if (validationResult.MemberNames.Any()) {
                    foreach (var memberName in validationResult.MemberNames) {
                        _validationErrors.Add(new ValidationErrorInfo(validationResult.ErrorMessage, memberName));
                    }
                }
                else {
                    _validationErrors.Add(new ValidationErrorInfo(validationResult.ErrorMessage));
                }
            }
        }
    }
}