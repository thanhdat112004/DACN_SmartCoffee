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
