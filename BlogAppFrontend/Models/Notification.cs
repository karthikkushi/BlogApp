namespace BlogAppFrontend.Models
{
    public class Notification
    {
        public string Id { get; set; } // MongoDB ObjectId
        public string UserId { get; set; } // Supabase user ID (recipient)
        public string Type { get; set; } // e.g., "follow"
        public string TriggerUserId { get; set; } // Supabase user ID (trigger)
        public string PostId { get; set; } // Optional
        public string Message { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
