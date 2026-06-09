#nullable enable

namespace ProjectFlood.Contracts.Player
{
    public sealed class UserProfileUpdateRequest
    {
        public string? DisplayName { get; set; }
        public int? AvatarId { get; set; }
    }

    public sealed class UserProfileUpdateResponse
    {
        public string DisplayName { get; set; } = string.Empty;
        public int AvatarId { get; set; }
    }
}
