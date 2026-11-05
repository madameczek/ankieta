using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Polls.Api.Infrastructure;

internal static class SqlErrors
{
    /// <summary>Cannot insert duplicate key row in object with unique index.</summary>
    public const int UniqueIndexViolation = 2601;

    /// <summary>Violation of PRIMARY KEY / UNIQUE constraint.</summary>
    public const int UniqueConstraintViolation = 2627;

    /// <summary>True when the save failed only because a row with the same key already exists (e.g. a concurrent insert).</summary>
    public static bool IsDuplicateKey(this DbUpdateException ex) =>
        ex.InnerException is SqlException { Number: UniqueIndexViolation or UniqueConstraintViolation };
}
