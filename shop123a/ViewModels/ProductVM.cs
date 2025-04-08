namespace shop123a.ViewModels
{
    public class ProductVM
    {
        public int ProductId { get; set; }

        public string ProductName { get; set; } = null!;

        public string? Description { get; set; }

        public decimal Price { get; set; }

        public int QuantityInStock { get; set; }

        public int? CategoryId { get; set; }

        public string? ImageUrl { get; set; }
    }
}
