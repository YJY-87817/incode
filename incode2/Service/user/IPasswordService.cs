namespace incode2.Service.user
{
    public interface IPasswordService
    {
        string Hash(string password);
        bool Verify(string password, string hashedPassword);
    }
}
