namespace Harjoitustyö.Services
{
    public enum ServiceStatus
    {
        Success,
        NotFound,
        Forbidden,
        BadRequest,
        Failed
    }

    public record ServiceResult<T>(ServiceStatus Status, T? Value = default, string? Error = null)
    {
        public static ServiceResult<T> Ok(T value) => new(ServiceStatus.Success, value);
        public static ServiceResult<T> Fail(ServiceStatus status, string? error = null) => new(status, default, error);
    }
}
