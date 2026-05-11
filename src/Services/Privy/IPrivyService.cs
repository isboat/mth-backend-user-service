using System.Text.Json.Serialization;

namespace UserService.Services.Privy
{
    public interface IPrivyService
    {
        Task<PrivyUser> VerifyTokenAsync(string privyToken, string expectedPrivyId);
    }

    public class PrivyUser
    {
        public string Id { get; set; } = null!;

        [JsonPropertyName("linked_accounts")]
        public PrivyLinkedAccount[] LinkedAccounts { get; set; } = [];
    }

    public class PrivyLinkedAccount
    {
        public string Type { get; set; } = null!;
        public string Address { get; set; } = null!;
    }

    public class PrivyWallet
    {
        public string Address { get; set; } = null!;
        public string ChainType { get; set; } = null!;
    }
}
