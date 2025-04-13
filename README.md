Nhập file .bacpac vào SQL Server:

Mở SQL Server Management Studio (SSMS).

Chuột phải vào Databases > chọn Import Data-tier Application.

Chọn đường dẫn đến file Shop123.bacpac và làm theo hướng dẫn để hoàn tất quá trình import.

Sau khi hoàn tất, cơ sở dữ liệu Shop123 sẽ xuất hiện trong danh sách database.

Cập nhật chuỗi kết nối trong appsettings.json:

Mở file appsettings.json và thay đổi phần "ConnectionStrings" như sau (giả sử SQL Server đang chạy cục bộ)
--
trong file chatbotcontrollner , aicontrollner viết thêm 2 câu lệnh
private readonly string _apiKey = "your_key"; // Khóa API của OpenAI
private readonly string _apiUrl = "https://api.openai.com/v1/chat/completions"; // Địa chỉ API endpoint của OpenAI
