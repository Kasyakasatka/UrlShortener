using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Custom_Exceptions
{
    public class NotFoundException : Exception
    {
        public NotFoundException(string name, object key)
            : base($"Entity \"{name}\" ({key}) was not found.")
        {
        }
    }

    public class ValidationException : Exception
    {
        public ValidationException(string message) : base(message) { }
    }

    public class DuplicateAliasException : Exception
    {
        public DuplicateAliasException(string alias)
            : base($"The custom alias '{alias}' is already in use.")
        {
        }
    }
}
