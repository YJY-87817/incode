using Microsoft.EntityFrameworkCore;
using incode2.Models;

namespace incode2.Partials
{
    public class User
    {     
            // 我主動追蹤了誰 (透過 Follow 表的 FollowerId 關聯)
            // 這裡的結果是：這筆使用者作為 "追蹤者" 的所有紀錄
            public virtual ICollection<Follow> FollowsAsFollower { get; set; } = new List<Follow>();

            // 誰正在追蹤我 (透過 Follow 表的 FolloweeId 關聯)
            // 這裡的結果是：這筆使用者作為 "被追蹤者" 的所有紀錄
            public virtual ICollection<Follow> FollowsAsFollowee { get; set; } = new List<Follow>();

        //導覽屬性
        //Follow 表連接的兩端其實都是同一個 User 表。這在資料庫中稱為「自我參照 (Self-referencing)」。
        //如果沒有導覽屬性，EF Core 無法辨識 FollowerId 代表的是「主動去追蹤的人」，還是「被追蹤的人」。
        //可以明確定義兩個方向：FollowsAsFollower(我追蹤的人)：從我的角度出發，連到 Follow 表的 FollowerId。
        //FollowsAsFollowee(我的粉絲)：從別人的角度連回來，連到 Follow 表的 FolloweeId。
    }
}
