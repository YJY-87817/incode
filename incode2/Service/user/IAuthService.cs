using incode2.DTO.user;
using incode2.Models;

namespace incode2.Service.user 
{   
    public interface IAuthService
    {       
        Task<ServiceResponse<string>> RegisterAsync(RegisterDto registerDto);
        void Register(RegisterDto registerDto);

        User? ValidateUser(string email, string password); //驗證使用者

        Task<ServiceResponse<string>> LoginAsync(string email, string password);

        Task<bool> UserExistsAsync(string email);
    }
}
