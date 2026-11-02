using incode2.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace incode2.Extensions
{
    public static class ResultDTOExtensions
    {
        /// <summary>
        /// 將 ResultDTO 轉成對應 HTTP 狀態碼的回應：
        /// 成功一律 200 (保留 body，避免 204 時前端拿不到資料)；失敗使用 ResultDTO.Code (400 / 404 / 409 / 500)
        /// </summary>
        public static ActionResult<ResultDTO<T>> ToActionResult<T>(this ControllerBase controller, ResultDTO<T> result)
        {
            if (result.Ok)
                return controller.Ok(result);

            var status = result.Code is >= 400 and <= 599 ? result.Code : 400;
            return controller.StatusCode(status, result);
        }
    }
}
