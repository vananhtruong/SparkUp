## SparkUp
# Thành viên 1 - Phương (đã được phân công)
-Phụ trách Authentication & Đăng ký
-Đăng ký user thông thường
-Đăng ký workers (hồ sơ thợ)
-Login (tích hợp Google và Facebook OAuth)
-Forgot password, gửi email reset (SMTP)
-WorkerProfileService (hồ sơ thợ)
# Thành viên 2 - Quyền (haiquyen00)
-Phụ trách chức năng kinh doanh chính
-Tìm kiếm danh sách thợ (theo địa điểm, loại dịch vụ)
-Hiển thị danh sách thợ thuộc service khách hàng muốn thuê
-Đặt lịch thợ theo ngày và khối lượng công việc
-TaskService (quản lý công việc/booking)
-Quản lý lịch trình của thợ
-Feedback cho thợ sau khi booking
# Thành viên 3 Anh 
-Phụ trách thanh toán và tính năng mở rộng
-PaymentService (thanh toán)
-Thanh toán trước cho admin
-Xử lý duyệt yêu cầu công việc của thợ
-Luồng chuyển tiền sau khi confirm hoàn thành
-Hệ thống tin nhắn
-Quản lý ví và giao dịch

# Công việc chung
Thiết kế database và migrations 
Xây dựng layout chung
Code reviews
Testing
Tích hợp các module
# Database

 - Users

| Column       | Type     | Description                   |
| ------------ | -------- | ----------------------------- |
| Id           | INT, PK  | Khóa chính                    |
| FullName     | NVARCHAR | Họ tên                        |
| Email        | NVARCHAR | Duy nhất                      |
| PhoneNumber  | NVARCHAR | Số điện thoại                 |
| PasswordHash | NVARCHAR | Mật khẩu                      |
| Role         | NVARCHAR | ‘Customer’, ‘Worker’, ‘Admin’ |
| AvatarUrl    | NVARCHAR | Ảnh đại diện                  |
| IsActive     | BIT      | Trạng thái hoạt động          |
| CreatedAt    | DATETIME | Ngày tạo                      |



 - WorkerProfiles

| Column          | Type     | Description                     |
| --------------- | -------- | ------------------------------- |
| Id              | INT, PK  | Khóa chính                      |
| UserId          | INT, FK  | Liên kết đến bảng `Users`       |
| Skills          | NVARCHAR | Các kỹ năng (danh sách kỹ năng) |
| Description     | TEXT     | Mô tả kinh nghiệm               |
| ExperienceYears | INT      | Số năm kinh nghiệm              |
| PortfolioUrl    | NVARCHAR | Link hình ảnh / chứng chỉ       |
| RatingAverage   | FLOAT    | Điểm đánh giá trung bình        |
| IsConfirmed     | BIT      | Được admin duyệt hồ sơ chưa     |

 - Tasks / Bookings

| Column        | Type     | Description                                                    |
| ------------- | -------- | -------------------------------------------------------------- |
| Id            | INT, PK  | Khóa chính                                                     |
| CustomerId    | INT, FK  | Người thuê thợ (liên kết `Users.Id`)                           |
| WorkerId      | INT, FK  | Thợ được thuê (liên kết `Users.Id`)                            |
| TaskTypeId    | INT, FK  | Loại công việc (liên kết `TaskTypes.Id`)                       |
| Address       | NVARCHAR | Địa chỉ thi công                                               |
| EstimatedWork | NVARCHAR | Ước tính khối lượng công việc (ví dụ: 3 bóng đèn, 5m dây...)   |
| Description   | TEXT     | Mô tả chi tiết yêu cầu                                         |
| ScheduledTime | DATETIME | Thời gian khách yêu cầu thợ có mặt                             |
| Status        | NVARCHAR | Trạng thái: Pending / Confirmed / InProgress / Done / Canceled |
| PaymentStatus | NVARCHAR | Trạng thái thanh toán: Unpaid / Paid / Refunded                |
| CreatedAt     | DATETIME | Ngày tạo                                                       |
| UpdatedAt     | DATETIME | Ngày cập nhật gần nhất                                         |


 - Payments

| Column      | Type     | Description                |
| ----------- | -------- | -------------------------- |
| Id          | INT, PK  | Khóa chính                 |
| TaskId      | INT, FK  | Liên kết với bảng `Tasks`  |
| Amount      | DECIMAL  | Số tiền                    |
| PaymentTime | DATETIME | Ngày thanh toán            |
| Method      | NVARCHAR | 'VNPay', 'Momo', 'Cash'... |
| Status      | NVARCHAR | Success / Failed / Pending |

 - Feedbacks

| Column     | Type     | Description                             |
| ---------- | -------- | --------------------------------------- |
| Id         | INT, PK  | Khóa chính                              |
| TaskId     | INT, FK  | Liên kết với bảng `Tasks`               |
| FromUserId | INT, FK  | Người gửi feedback (thường là Customer) |
| ToWorker   | INT, FK  | Người nhận feedback (thường là Worker)  |
| Rating     | INT      | Điểm đánh giá (1–5 sao)                 |
| Comment    | NVARCHAR | Nội dung nhận xét                       |
| CreatedAt  | DATETIME | Thời điểm gửi feedback                  |

 - Messages

| Column   | Type     | Description                      |
| -------- | -------- | -------------------------------- |
| Id       | INT, PK  | Khóa chính                       |
| TaskId   | INT, FK  | Liên kết task (nơi 2 người chat) |
| SenderId | INT, FK  | Người gửi                        |
| Message  | TEXT     | Nội dung tin nhắn                |
| SentAt   | DATETIME | Thời điểm gửi                    |

 - Notifications

| Column    | Type     | Description        |
| --------- | -------- | ------------------ |
| Id        | INT, PK  | Khóa chính         |
| UserId    | INT, FK  | Người nhận         |
| Message   | TEXT     | Nội dung thông báo |
| IsRead    | BIT      | Đã đọc hay chưa    |
| CreatedAt | DATETIME | Ngày gửi           |

- WorkerSchedules

| Column    | Type          | Description                                 |
| --------- | ------------- | ------------------------------------------- |
| Id        | INT, PK       | Khóa chính                                  |
| WorkerId  | INT, FK       | Liên kết đến bảng `Users` (Role = 'Worker') |
| StartTime | DATETIME      | Bắt đầu ca làm việc hoặc thời gian bận      |
| EndTime   | DATETIME      | Kết thúc ca làm việc hoặc thời gian bận     |
| Type      | NVARCHAR      | 'Available' / 'Busy' / 'Booked' / 'Break'   |
| TaskId    | INT, FK, NULL | Nếu là thời gian bận vì đã được đặt Task    |
| Note      | NVARCHAR      | Ghi chú (ví dụ: “Bận đi công trình A”)      |

- Wallets

| Column    | Type     | Description               |
| --------- | -------- | ------------------------- |
| Id        | INT, PK  | Khóa chính                |
| UserId    | INT, FK  | Liên kết với bảng `Users` |
| Balance   | DECIMAL  | Số dư khả dụng hiện tại   |
| IsLocked  | BIT      | Có bị khóa ví không       |
| UpdatedAt | DATETIME | Lần cuối cập nhật số dư   |

- WalletTransactions

| Column      | Type          | Description                           |
| ----------- | ------------- | ------------------------------------- |
| Id          | INT, PK       | Khóa chính                            |
| WalletId    | INT, FK       | Ví liên quan                          |
| Amount      | DECIMAL       | Số tiền giao dịch (âm nếu trừ tiền)   |
| Type        | NVARCHAR      | Deposit / Withdraw / Payment / Refund |
| TaskId      | INT, FK, NULL | Liên kết task nếu liên quan           |
| Description | NVARCHAR      | Mô tả chi tiết                        |
| CreatedAt   | DATETIME      | Thời gian giao dịch                   |
| Status      | NVARCHAR      | Success / Pending / Failed            |

- TaskTypes

| Column      | Type     | Description                       |
| ----------- | -------- | --------------------------------- |
| Id          | INT, PK  | Khóa chính                        |
| Name        | NVARCHAR | Tên loại công việc (VD: Sửa điện) |
| Description | NVARCHAR | Mô tả thêm (tùy chọn)             |
| IsActive    | BIT      | Còn sử dụng hay không             |


