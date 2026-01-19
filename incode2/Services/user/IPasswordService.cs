namespace incode2.Services.user
{
    public interface IPasswordService
    {
        string Hash(string password);
        bool Verify(string password, string hashedPassword);
    }
}
