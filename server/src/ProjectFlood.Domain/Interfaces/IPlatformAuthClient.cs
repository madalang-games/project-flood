namespace ProjectFlood.Domain.Interfaces;

public interface IPlatformAuthClient
{
    Task<long?> GetUserIdByPidAsync(string pid, CancellationToken ct);
}
