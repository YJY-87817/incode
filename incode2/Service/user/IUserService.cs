using incode2.DTO.user;
using incode2.Models;

namespace incode2.Service.user
{
    public interface IUserService
    {
        User ValidateUser(string email, string password); //驗證使用者
        void Register(RegisterDto registerDto);
    }
}
