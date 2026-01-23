namespace SnakeAid.Core.Domains
{
    public class Account : BaseEntity
    {
        public Guid Id { get; set; }
        public string Username { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string PhoneNumber { get; set; }
        public AccountRole Role { get; set; } = AccountRole.User;
        public bool IsActive { get; set; } = true;
    }

    public enum AccountRole
    {
        User = 0,
        Admin = 1,
        Expert = 2,
        Rescuer = 3,
    }
}