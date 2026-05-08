namespace RemittanceManager.Application.Common.Constants;

public static class ErrorMessages
{
    public const string UnhandledExceptionLog = "Unhandled exception while processing request {Path}";
    public const string NotFound = "Resource not found";
    public const string Unauthorized = "Unauthorized";
    public const string Conflict = "Conflict";
    public const string InternalServerError = "Internal server error";
    public const string UnexpectedError = "An unexpected error occurred.";
    public const string RemittanceNotFound = "Remittance '{0}' was not found.";
    public const string CannotDeleteRemittance = "Cannot delete a remittance with status '{0}'. Only Pending or Cancelled remittances can be deleted.";
    public const string InvalidStatusTransition = "Invalid status transition from '{0}' to '{1}'.";
    public const string CannotModifyFields = "Cannot modify fields ({0}) when status is '{1}'. Only Status can be changed.";
}
