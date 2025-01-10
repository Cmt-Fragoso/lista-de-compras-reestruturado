using System;

namespace ListaCompras.Core.Models
{
    public class SyncOperation
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public SyncOperationType Type { get; set; }
        public string EntityType { get; set; }
        public int EntityId { get; set; }
        public string Data { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public enum SyncOperationType
    {
        Create,
        Update,
        Delete
    }
}