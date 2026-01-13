using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace incode2.Attributes
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class RequirePermissionAttribute : Attribute, IAuthorizationFilter
    {
        private readonly int _requiredLevel;

        public RequirePermissionAttribute(RoleLevel level)
        {
            _requiredLevel = (int)level;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            // 1. 檢查是否登入
            // 因為 Program.cs 有 UseAuthentication，這裡的 User 已經自動從 Cookie 解析出來了
            if (!context.HttpContext.User.Identity.IsAuthenticated)
            {
                // 回傳 401，API Controller 會處理成 JSON 錯誤或觸發 Program.cs 的跳轉邏輯
                context.Result = new UnauthorizedResult();
                return;
            }

            // 2. 讀取 Cookie 裡的權限資料
            var roleClaim = context.HttpContext.User.Claims.FirstOrDefault(c => c.Type == "RoleId")?.Value;

            // 3. 比較權限 (數字越小權限越大)
            if (roleClaim == null || int.Parse(roleClaim) > _requiredLevel)
            {
                // 回傳 403 Forbidden
                context.Result = new ForbidResult(); // 403 禁止進入
            }
        }
    }
}
