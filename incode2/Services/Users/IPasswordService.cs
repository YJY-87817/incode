namespace incode2.Services.Users
{
    public interface IPasswordService
    {
        string Hash(string password);
        bool Verify(string password, string hashedPassword);
    }
}
