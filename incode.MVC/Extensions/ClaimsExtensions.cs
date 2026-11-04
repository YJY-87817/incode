using System.Security.Claims;
namespace incode.MVC.Extensions
{
    public static class ClaimsExtensions// 擴充方法必須是 static，且第一個參數用 this
    {
        //讓所有 Controller 的 User 物件都能直接呼叫.GetId()
        public static int GetId(this ClaimsPrincipal user)
        {
            var idClaim = user.FindFirst(ClaimTypes.NameIdentifier);
            if (idClaim != null && int.TryParse(idClaim.Value, out int userId))
            {
                return userId;
            }
            return 0;
        }
    }
}
