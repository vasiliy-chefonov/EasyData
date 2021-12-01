using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace EasyData.Exceptions
{
    /// <summary>
    /// Custom entity validation exception.
    /// </summary>
    public class EntityValidationException: Exception
    {
        /// <summary>
        /// Store inner errors for each of the fields.
        /// </summary>
        public List<ValidationErrorInfo> Errors { get; set; } = new List<ValidationErrorInfo>();

        public EntityValidationException(List<ValidationErrorInfo> errors = null)
        {
            if (errors != null) {
                Errors = errors;
            }
        }

        public EntityValidationException(string message, List<ValidationErrorInfo> errors = null) : base(message)
        {
            if (errors != null) {
                Errors = errors;
            }
        }

        public EntityValidationException(string message, Exception inner, List<ValidationErrorInfo> errors = null) : base(message, inner)
        {
            if (errors != null) {
                Errors = errors;
            }
        }
    }

    /// <summary>
    /// Store info about the validation error for the field.
    /// </summary>
    public class ValidationErrorInfo
    {
        /// <summary>
        /// Error code.
        /// </summary>
        [JsonProperty("code")]
        public int? Code { get; set; }

        /// <summary>
        /// Field name.
        /// </summary>
        [JsonProperty("field")]
        public string Field { get; set; }

        /// <summary>
        /// Error message to show.
        /// </summary>
        [JsonProperty("message")]
        public string Message { get; set; }

        public ValidationErrorInfo(string message, string field=null, int? code=null)
        {
            Message = message;
            Field = field;
            Code = code;
        }
    }
}
