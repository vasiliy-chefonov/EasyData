using System;
using System.Collections.Generic;
using System.Text;

namespace EasyData.Exceptions
{
    /// <summary>
    /// Custom entity validation exception.
    /// </summary>
    public class EntityValidationException: Exception
    {
        public EntityValidationException()
        {

        }

        public EntityValidationException(string message): base(message)
        {

        }

        public EntityValidationException(string message, Exception inner): base(message, inner)
        {

        }
    }
}
