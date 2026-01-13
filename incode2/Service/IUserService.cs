using incode2.DTO;
using incode2.Models;

namespace incode2.Service
{
    public interface IUserService
    {
        User ValidateUser(string email, string password); //驗證使用者
        void Register(RegisterDto registerDto);
    }
}
