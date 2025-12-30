namespace incode2.Partials
{
    public class Follow
    {
        // 追蹤者本人
        public virtual User Follower { get; set; } = null!;

        // 被追蹤的人
        public virtual User Followee { get; set; } = null!;
    }
}
