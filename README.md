# DACN_SmartCoffee
# Database
-- Bảng Roles (Vai trò)
CREATE TABLE Roles (
    RoleID INT PRIMARY KEY IDENTITY,
    RoleName VARCHAR(50)  -- 'Customer', 'Manager', 'Staff'
);

-- Bảng Users (Người dùng)
CREATE TABLE Users (
    UserID INT PRIMARY KEY IDENTITY,
    FullName VARCHAR(255),
    Email VARCHAR(255) UNIQUE,
    PhoneNumber VARCHAR(20),
    PasswordHash VARCHAR(255),
    RoleID INT,  -- Liên kết với bảng Roles
    CreatedAt DATETIME DEFAULT GETDATE(),
    UpdatedAt DATETIME,
    FOREIGN KEY (RoleID) REFERENCES Roles(RoleID)
);

-- Bảng Branches (Chi nhánh)
CREATE TABLE Branches (
    BranchID INT PRIMARY KEY IDENTITY,
    BranchName VARCHAR(255),
    Location VARCHAR(255),
    PhoneNumber VARCHAR(20),
    CreatedAt DATETIME DEFAULT GETDATE()
);

-- Bảng Tables (Bàn)
CREATE TABLE Tables (
    TableID INT PRIMARY KEY IDENTITY,
    BranchID INT,
    TableNumber INT,
    Area VARCHAR(50),  -- 'Window', 'Quiet', 'Outdoor'
    Status VARCHAR(50),  -- 'Available', 'Reserved', 'Occupied'
    Capacity INT,  -- Số ghế ngồi
    FOREIGN KEY (BranchID) REFERENCES Branches(BranchID)
);

-- Bảng Reservations (Đặt bàn)
CREATE TABLE Reservations (
    ReservationID INT PRIMARY KEY IDENTITY,
    CustomerID INT,
    BranchID INT,
    TableID INT,
    ReservationDate DATETIME,
    NumberOfPeople INT,
    AreaPreference VARCHAR(50),  -- e.g., 'Window', 'Quiet'
    Status VARCHAR(50),  -- 'Pending', 'Confirmed', 'Cancelled'
    CreatedAt DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (CustomerID) REFERENCES Users(UserID),
    FOREIGN KEY (BranchID) REFERENCES Branches(BranchID),
    FOREIGN KEY (TableID) REFERENCES Tables(TableID)
);

-- Bảng Payments (Thanh toán)
CREATE TABLE Payments (
    PaymentID INT PRIMARY KEY IDENTITY,
    ReservationID INT,
    Amount DECIMAL(10, 2),
    PaymentMethod VARCHAR(50),  -- 'MoMo', 'Paypal', 'Cash'
    PaymentStatus VARCHAR(50),  -- 'Success', 'Failed'
    PaidAt DATETIME,
    FOREIGN KEY (ReservationID) REFERENCES Reservations(ReservationID)
);

-- Bảng Loyalty (Khách hàng thân thiết)
CREATE TABLE Loyalty (
    LoyaltyID INT PRIMARY KEY IDENTITY,
    CustomerID INT,
    Points INT,
    LastUpdated DATETIME,
    FOREIGN KEY (CustomerID) REFERENCES Users(UserID)
);

-- Bảng MenuItems (Món ăn)
CREATE TABLE MenuItems (
    ItemID INT PRIMARY KEY IDENTITY,
    Name VARCHAR(255),
    Description TEXT,
    Price DECIMAL(10, 2),
    Category VARCHAR(50),  -- e.g., 'Drink', 'Food'
    CreatedAt DATETIME DEFAULT GETDATE()
);

-- Bảng Staff (Nhân viên)
CREATE TABLE Staff (
    StaffID INT PRIMARY KEY IDENTITY,
    FullName VARCHAR(255),
    Role VARCHAR(50),  -- e.g., 'Manager', 'Waiter', 'Cashier'
    BranchID INT,
    Email VARCHAR(255),
    PhoneNumber VARCHAR(20),
    CreatedAt DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (BranchID) REFERENCES Branches(BranchID)
);

-- Bảng Notifications (Thông báo)
CREATE TABLE Notifications (
    NotificationID INT PRIMARY KEY IDENTITY,
    CustomerID INT,
    NotificationType VARCHAR(50),  -- e.g., 'Reminder', 'Confirmation', 'Alert'
    Message TEXT,
    SentAt DATETIME,
    Status VARCHAR(50),  -- 'Sent', 'Pending', 'Failed'
    FOREIGN KEY (CustomerID) REFERENCES Users(UserID)
);

-- Bảng AI_Predictions (Dự báo AI)
CREATE TABLE AI_Predictions (
    PredictionID INT PRIMARY KEY IDENTITY,
    BranchID INT,
    PredictionDate DATETIME,
    PredictedSeats INT,  -- Dự báo số khách
    HeatmapData TEXT,  -- Dữ liệu heatmap cho chi nhánh
    FOREIGN KEY (BranchID) REFERENCES Branches(BranchID)
);

-- Bảng Feedback (Phản hồi)
CREATE TABLE Feedback (
    FeedbackID INT PRIMARY KEY IDENTITY,
    CustomerID INT,
    BranchID INT,
    Rating INT,  -- Đánh giá từ 1 đến 5
    Comment TEXT,
    CreatedAt DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (CustomerID) REFERENCES Users(UserID),
    FOREIGN KEY (BranchID) REFERENCES Branches(BranchID)
);

-- Bảng Booking_Logs (Log đặt bàn)
CREATE TABLE Booking_Logs (
    LogID INT PRIMARY KEY IDENTITY,
    ActionType VARCHAR(50),  -- 'Create', 'Update', 'Cancel', 'Check-in', 'Payment'
    ActionDetails TEXT,
    ActionDate DATETIME DEFAULT GETDATE(),
    UserID INT,  -- Người thực hiện hành động
    FOREIGN KEY (UserID) REFERENCES Users(UserID)
);

-- Dữ liệu mẫu cho bảng Roles
INSERT INTO Roles (RoleName) VALUES ('Khách hàng');
INSERT INTO Roles (RoleName) VALUES ('Quản lý');
INSERT INTO Roles (RoleName) VALUES ('Nhân viên');
GO

-- Dữ liệu mẫu cho bảng Users
INSERT INTO Users (FullName, Email, PhoneNumber, PasswordHash, RoleID) 
VALUES ('Nguyễn Văn A', 'nguyenva@gmail.com', '0901234567', 'hashedpassword1', 1);  -- Khách hàng

INSERT INTO Users (FullName, Email, PhoneNumber, PasswordHash, RoleID) 
VALUES ('Lê Thị B', 'lethi.b@admin.com', '0907654321', 'hashedpassword2', 2);  -- Quản lý

INSERT INTO Users (FullName, Email, PhoneNumber, PasswordHash, RoleID) 
VALUES ('Trần Văn C', 'tranvanc@staff.com', '0912345678', 'hashedpassword3', 3);  -- Nhân viên
GO

-- Dữ liệu mẫu cho bảng Branches
INSERT INTO Branches (BranchName, Location, PhoneNumber) 
VALUES ('Cà Phê Sài Gòn', 'Quận 1, TP.HCM', '02812345678');

INSERT INTO Branches (BranchName, Location, PhoneNumber) 
VALUES ('Cà Phê Hà Nội', 'Quận Hoàn Kiếm, Hà Nội', '02498765432');
GO

-- Dữ liệu mẫu cho bảng Tables
INSERT INTO Tables (BranchID, TableNumber, Area, Status, Capacity) 
VALUES (1, 1, 'Cửa sổ', 'Available', 4);

INSERT INTO Tables (BranchID, TableNumber, Area, Status, Capacity) 
VALUES (1, 2, 'Yên tĩnh', 'Reserved', 2);

INSERT INTO Tables (BranchID, TableNumber, Area, Status, Capacity) 
VALUES (2, 1, 'Sân ngoài', 'Occupied', 6);
GO

-- Dữ liệu mẫu cho bảng Reservations
INSERT INTO Reservations (CustomerID, BranchID, TableID, ReservationDate, NumberOfPeople, AreaPreference, Status) 
VALUES (1, 1, 1, '2025-10-10 10:00:00', 2, 'Cửa sổ', 'Confirmed');

INSERT INTO Reservations (CustomerID, BranchID, TableID, ReservationDate, NumberOfPeople, AreaPreference, Status) 
VALUES (2, 1, 2, '2025-10-11 18:00:00', 4, 'Yên tĩnh', 'Pending');
GO

-- Dữ liệu mẫu cho bảng Payments
INSERT INTO Payments (ReservationID, Amount, PaymentMethod, PaymentStatus, PaidAt) 
VALUES (1, 150000, 'MoMo', 'Success', '2025-10-10 09:00:00');

INSERT INTO Payments (ReservationID, Amount, PaymentMethod, PaymentStatus, PaidAt) 
VALUES (2, 250000, 'PayPal', 'Failed', '2025-10-11 17:30:00');
GO

-- Dữ liệu mẫu cho bảng Loyalty
INSERT INTO Loyalty (CustomerID, Points, LastUpdated) 
VALUES (1, 500, '2025-10-10 10:30:00');

INSERT INTO Loyalty (CustomerID, Points, LastUpdated) 
VALUES (2, 1200, '2025-10-11 18:30:00');
GO

-- Dữ liệu mẫu cho bảng MenuItems
INSERT INTO MenuItems (Name, Description, Price, Category) 
VALUES ('Cà phê sữa đá', 'Cà phê đá với sữa đặc', 35000, 'Drink');

INSERT INTO MenuItems (Name, Description, Price, Category) 
VALUES ('Bánh mì ốp la', 'Bánh mì với trứng và xúc xích', 25000, 'Food');
GO

-- Dữ liệu mẫu cho bảng Staff
INSERT INTO Staff (FullName, Role, BranchID, Email, PhoneNumber) 
VALUES ('Nguyễn Thị D', 'Manager', 1, 'nguyenthid@cafe.com', '0909876543');

INSERT INTO Staff (FullName, Role, BranchID, Email, PhoneNumber) 
VALUES ('Trần Minh E', 'Waiter', 2, 'tranminhe@cafe.com', '0906543210');
GO

-- Dữ liệu mẫu cho bảng Notifications
INSERT INTO Notifications (CustomerID, NotificationType, Message, SentAt, Status) 
VALUES (1, 'Reminder', 'Nhắc nhở về đặt bàn tại Cà Phê Sài Gòn', '2025-10-10 08:30:00', 'Sent');

INSERT INTO Notifications (CustomerID, NotificationType, Message, SentAt, Status) 
VALUES (2, 'Confirmation', 'Đặt bàn thành công tại Cà Phê Hà Nội', '2025-10-11 17:00:00', 'Sent');
GO

-- Dữ liệu mẫu cho bảng AI_Predictions
INSERT INTO AI_Predictions (BranchID, PredictionDate, PredictedSeats, HeatmapData) 
VALUES (1, '2025-10-10 09:00:00', 50, 'Heatmap data for Sài Gòn');

INSERT INTO AI_Predictions (BranchID, PredictionDate, PredictedSeats, HeatmapData) 
VALUES (2, '2025-10-11 09:00:00', 30, 'Heatmap data for Hà Nội');
GO

-- Dữ liệu mẫu cho bảng Feedback
INSERT INTO Feedback (CustomerID, BranchID, Rating, Comment, CreatedAt) 
VALUES (1, 1, 5, 'Quán rất đẹp và nhân viên phục vụ tốt!', '2025-10-10 10:30:00');

INSERT INTO Feedback (CustomerID, BranchID, Rating, Comment, CreatedAt) 
VALUES (2, 2, 4, 'Món ăn khá ngon nhưng giá hơi cao.', '2025-10-11 18:45:00');
GO

-- Dữ liệu mẫu cho bảng Booking_Logs
INSERT INTO Booking_Logs (ActionType, ActionDetails, ActionDate, UserID) 
VALUES ('Create', 'Đặt bàn cho 2 người tại chi nhánh Cà Phê Sài Gòn, bàn cửa sổ', '2025-10-10 09:30:00', 1);

INSERT INTO Booking_Logs (ActionType, ActionDetails, ActionDate, UserID) 
VALUES ('Payment', 'Thanh toán 150.000đ qua MoMo', '2025-10-10 09:50:00', 1);
GO
