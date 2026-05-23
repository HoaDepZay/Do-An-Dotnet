# HƯỚNG DẪN CHI TIẾT CÁC BƯỚC TRIỂN KHAI HỆ THỐNG (DEPLOYMENT GUIDE)

Tài liệu này ghi lại chính xác từng bước thực tế để triển khai Backend API lên đám mây Azure App Service và đóng gói bộ cài đặt Frontend WPF chạy thật bằng công cụ Inno Setup.

---

## ☁️ PHẦN 1: TRIỂN KHAI BACKEND WEB API LÊN AZURE APP SERVICE

Thực hiện trên **Visual Studio (màu tím)** mở Solution Backend (`QldtSdh.Backend.slnx`):

### Bước 1: Khởi động trình thuật sĩ Publish
1.  Nhấp chuột phải vào dự án **`QldtSdh.WebApi`** $\rightarrow$ chọn **Publish...**
2.  Tại hộp thoại hiện ra, chọn **Azure** $\rightarrow$ bấm **Next**.
3.  Chọn **Azure App Service (Windows)** hoặc **Linux** $\rightarrow$ bấm **Next**.

### Bước 2: Đăng nhập và tạo Web App Instance
1.  Đăng nhập tài khoản Microsoft chứa gói **Azure for Students** của bạn (ví dụ: `dangquanghoa206@gmail.com`).
2.  Tại mục **Subscription name**, chọn **Azure for Students**.
3.  Nhấp chọn nút **`+ Create new`** (hoặc link *Create a new instance*) để tạo dịch vụ mới:
    *   **Name:** Nhập tên duy nhất cho API của bạn (ví dụ: `qldtsdh-api-a6gcb9fcb3bffhf2`).
    *   **Resource Group:** Nhấp **New** tạo mới một nhóm (ví dụ: `QldtSdhGroup`).
    *   **Hosting Plan:** Nhấp **New** tạo gói lưu trữ mới:
        *   **Location:** Chọn **East Asia** (Hồng Kông) hoặc **Southeast Asia** (Singapore) để kết nối nhanh nhất.
        *   **Size:** Chọn gói **Free (F1)** để không bị trừ tiền $100 credit.
    *   Nhấp **Create** ở góc dưới cùng bên phải và chờ khoảng 1 phút để Azure tạo tài nguyên.
4.  Khi tạo xong và quay lại màn hình chọn App Service:
    *   Tích chọn vào ô **`Deploy as ZIP package`** (Đóng gói và đẩy file ZIP).
    *   Bấm **Next**.

### Bước 3: Cấu hình API Management & Deployment Type
1.  Tại màn hình **API Management**:
    *   Tích chọn vào ô **`Skip this step`** (Bỏ qua bước này) ở góc dưới cùng bên trái.
    *   Bấm **Next**.
2.  Tại màn hình **Deployment type**:
    *   Chọn tùy chọn đầu tiên: **`Publish (generates pubxml file)`**.
    *   Bấm **Finish** $\rightarrow$ Đợi chương trình tạo profile $\rightarrow$ Bấm **Close** để đóng cửa sổ wizard.

### Bước 4: Thực hiện đẩy code (Publish)
1.  Tại màn hình điều khiển chính của trang Publish, nhấp vào nút **`Publish`** (Nút màu xanh lá/xanh lam ở góc trên bên phải).
2.  Đợi Visual Studio biên dịch dự án và đẩy code lên Azure (xem tiến trình ở cửa sổ Output).
3.  Khi hoàn thành, trình duyệt sẽ tự động mở trang web API chạy thật của bạn (ví dụ: `https://qldtsdh-api-a6gcb9fcb3bffhf2.eastasia-01.azurewebsites.net/`). 
4.  *Kiểm tra:* Truy cập thử thêm đuôi `/api/student` ở cuối link. Nếu màn hình trả về lỗi **`401 Unauthorized`** nghĩa là API đã chạy thật và hoạt động tốt.
5.  *Trang tài liệu:* Bạn có thể truy cập trang test API chạy thật tại: `https://<ten-app-cua-ban>.azurewebsites.net/swagger/index.html`.

### Bước 5: Cấu hình biến môi trường kết nối Database
Để API trên Azure kết nối được với cơ sở dữ liệu đám mây Azure SQL:
1.  Truy cập vào [Azure Portal](https://portal.azure.com/).
2.  Vào dịch vụ **App Services** $\rightarrow$ chọn Web App của bạn.
3.  Chọn mục **Configuration** (hoặc **Environment variables** ở menu bên trái).
4.  Thêm mới một cấu hình (Connection String):
    *   **Name:** `ConnectionStrings__DefaultConnection` (chú ý có 2 dấu gạch dưới).
    *   **Value:** Chuỗi kết nối đến Azure SQL Database của bạn.
5.  Bấm **Save** để lưu cấu hình.

---

## 🖥️ PHẦN 2: BIÊN DỊCH VÀ ĐÓNG GÓI FRONTEND WPF CLIENT

Thực hiện trên máy tính cục bộ của bạn:

### Bước 1: Cấu hình địa chỉ API chạy thật
1.  Mở dự án Frontend WPF.
2.  Mở file [appsettings.json](file:///c:/Users/DANGQUANGHOA/Desktop/Do-An-Dotnet/frontend/QldtSdh.Wpf/appsettings.json), thay thế `BaseAddress` trỏ về link API chạy thật trên Azure của bạn:
    ```json
    {
      "ApiSettings": {
        "BaseAddress": "https://qldtsdh-api-a6gcb9fcb3bffhf2.eastasia-01.azurewebsites.net/api/"
      }
    }
    ```

### Bước 2: Biên dịch ứng dụng dạng Self-Contained (Tự chứa Runtime)
Để phần mềm có thể chạy trên mọi máy tính khác mà không yêu cầu cài đặt .NET 10:
1.  Mở PowerShell hoặc Command Prompt tại thư mục gốc dự án (`Do-An-Dotnet/`).
2.  Chạy lệnh biên dịch sau:
    ```powershell
    dotnet publish frontend/QldtSdh.Wpf/QldtSdh.Wpf.csproj -c Release -r win-x64 --self-contained true
    ```
3.  Khi có thông báo thành công, toàn bộ file đóng gói sẽ nằm tại:
    `frontend/QldtSdh.Wpf/bin/Release/net10.0-windows/win-x64/publish/`

### Bước 3: Tạo file cài đặt duy nhất `QldtSdh_Setup.exe` bằng Inno Setup
1.  Khởi động phần mềm **Inno Setup Compiler**.
2.  Chọn **Open an existing script file** $\rightarrow$ tìm chọn đến file [frontend/setup_config.iss](file:///c:/Users/DANGQUANGHOA/Desktop/Do-An-Dotnet/frontend/setup_config.iss) có sẵn trong dự án.
3.  Nhấn nút **Play (▶️)** trên thanh công cụ hoặc bấm phím tắt **`Ctrl + F9`** để bắt đầu nén và tạo file cài đặt.
4.  Khi quá trình biên dịch hoàn tất, file cài đặt duy nhất mang tên **`QldtSdh_Setup.exe`** sẽ xuất hiện ngay tại thư mục **`frontend/`**.

Bây giờ, bạn chỉ cần gửi duy nhất tệp **`QldtSdh_Setup.exe`** này cho bất kỳ ai chạy thử trên máy của họ mà không cần nén/giải nén rườm rà!
