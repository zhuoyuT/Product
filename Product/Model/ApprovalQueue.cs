namespace Product.Model
{
    public class ApprovalQueue
    {
        public int ApprovalQueueId { get; set; }
        public int ProductId { get; set; }
        public string RequestReason { get; set; }
        public DateTime RequestDate { get; set; }
        public ProductDetail Product { get; set; }
        public decimal? OriginalPrice { get; set; }
        public string? OriginalStatus { get; set; }
    }
}
