namespace Product.Model
{
    public class ProductDetail
    {
        public int ProductDetailId { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public DateTime PostedDate { get; set; }
        public bool IsActive { get; set; }
        public string Status { get; set; } // Create, Update, Delete
    }
}
