# TÀI LIỆU QUY TRÌNH VẬN HÀNH & TRIỂN KHAI HỆ THỐNG (DEPLOYMENT FLOW)

Tài liệu này tổng hợp toàn bộ luồng vận hành (Data Flow, Authentication Flow, Workflow) của dự án **Trung tâm điều hành và hồ sơ học viên 360°** trên môi trường chạy thật (Production) cùng quy trình đóng gói cài đặt.

---

## 🌐 1. Sơ Đồ Kiến Trúc Hoạt Động (System Architecture Flow)

Hệ thống chạy thật trên môi trường Cloud Azure kết nối đa phương tiện:

```mermaid
graph TD
    subgraph Client_Side [Máy Tính Người Dùng - Windows Client]
        WPF[QldtSdh.Wpf.exe - Ứng dụng WPF]
        Session[SessionService.cs - Bộ nhớ RAM]
        API_Call[ApiService.cs - HttpClient]
        WPF -->|Đăng nhập / Thao tác| API_Call
        API_Call -->|Đọc Token & Username| Session
    end

    subgraph Cloud_Server [Hạ Tầng Điện Toán Đám Mây Azure]
        AppService[Azure App Service - Web API chạy thật]
        DB[(Azure SQL Database - Lưu trữ cơ sở dữ liệu)]
        
        API_Call -->|Gửi yêu cầu HTTPS + JWT Token| AppService
        AppService <-->|Truy vấn EF Core| DB
    end

    style Client_Side fill:#eafaf1,stroke:#2ecc71,stroke-width:2px
    style Cloud_Server fill:#ebf5fb,stroke:#3498db,stroke-width:2px
```

---

## 🔑 2. Luồng Xác Thực Hệ Thống (Authentication Flow)

Ứng dụng bảo mật bằng cơ chế Token-based Authentication (JWT Bearer):

```mermaid
sequenceDiagram
    autonumber
    actor User as Cán bộ đào tạo / Admin
    participant WPF as WPF Client UI
    participant API as Azure App Service (API)
    participant DB as Azure SQL Database

    User->>WPF: Nhập Username và Mật khẩu (ví dụ: canboA / canbo123)
    WPF->>API: POST /api/auth/login (Dữ liệu JSON)
    API->>API: Băm mật khẩu nhập vào bằng SHA-256
    API->>DB: Kiểm tra Username & So khớp PasswordHash
    DB-->>API: Trả về thông tin Cán bộ + Quyền hạn (Role)
    API->>API: Ký điện tử và sinh mã JWT Token (Claims: UserId, Username, Role)
    API-->>WPF: Phản hồi 200 OK + JWT Token + RoleCode + FullName
    WPF->>WPF: Lưu Token và Username vào Session (RAM)
    WPF->>WPF: Tải giao diện chính (MainWindow), điều hướng Menu theo Quyền hạn
    Note over WPF, API: Cho các request sau (Xem điểm, Đóng học phí, Tạo sự vụ...)
    WPF->>API: HTTP Request + Header [Authorization: Bearer <token>] & [X-User-Name: <username>]
    API->>API: Giải mã & Xác thực chữ ký Token
    API-->>WPF: Trả về dữ liệu nghiệp vụ
```

---

## 🛠️ 3. Luồng Xử Lý Sự Vụ Hỗ Trợ (Case Management & Business Rules Flow)

Quy trình xử lý phản hồi khiếu nại của học viên tuân thủ nghiêm ngặt mô hình workflow và các ràng buộc nghiệp vụ:

```mermaid
flowchart TD
    Start([1. Tạo Sự Vụ]) -->|Trạng thái: Created| Assign[2. Gán Cán bộ Xử lý]
    Assign -->|Trạng thái: Assigned| CheckRule1{3. Cán bộ thực hiện yêu cầu chuyển sang Processing/Closed?}
    
    %% Rule 1
    CheckRule1 -->|KHÔNG phải Cán bộ được gán / ADMIN| DenyRule1[Hủy bỏ - Báo lỗi: 400 Bad Request]
    CheckRule1 -->|Hợp lệ| Process[4. Bắt đầu xử lý - Trạng thái: Processing]
    
    %% Rule 2
    Process --> RequestClose[5. Yêu cầu Đóng sự vụ - Trạng thái: Closed]
    RequestClose --> CheckRule2{6. Có ít nhất một ghi chú chứa từ khóa kết luận?}
    CheckRule2 -->|KHÔNG có| DenyRule2[Hủy bỏ - Báo lỗi: Yêu cầu ghi chú kết luận xử lý]
    CheckRule2 -->|CÓ ghi chú hợp lệ| Close[7. Hoàn tất - Đổi trạng thái sang Closed]
    
    style DenyRule1 fill:#f9ebea,stroke:#c0392b,stroke-width:1px
    style DenyRule2 fill:#f9ebea,stroke:#c0392b,stroke-width:1px
    style Close fill:#d5f5e3,stroke:#27ae60,stroke-width:2px
```

---

## 📦 4. Quy Trình Triển Khai Thực Tế (Deployment Flow)

Quy trình đóng gói phần mềm và đưa lên chạy thật đã thực hiện thành công:

### 4.1. Quy Trình Triển Khai Web API (Backend)
1.  **Cấu hình CSDL Azure SQL:** Bật tính năng *"Allow Azure services and resources to access this server"* trên Azure Portal.
2.  **Publish ứng dụng:** Publish dự án `QldtSdh.WebApi` trực tiếp lên **Azure App Service** thông qua Visual Studio.
3.  **Cấu hình biến môi trường:** Cài đặt Connection String `ConnectionStrings__DefaultConnection` trên Cấu hình App Service của Azure Portal để đảm bảo kết nối CSDL bảo mật và tự động chạy cơ chế Seed Data khi khởi động.
4.  **Kích hoạt Swagger:** Bật cấu hình Swagger UI chạy ở cả môi trường Production giúp việc hiển thị danh sách API phục vụ báo cáo.

### 4.2. Quy Trình Triển Khai WPF App (Frontend)
1.  **Cấu hình API Endpoint:** Thay đổi `BaseAddress` trong file cấu hình `appsettings.json` của WPF trỏ về link API thật trên Azure: `https://qldtsdh-api-a6gcb9fcb3bffhf2.eastasia-01.azurewebsites.net/api/`.
2.  **Biên dịch Self-Contained:** Chạy lệnh biên dịch tự chứa runtime .NET 10 cho hệ điều hành Windows 64-bit:
    ```powershell
    dotnet publish -c Release -r win-x64 --self-contained true
    ```
    *Dữ liệu build thành công xuất ra tại thư mục: `frontend/QldtSdh.Wpf/bin/Release/net10.0-windows/win-x64/publish/`.*
3.  **Đóng gói bộ cài đặt Setup:** Sử dụng **Inno Setup** chạy tệp cấu hình [frontend/setup_config.iss](file:///c:/Users/DANGQUANGHOA/Desktop/Do-An-Dotnet/frontend/setup_config.iss) đóng gói toàn bộ thư mục xuất bản thành duy nhất **1 file cài đặt `QldtSdh_Setup.exe`**.
