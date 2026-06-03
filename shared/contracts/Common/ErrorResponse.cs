#nullable enable
namespace ProjectFlood.Contracts.Common
{
    public sealed class ErrorResponse
    {
        public string Code    { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}
