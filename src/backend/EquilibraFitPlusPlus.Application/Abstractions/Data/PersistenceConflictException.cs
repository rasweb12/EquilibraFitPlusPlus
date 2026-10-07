using System;
using System.Collections.Generic;
using System.Text;

namespace EquilibraFitPlusPlus.Application.Abstractions.Data;

public sealed class PersistenceConflictException : Exception
{
    public PersistenceConflictException(
        string code,
        string message,
        Exception? innerException = null)
        : base(message, innerException)
    {
        Code = code;
    }

    public string Code { get; }
}
