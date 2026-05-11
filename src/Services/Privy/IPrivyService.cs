namespace UserService.Services.Privy
{
    public interface IPrivyService
    {
        Task<PrivyUser> VerifyTokenAsync(string privyToken);
    }

    public class PrivyUser
    {
        public string Id { get; set; } = null!;
        public string? Email { get; set; }
        public List<PrivyWallet> Wallets { get; set; } = new();
        public PrivyUser? User { get; set; }
    }

    public class PrivyWallet
    {
        public string Address { get; set; } = null!;
        public string ChainType { get; set; } = null!;
    }
}
