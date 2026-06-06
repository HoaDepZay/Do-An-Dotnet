using System;
using QldtSdh.Data;

namespace DbMigrator
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("====================================================");
            Console.WriteLine("BẮT ĐẦU TRIỂN KHAI CÁC BẢNG LÊN SQL SERVER...");
            Console.WriteLine("====================================================");
            Console.WriteLine("Server: 100.109.65.2,1433");
            Console.WriteLine("Database: HE-THONG-QUAN-TRI-306");
            Console.WriteLine("Kết nối và khởi tạo cấu trúc dữ liệu...");

            try
            {
                using (var context = new QldtSdhDbContext())
                {
                    // EnsureCreated will inspect the entities and create the database tables if they do not exist
                    Console.WriteLine("Đang chạy EnsureCreated() (Tự động tạo bảng nếu chưa có)...");
                    context.Database.EnsureCreated();
                    Console.WriteLine("Đã tạo bảng thành công!");

                    Console.WriteLine("Đang bắt đầu seed dữ liệu mẫu (30 học viên + điểm + học phí + đề tài)...");
                    DbInitializer.Initialize(context);
                    Console.WriteLine("Đã nạp dữ liệu mẫu thành công!");
                }

                Console.WriteLine("\n====================================================");
                Console.WriteLine("TRIỂN KHAI DATABASE THÀNH CÔNG RỰC RỠ!");
                Console.WriteLine("Tất cả các bảng đã sẵn sàng để sử dụng.");
                Console.WriteLine("====================================================");
            }
            catch (Exception ex)
            {
                Console.WriteLine("\n[LỖI] Quá trình khởi tạo database thất bại:");
                Console.WriteLine(ex.ToString());
                Console.WriteLine("====================================================");
            }
        }
    }
}
