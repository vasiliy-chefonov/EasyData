using System;
using System.Collections.Generic;
using System.Text;

namespace EasyData.Services
{
    /// <summary>
    /// Entity validator abstract class.
    /// </summary>
    public abstract class Validator<T>
    {
        /// <summary>
        /// Validate entity.
        /// Throw an exception if there is a failure while validating.
        /// </summary>
        /// <param name="entity">Entity instance.</param>
        public abstract void Validate(T entity);
    }

    /// <summary>
    /// Default validator class.
    /// </summary>
    /// <typeparam name="T">Entity type.</typeparam>
    public class PredefinedValidator<T> : Validator<T>
    {
        private readonly Action<T> _validate;

        /// <summary>
        /// Set validation action.
        /// </summary>
        /// <param name="validate">Validating action.</param>
        public PredefinedValidator(Action<T> validate)
        {
            this._validate = validate ?? throw new ArgumentNullException(nameof(validate));
        }

        /// <inheritdoc />
        public override void Validate(T entity) => _validate(entity);
    }
}
