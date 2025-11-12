namespace BlogAppFrontend.Models
{
    public class Follower
    {
        public string Id { get; set; }
        public string FollowerId { get; set; }
        public string FollowingId { get; set; }
        public DateTime CreatedAt { get; set; }
        public string FollowerUsername { get; set; }
        public string FollowingUsername { get; set; }
    }
}