#nullable enable
namespace ProjectFlood.Contracts.Currency
{
    public sealed class SpendSoftRequest
    {
        public long Amount { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}
