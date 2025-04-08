namespace shop123a.ViewModels
{
    public class CartItem
    {
        public int ProductId { get; set; }

        public string ProductName { get; set; } = null!;

        public decimal Price { get; set; }
        public string? ImageUrl { get; set; }
        public int SoLuong {  get; set; }
        public double ThanhTien => (double)(SoLuong * Price);
    }
}
