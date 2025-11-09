
-- ===============================================================
-- SMART BOOKING CAFÉ – SIMPLE SCHEMA (NO SCHEMA PREFIX) + SEED
-- 3 roles only: ADMIN / MANAGER / STAFF
-- ===============================================================

IF DB_ID(N'SmartBookingCafe') IS NULL
BEGIN
  EXEC('CREATE DATABASE SmartBookingCafe');
END
GO

USE SmartBookingCafe;
GO

/* ---------- Safe drop (foreign keys, tables, views) ---------- */
DECLARE @sql NVARCHAR(MAX) = N'';

-- Drop FKs
SELECT @sql = STRING_AGG('ALTER TABLE ' + QUOTENAME(OBJECT_NAME(parent_object_id)) +
                         ' DROP CONSTRAINT ' + QUOTENAME(name) + ';', CHAR(10))
FROM sys.foreign_keys
WHERE OBJECT_SCHEMA_NAME(parent_object_id) = 'dbo';
IF COALESCE(@sql,N'')<>N'' EXEC sp_executesql @sql;

-- Drop tables
SET @sql = N'';
SELECT @sql = STRING_AGG('DROP TABLE ' + QUOTENAME(name) + ';', CHAR(10))
FROM sys.tables WHERE OBJECT_SCHEMA_NAME(object_id) = 'dbo';
IF COALESCE(@sql,N'')<>N'' EXEC sp_executesql @sql;

-- Drop views
IF OBJECT_ID('v_SalesDaily') IS NOT NULL DROP VIEW v_SalesDaily;
GO

/* =====================
   TABLES (compact)
===================== */

-- Branch & Users
CREATE TABLE Branch (
  BranchId     INT IDENTITY(1,1) PRIMARY KEY,
  Code         VARCHAR(20) NOT NULL UNIQUE,
  Name         NVARCHAR(200) NOT NULL,
  Address      NVARCHAR(500) NULL,
  IsActive     BIT NOT NULL DEFAULT 1,
  CreatedUtc   DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);

CREATE TABLE [User] (
  UserId       INT IDENTITY(1,1) PRIMARY KEY,
  Username     VARCHAR(50) NOT NULL UNIQUE,
  DisplayName  NVARCHAR(200) NOT NULL,
  Email        VARCHAR(200) NULL,
  Phone        VARCHAR(50) NULL,
  PasswordHash VARCHAR(200) NOT NULL,
  IsActive     BIT NOT NULL DEFAULT 1,
  CreatedUtc   DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);

CREATE TABLE Role (
  RoleId INT IDENTITY(1,1) PRIMARY KEY,
  Code   VARCHAR(50) NOT NULL UNIQUE,  -- ADMIN / MANAGER / STAFF
  Name   NVARCHAR(100) NOT NULL
);

CREATE TABLE Permission (
  PermId INT IDENTITY(1,1) PRIMARY KEY,
  Code   VARCHAR(100) NOT NULL UNIQUE,
  Name   NVARCHAR(200) NOT NULL
);

CREATE TABLE UserRole (
  UserId INT NOT NULL,
  RoleId INT NOT NULL,
  PRIMARY KEY (UserId, RoleId),
  FOREIGN KEY (UserId) REFERENCES [User](UserId),
  FOREIGN KEY (RoleId) REFERENCES Role(RoleId)
);

CREATE TABLE RolePermission (
  RoleId INT NOT NULL,
  PermId INT NOT NULL,
  PRIMARY KEY (RoleId, PermId),
  FOREIGN KEY (RoleId) REFERENCES Role(RoleId),
  FOREIGN KEY (PermId) REFERENCES Permission(PermId)
);

-- Customer & Loyalty
CREATE TABLE Customer (
  CustomerId INT IDENTITY(1,1) PRIMARY KEY,
  Phone      VARCHAR(30) UNIQUE,
  Name       NVARCHAR(200) NOT NULL,
  Email      VARCHAR(200) NULL,
  TierCode   VARCHAR(20) NULL,
  CreatedUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);

CREATE TABLE MembershipTier (
  TierCode  VARCHAR(20) PRIMARY KEY,
  Name      NVARCHAR(100) NOT NULL,
  MinPoints INT NOT NULL DEFAULT 0
);

CREATE TABLE PointsLedger (
  Id        BIGINT IDENTITY(1,1) PRIMARY KEY,
  CustomerId INT NOT NULL,
  Points    INT NOT NULL,
  Reason    NVARCHAR(200) NULL,
  CreatedUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
  FOREIGN KEY (CustomerId) REFERENCES Customer(CustomerId)
);

-- Tables, QR, Waitlist
CREATE TABLE CafeTable (
  TableId   INT IDENTITY(1,1) PRIMARY KEY,
  BranchId  INT NOT NULL,
  Code      VARCHAR(20) NOT NULL,
  Seats     INT NOT NULL,
  ZoneName  NVARCHAR(100) NULL,
  UNIQUE (BranchId, Code),
  FOREIGN KEY (BranchId) REFERENCES Branch(BranchId)
);

CREATE TABLE TableQr (
  Id        INT IDENTITY(1,1) PRIMARY KEY,
  TableId   INT NOT NULL,
  QrToken   VARCHAR(64) NOT NULL UNIQUE,
  IsActive  BIT NOT NULL DEFAULT 1,
  LastRotate DATETIME2 NULL,
  FOREIGN KEY (TableId) REFERENCES CafeTable(TableId)
);

CREATE TABLE WaitlistTicket (
  TicketId  BIGINT IDENTITY(1,1) PRIMARY KEY,
  BranchId  INT NOT NULL,
  CustomerId INT NULL,
  PartySize INT NOT NULL,
  AreaName  NVARCHAR(100) NULL,
  Status    VARCHAR(20) NOT NULL, -- WAITING/NOTIFIED/HOLDING/CHECKED_IN/NO_SHOW/CANCELED
  EstReady  DATETIME2 NULL,
  HoldUntil DATETIME2 NULL,
  Note      NVARCHAR(200) NULL,
  CreatedUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
  FOREIGN KEY (BranchId) REFERENCES Branch(BranchId),
  FOREIGN KEY (CustomerId) REFERENCES Customer(CustomerId)
);

-- Menu & Inventory
CREATE TABLE Sku (
  SkuId       INT IDENTITY(1,1) PRIMARY KEY,
  Code        VARCHAR(30) UNIQUE NOT NULL,
  Name        NVARCHAR(200) NOT NULL,
  Category    NVARCHAR(100) NOT NULL,
  Price       DECIMAL(12,2) NOT NULL,
  IsIngredient BIT NOT NULL DEFAULT 0,
  Unit        NVARCHAR(20) NULL,
  IsActive    BIT NOT NULL DEFAULT 1
);

CREATE TABLE RecipeBom (
  SkuId        INT NOT NULL,
  IngredientId INT NOT NULL,
  Qty          DECIMAL(12,4) NOT NULL,
  PRIMARY KEY (SkuId, IngredientId),
  FOREIGN KEY (SkuId) REFERENCES Sku(SkuId),
  FOREIGN KEY (IngredientId) REFERENCES Sku(SkuId)
);

CREATE TABLE StockOnHand (
  BranchId INT NOT NULL,
  SkuId    INT NOT NULL,
  Qty      DECIMAL(14,4) NOT NULL DEFAULT 0,
  PRIMARY KEY (BranchId, SkuId),
  FOREIGN KEY (BranchId) REFERENCES Branch(BranchId),
  FOREIGN KEY (SkuId) REFERENCES Sku(SkuId)
);

CREATE TABLE InventoryTxn (
  TxnId     BIGINT IDENTITY(1,1) PRIMARY KEY,
  BranchId  INT NOT NULL,
  SkuId     INT NOT NULL,
  QtyChange DECIMAL(14,4) NOT NULL,
  Reason    VARCHAR(30) NOT NULL, -- GRN/ORDER/WASTE/ADJUST/TRANSFER_OUT/TRANSFER_IN
  RefType   VARCHAR(30) NULL,
  RefId     VARCHAR(50) NULL,
  CreatedUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
  FOREIGN KEY (BranchId) REFERENCES Branch(BranchId),
  FOREIGN KEY (SkuId) REFERENCES Sku(SkuId)
);

-- Purchasing
CREATE TABLE Supplier (
  SupplierId INT IDENTITY(1,1) PRIMARY KEY,
  Code       VARCHAR(30) UNIQUE NOT NULL,
  Name       NVARCHAR(200) NOT NULL,
  Contact    NVARCHAR(200) NULL
);

CREATE TABLE PurchaseOrder (
  PoId     INT IDENTITY(1,1) PRIMARY KEY,
  SupplierId INT NOT NULL,
  BranchId INT NOT NULL,
  Status   VARCHAR(20) NOT NULL, -- DRAFT/APPROVED/CLOSED/CANCELED
  Expected DATETIME2 NULL,
  TotalEst DECIMAL(14,2) NULL,
  CreatedUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
  FOREIGN KEY (SupplierId) REFERENCES Supplier(SupplierId),
  FOREIGN KEY (BranchId) REFERENCES Branch(BranchId)
);

CREATE TABLE PoLine (
  LineId  INT IDENTITY(1,1) PRIMARY KEY,
  PoId    INT NOT NULL,
  SkuId   INT NOT NULL,
  Qty     DECIMAL(14,4) NOT NULL,
  CostEst DECIMAL(14,4) NOT NULL,
  FOREIGN KEY (PoId) REFERENCES PurchaseOrder(PoId),
  FOREIGN KEY (SkuId) REFERENCES Sku(SkuId)
);

CREATE TABLE GoodsReceipt (
  GrId    INT IDENTITY(1,1) PRIMARY KEY,
  PoId    INT NULL,
  BranchId INT NOT NULL,
  Status  VARCHAR(20) NOT NULL, -- PARTIAL/COMPLETE/CANCELED
  Received DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
  FOREIGN KEY (PoId) REFERENCES PurchaseOrder(PoId),
  FOREIGN KEY (BranchId) REFERENCES Branch(BranchId)
);

CREATE TABLE GrLine (
  LineId INT IDENTITY(1,1) PRIMARY KEY,
  GrId   INT NOT NULL,
  SkuId  INT NOT NULL,
  Qty    DECIMAL(14,4) NOT NULL,
  Cost   DECIMAL(14,4) NOT NULL,
  Lot    NVARCHAR(50) NULL,
  Expiry DATE NULL,
  FOREIGN KEY (GrId) REFERENCES GoodsReceipt(GrId),
  FOREIGN KEY (SkuId) REFERENCES Sku(SkuId)
);

-- Transfer & Waste
CREATE TABLE StockTransfer (
  StId      INT IDENTITY(1,1) PRIMARY KEY,
  FromBranch INT NOT NULL,
  ToBranch   INT NOT NULL,
  Status    VARCHAR(20) NOT NULL, -- REQUESTED/APPROVED/IN_TRANSIT/RECEIVED
  CreatedUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
  FOREIGN KEY (FromBranch) REFERENCES Branch(BranchId),
  FOREIGN KEY (ToBranch)   REFERENCES Branch(BranchId)
);

CREATE TABLE StockTransferLine (
  LineId INT IDENTITY(1,1) PRIMARY KEY,
  StId   INT NOT NULL,
  SkuId  INT NOT NULL,
  Qty    DECIMAL(14,4) NOT NULL,
  FOREIGN KEY (StId) REFERENCES StockTransfer(StId),
  FOREIGN KEY (SkuId) REFERENCES Sku(SkuId)
);

CREATE TABLE WasteTicket (
  TicketId INT IDENTITY(1,1) PRIMARY KEY,
  BranchId INT NOT NULL,
  Reason   NVARCHAR(200) NOT NULL,
  Status   VARCHAR(20) NOT NULL DEFAULT 'OPEN',
  CreatedBy INT NOT NULL,
  ApprovedBy INT NULL,
  CreatedUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
  FOREIGN KEY (BranchId) REFERENCES Branch(BranchId)
);

CREATE TABLE WasteLine (
  LineId  INT IDENTITY(1,1) PRIMARY KEY,
  TicketId INT NOT NULL,
  SkuId   INT NOT NULL,
  Qty     DECIMAL(14,4) NOT NULL,
  FOREIGN KEY (TicketId) REFERENCES WasteTicket(TicketId),
  FOREIGN KEY (SkuId) REFERENCES Sku(SkuId)
);

-- POS
CREATE TABLE OrderHeader (
  OrderId   BIGINT IDENTITY(1,1) PRIMARY KEY,
  BranchId  INT NOT NULL,
  TableId   INT NULL,
  CustomerId INT NULL,
  Channel   VARCHAR(10) NOT NULL, -- POS/QR/WEB/DELIVERY
  Status    VARCHAR(20) NOT NULL, -- DRAFT/CONFIRMED/IN_PREP/READY/SERVED/CLOSED/VOID
  CreatedUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
  FOREIGN KEY (BranchId) REFERENCES Branch(BranchId),
  FOREIGN KEY (TableId) REFERENCES CafeTable(TableId),
  FOREIGN KEY (CustomerId) REFERENCES Customer(CustomerId)
);

CREATE TABLE OrderItem (
  ItemId    BIGINT IDENTITY(1,1) PRIMARY KEY,
  OrderId   BIGINT NOT NULL,
  SkuId     INT NOT NULL,
  Qty       DECIMAL(12,2) NOT NULL,
  UnitPrice DECIMAL(12,2) NOT NULL,
  Note      NVARCHAR(200) NULL,
  FOREIGN KEY (OrderId) REFERENCES OrderHeader(OrderId),
  FOREIGN KEY (SkuId) REFERENCES Sku(SkuId)
);

CREATE TABLE Bill (
  BillId    BIGINT IDENTITY(1,1) PRIMARY KEY,
  OrderId   BIGINT NOT NULL,
  Subtotal  DECIMAL(12,2) NOT NULL,
  Discount  DECIMAL(12,2) NOT NULL DEFAULT 0,
  Service   DECIMAL(12,2) NOT NULL DEFAULT 0,
  Vat       DECIMAL(12,2) NOT NULL DEFAULT 0,
  Total     DECIMAL(12,2) NOT NULL,
  Status    VARCHAR(20) NOT NULL, -- OPEN/PAID/REFUNDED/VOID
  CreatedUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
  FOREIGN KEY (OrderId) REFERENCES OrderHeader(OrderId)
);

CREATE TABLE PaymentMethod (
  MethodCode VARCHAR(20) PRIMARY KEY, -- CASH/CARD/MOMO/VNPAY
  Name       NVARCHAR(100) NOT NULL
);

CREATE TABLE OrderPayment (
  PaymentId BIGINT IDENTITY(1,1) PRIMARY KEY,
  BillId    BIGINT NOT NULL,
  MethodCode VARCHAR(20) NOT NULL,
  Amount    DECIMAL(12,2) NOT NULL,
  PaidUtc   DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
  FOREIGN KEY (BillId) REFERENCES Bill(BillId),
  FOREIGN KEY (MethodCode) REFERENCES PaymentMethod(MethodCode)
);

-- Voucher
CREATE TABLE Voucher (
  VoucherId   INT IDENTITY(1,1) PRIMARY KEY,
  Code        VARCHAR(30) UNIQUE NOT NULL,
  Name        NVARCHAR(200) NOT NULL,
  Scope       VARCHAR(20) NOT NULL, -- ORDER/SKU/CHANNEL
  Priority    INT NOT NULL DEFAULT 100,
  PercentOff  DECIMAL(5,2) NULL,
  AmountOff   DECIMAL(12,2) NULL,
  MinSubtotal DECIMAL(12,2) NULL,
  StartUtc    DATETIME2 NULL,
  EndUtc      DATETIME2 NULL,
  MaxUsage    INT NULL,
  UsageCount  INT NOT NULL DEFAULT 0,
  IsActive    BIT NOT NULL DEFAULT 1
);

CREATE TABLE VoucherApplication (
  Id        BIGINT IDENTITY(1,1) PRIMARY KEY,
  BillId    BIGINT NOT NULL,
  VoucherId INT NOT NULL,
  Amount    DECIMAL(12,2) NOT NULL,
  FOREIGN KEY (BillId) REFERENCES Bill(BillId),
  FOREIGN KEY (VoucherId) REFERENCES Voucher(VoucherId)
);

-- Finance & HR & Devices (compact)
CREATE TABLE PaymentReconciliation (
  ReconId   INT IDENTITY(1,1) PRIMARY KEY,
  BranchId  INT NOT NULL,
  BizDate   DATE NOT NULL,
  MethodCode VARCHAR(20) NOT NULL,
  SystemAmt DECIMAL(12,2) NOT NULL,
  CountedAmt DECIMAL(12,2) NOT NULL,
  Diff      DECIMAL(12,2) NOT NULL,
  Note      NVARCHAR(200) NULL,
  CreatedUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
  FOREIGN KEY (BranchId) REFERENCES Branch(BranchId)
);

CREATE TABLE CashMovement (
  MovId     INT IDENTITY(1,1) PRIMARY KEY,
  BranchId  INT NOT NULL,
  Type      VARCHAR(10) NOT NULL, -- IN/OUT
  Amount    DECIMAL(12,2) NOT NULL,
  Reason    NVARCHAR(200) NULL,
  Ref       NVARCHAR(100) NULL,
  CreatedUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
  FOREIGN KEY (BranchId) REFERENCES Branch(BranchId)
);

CREATE TABLE EmployeeShift (
  ShiftId INT IDENTITY(1,1) PRIMARY KEY,
  BranchId INT NOT NULL,
  UserId  INT NOT NULL,
  RoleCode VARCHAR(50) NOT NULL, -- ADMIN/MANAGER/STAFF
  StartUtc DATETIME2 NOT NULL,
  EndUtc   DATETIME2 NULL,
  FOREIGN KEY (BranchId) REFERENCES Branch(BranchId),
  FOREIGN KEY (UserId) REFERENCES [User](UserId)
);

CREATE TABLE Timesheet (
  TimesheetId INT IDENTITY(1,1) PRIMARY KEY,
  BranchId INT NOT NULL,
  UserId  INT NOT NULL,
  CheckInUtc  DATETIME2 NOT NULL,
  CheckOutUtc DATETIME2 NULL,
  Method  VARCHAR(10) NOT NULL, -- QR/OTP/GEO
  FOREIGN KEY (BranchId) REFERENCES Branch(BranchId),
  FOREIGN KEY (UserId) REFERENCES [User](UserId)
);

CREATE TABLE TipLedger (
  Id      INT IDENTITY(1,1) PRIMARY KEY,
  ShiftId INT NOT NULL,
  UserId  INT NOT NULL,
  Amount  DECIMAL(12,2) NOT NULL,
  Source  VARCHAR(10) NOT NULL, -- BILL/POOL
  CreatedUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
  FOREIGN KEY (ShiftId) REFERENCES EmployeeShift(ShiftId),
  FOREIGN KEY (UserId) REFERENCES [User](UserId)
);

CREATE TABLE DevicePrinter (
  PrinterId INT IDENTITY(1,1) PRIMARY KEY,
  BranchId  INT NOT NULL,
  Type      VARCHAR(20) NOT NULL, -- KDS/TICKET/BAR
  Ip        VARCHAR(50) NOT NULL,
  IsActive  BIT NOT NULL DEFAULT 1,
  FOREIGN KEY (BranchId) REFERENCES Branch(BranchId)
);

CREATE TABLE AuditLog (
  AuditId  BIGINT IDENTITY(1,1) PRIMARY KEY,
  ActorId  INT NULL,
  Action   VARCHAR(50) NOT NULL,
  Entity   VARCHAR(50) NOT NULL,
  EntityId VARCHAR(50) NOT NULL,
  OldValue NVARCHAR(MAX) NULL,
  NewValue NVARCHAR(MAX) NULL,
  Ip       VARCHAR(64) NULL,
  CreatedUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);

CREATE TABLE SyncQueue (
  Id       BIGINT IDENTITY(1,1) PRIMARY KEY,
  Entity   VARCHAR(50) NOT NULL,
  Payload  NVARCHAR(MAX) NOT NULL,
  Status   VARCHAR(20) NOT NULL DEFAULT 'PENDING',
  RetryCnt INT NOT NULL DEFAULT 0,
  CreatedUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);

CREATE TABLE ReportSnapshot (
  Id       INT IDENTITY(1,1) PRIMARY KEY,
  BranchId INT NOT NULL,
  Type     VARCHAR(5) NOT NULL, -- X/Z
  Payload  NVARCHAR(MAX) NOT NULL,
  CreatedUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
  FOREIGN KEY (BranchId) REFERENCES Branch(BranchId)
);

/* =====================
   SEED (compact)
===================== */

-- Payment methods
INSERT INTO PaymentMethod(MethodCode, Name) VALUES
('CASH',N'Tiền mặt'),('CARD',N'Thẻ'),('MOMO',N'Momo'),('VNPAY',N'VNPay');

-- Branches
INSERT INTO Branch(Code, Name, Address)
VALUES ('HCM01', N'Chi nhánh Quận 1', N'123 Đồng Khởi, Q1, TP.HCM'),
       ('HN01',  N'Chi nhánh Hoàn Kiếm', N'45 Tràng Tiền, Hoàn Kiếm, HN');

-- Users (5 accounts)
INSERT INTO [User](Username, DisplayName, Email, Phone, PasswordHash) VALUES
('admin',   N'Quản trị hệ thống','admin@cafe.local','0900000000','HASH_ADMIN'),
('manager', N'Quản lý cửa hàng','manager@cafe.local','0900000001','HASH_MANAGER'),
('cashier', N'Nhân viên thu ngân','cashier@cafe.local','0900000002','HASH_CASHIER'),
('barista', N'Nhân viên pha chế','barista@cafe.local','0900000003','HASH_BARISTA'),
('waiter',  N'Nhân viên phục vụ','waiter@cafe.local','0900000004','HASH_WAITER');

-- Roles (ONLY 3)
INSERT INTO Role(Code, Name) VALUES
('ADMIN',N'Quản trị'),('MANAGER',N'Quản lý'),('STAFF',N'Nhân viên');

-- Permissions (sample)
INSERT INTO Permission(Code, Name) VALUES
('POS_OPEN',N'Mở đơn tại POS'),
('POS_PAY',N'Thanh toán hóa đơn'),
('ORDER_VOID',N'Void đơn'),
('PRICE_OVERRIDE',N'Đổi giá'),
('DISCOUNT_MANUAL',N'Giảm giá thủ công'),
('VIEW_REPORT',N'Xem báo cáo'),
('INVENTORY_EDIT',N'Điều chỉnh tồn kho'),
('PO_APPROVE',N'Duyệt PO');

-- Role-Permission
INSERT INTO RolePermission(RoleId, PermId)
SELECT r.RoleId, p.PermId FROM Role r CROSS JOIN Permission p WHERE r.Code='ADMIN';
INSERT INTO RolePermission(RoleId, PermId)
SELECT r.RoleId, p.PermId FROM Role r JOIN Permission p ON p.Code IN ('POS_OPEN','POS_PAY','ORDER_VOID','VIEW_REPORT','INVENTORY_EDIT','PO_APPROVE')
WHERE r.Code='MANAGER';
INSERT INTO RolePermission(RoleId, PermId)
SELECT r.RoleId, p.PermId FROM Role r JOIN Permission p ON p.Code IN ('POS_OPEN','POS_PAY','VIEW_REPORT')
WHERE r.Code='STAFF';

-- User-Role mapping
INSERT INTO UserRole(UserId, RoleId)
SELECT u.UserId, r.RoleId FROM [User] u JOIN Role r ON u.Username='admin' AND r.Code='ADMIN';
INSERT INTO UserRole(UserId, RoleId)
SELECT u.UserId, r.RoleId FROM [User] u JOIN Role r ON u.Username='manager' AND r.Code='MANAGER';
INSERT INTO UserRole(UserId, RoleId)
SELECT u.UserId, r.RoleId FROM [User] u JOIN Role r ON u.Username IN ('cashier','barista','waiter') AND r.Code='STAFF';

-- Membership tiers & customers
INSERT INTO MembershipTier(TierCode, Name, MinPoints) VALUES
('SILVER',N'Silver',0),('GOLD',N'Gold',500),('PLAT',N'Platinum',1500);

INSERT INTO Customer(Phone, Name, Email, TierCode) VALUES
('0901111111',N'Nguyễn An','an@example.com','SILVER'),
('0902222222',N'Trần Bình','binh@example.com','GOLD');

-- Tables & QR
INSERT INTO CafeTable(BranchId, Code, Seats, ZoneName) VALUES
(1,'T1',2,N'Indoor'),(1,'T2',4,N'Indoor'),(1,'T3',4,N'Window'),
(2,'H1',2,N'Indoor'),(2,'H2',4,N'Balcony');

INSERT INTO TableQr(TableId, QrToken, IsActive) VALUES
(1,'QR_T1_TOKEN',1),(2,'QR_T2_TOKEN',1),(3,'QR_T3_TOKEN',1),(4,'QR_H1_TOKEN',1),(5,'QR_H2_TOKEN',1);

-- SKUs & BOM
INSERT INTO Sku(Code,Name,Category,Price,IsIngredient,Unit) VALUES
('CF_LATTE',N'Cà phê Latte',N'Coffee',45000,0,N'ly'),
('CF_CAPPU',N'Cappuccino',N'Coffee',49000,0,N'ly'),
('TEA_PEACH',N'Trà đào',N'Trà',39000,0,N'ly'),
('ING_ESP',N'Espresso Shot',N'Nguyên liệu',0,1,N'shot'),
('ING_MILK',N'Sữa tươi',N'Nguyên liệu',0,1,N'ml'),
('ING_TEA',N'Trà đen',N'Nguyên liệu',0,1,N'ml'),
('ING_PEACH',N'Mứt đào',N'Nguyên liệu',0,1,N'g'),
('ING_SUGAR',N'Đường',N'Nguyên liệu',0,1,N'g');

INSERT INTO RecipeBom(SkuId,IngredientId,Qty)
SELECT s1.SkuId,s2.SkuId,1 FROM Sku s1 JOIN Sku s2 ON s1.Code='CF_LATTE' AND s2.Code='ING_ESP'
UNION ALL SELECT s1.SkuId,s2.SkuId,180 FROM Sku s1 JOIN Sku s2 ON s1.Code='CF_LATTE' AND s2.Code='ING_MILK';
INSERT INTO RecipeBom(SkuId,IngredientId,Qty)
SELECT s1.SkuId,s2.SkuId,1 FROM Sku s1 JOIN Sku s2 ON s1.Code='CF_CAPPU' AND s2.Code='ING_ESP'
UNION ALL SELECT s1.SkuId,s2.SkuId,150 FROM Sku s1 JOIN Sku s2 ON s1.Code='CF_CAPPU' AND s2.Code='ING_MILK';
INSERT INTO RecipeBom(SkuId,IngredientId,Qty)
SELECT s1.SkuId,s2.SkuId,200 FROM Sku s1 JOIN Sku s2 ON s1.Code='TEA_PEACH' AND s2.Code='ING_TEA'
UNION ALL SELECT s1.SkuId,s2.SkuId,30 FROM Sku s1 JOIN Sku s2 ON s1.Code='TEA_PEACH' AND s2.Code='ING_PEACH'
UNION ALL SELECT s1.SkuId,s2.SkuId,15 FROM Sku s1 JOIN Sku s2 ON s1.Code='TEA_PEACH' AND s2.Code='ING_SUGAR';

-- Initial stock (ingredients only)
INSERT INTO StockOnHand(BranchId, SkuId, Qty)
SELECT 1, SkuId, 1000 FROM Sku WHERE IsIngredient=1;
INSERT INTO StockOnHand(BranchId, SkuId, Qty)
SELECT 2, SkuId, 800 FROM Sku WHERE IsIngredient=1;

-- Suppliers + PO/GRN
INSERT INTO Supplier(Code,Name,Contact) VALUES
('SUP01',N'Nhà cung cấp A',N'0938 000 111'),('SUP02',N'Nhà cung cấp B',N'0938 000 222');

INSERT INTO PurchaseOrder(SupplierId, BranchId, Status, Expected, TotalEst)
VALUES (1,1,'APPROVED', DATEADD(day,1,SYSUTCDATETIME()), 500000);

INSERT INTO PoLine(PoId,SkuId,Qty,CostEst)
SELECT 1, s.SkuId, 500, 3000 FROM Sku s WHERE s.Code='ING_MILK'
UNION ALL SELECT 1, s.SkuId, 400, 2500 FROM Sku s WHERE s.Code='ING_TEA';

INSERT INTO GoodsReceipt(PoId, BranchId, Status) VALUES (1,1,'COMPLETE');
INSERT INTO GrLine(GrId,SkuId,Qty,Cost,Lot,Expiry)
SELECT 1, s.SkuId, 500, 2950, 'LOT-MILK-01', DATEADD(day,10,GETUTCDATE()) FROM Sku s WHERE s.Code='ING_MILK'
UNION ALL SELECT 1, s.SkuId, 400, 2450, 'LOT-TEA-01', DATEADD(day,180,GETUTCDATE()) FROM Sku s WHERE s.Code='ING_TEA';

-- Inventory from GRN
INSERT INTO InventoryTxn(BranchId,SkuId,QtyChange,Reason,RefType,RefId)
SELECT 1, s.SkuId, 500, 'GRN','GR','1' FROM Sku s WHERE s.Code='ING_MILK'
UNION ALL SELECT 1, s.SkuId, 400, 'GRN','GR','1' FROM Sku s WHERE s.Code='ING_TEA';

-- Transfer
INSERT INTO StockTransfer(FromBranch,ToBranch,Status) VALUES (1,2,'RECEIVED');
INSERT INTO StockTransferLine(StId,SkuId,Qty)
SELECT 1, SkuId, 100 FROM Sku WHERE Code='ING_TEA';
INSERT INTO InventoryTxn(BranchId,SkuId,QtyChange,Reason,RefType,RefId)
SELECT 1, SkuId, -100, 'TRANSFER_OUT','ST','1' FROM Sku WHERE Code='ING_TEA';
INSERT INTO InventoryTxn(BranchId,SkuId,QtyChange,Reason,RefType,RefId)
SELECT 2, SkuId, 100, 'TRANSFER_IN','ST','1' FROM Sku WHERE Code='ING_TEA';

-- Waste
INSERT INTO WasteTicket(BranchId,Reason,Status,CreatedBy) VALUES (1,N'Hao hụt sữa cuối ngày','APPROVED',2);
INSERT INTO WasteLine(TicketId,SkuId,Qty)
SELECT 1, SkuId, 50 FROM Sku WHERE Code='ING_MILK';
INSERT INTO InventoryTxn(BranchId,SkuId,QtyChange,Reason,RefType,RefId)
SELECT 1, SkuId, -50, 'WASTE','WT','1' FROM Sku WHERE Code='ING_MILK';

-- Voucher
INSERT INTO Voucher(Code,Name,Scope,Priority,PercentOff,MinSubtotal,StartUtc,EndUtc,MaxUsage)
VALUES ('WELCOME10',N'Giảm 10% đơn đầu','ORDER',10,10.00,50000,DATEADD(day,-1,SYSUTCDATETIME()),DATEADD(day,30,SYSUTCDATETIME()),1000);

-- Demo POS flow
DECLARE @OrderId BIGINT;
INSERT INTO OrderHeader(BranchId,TableId,CustomerId,Channel,Status)
SELECT 1, t.TableId, 1, 'POS','CONFIRMED' FROM CafeTable t WHERE t.Code='T1' AND t.BranchId=1;
SET @OrderId = SCOPE_IDENTITY();

INSERT INTO OrderItem(OrderId,SkuId,Qty,UnitPrice)
SELECT @OrderId, SkuId, 1, Price FROM Sku WHERE Code='CF_LATTE';
INSERT INTO OrderItem(OrderId,SkuId,Qty,UnitPrice)
SELECT @OrderId, SkuId, 1, Price FROM Sku WHERE Code='TEA_PEACH';

-- consume inventory by recipe (simplified)
INSERT INTO InventoryTxn(BranchId,SkuId,QtyChange,Reason,RefType,RefId)
SELECT 1, i.SkuId, -rb.Qty, 'ORDER','ORDER', CAST(@OrderId AS VARCHAR(50))
FROM Sku f JOIN RecipeBom rb ON rb.SkuId=f.SkuId JOIN Sku i ON i.SkuId=rb.IngredientId
WHERE f.Code IN ('CF_LATTE','TEA_PEACH');

DECLARE @Subtotal DECIMAL(12,2) = (SELECT SUM(Qty*UnitPrice) FROM OrderItem WHERE OrderId=@OrderId);
DECLARE @Discount DECIMAL(12,2) = ROUND(@Subtotal*0.10,0); -- WELCOME10 demo
DECLARE @Service  DECIMAL(12,2) = ROUND(@Subtotal*0.05,0);
DECLARE @Vat      DECIMAL(12,2) = ROUND((@Subtotal-@Discount+@Service)*0.08,0);
DECLARE @Total    DECIMAL(12,2) = @Subtotal-@Discount+@Service+@Vat;

INSERT INTO Bill(OrderId,Subtotal,Discount,Service,Vat,Total,Status)
VALUES (@OrderId,@Subtotal,@Discount,@Service,@Vat,@Total,'OPEN');

DECLARE @BillId BIGINT = SCOPE_IDENTITY();

INSERT INTO VoucherApplication(BillId,VoucherId,Amount)
SELECT @BillId, v.VoucherId, @Discount FROM Voucher v WHERE v.Code='WELCOME10';

INSERT INTO OrderPayment(BillId,MethodCode,Amount) VALUES (@BillId,'CASH',@Total);
UPDATE Bill SET Status='PAID' WHERE BillId=@BillId;
UPDATE OrderHeader SET Status='CLOSED' WHERE OrderId=@OrderId;

-- Reconciliation
INSERT INTO PaymentReconciliation(BranchId,BizDate,MethodCode,SystemAmt,CountedAmt,Diff,Note)
VALUES (1, CAST(GETUTCDATE() AS DATE), 'CASH', @Total, @Total, 0, N'Khớp tiền mặt');

-- HR demo
INSERT INTO EmployeeShift(BranchId,UserId,RoleCode,StartUtc)
SELECT 1, u.UserId, 'STAFF', DATEADD(hour,-2,SYSUTCDATETIME()) FROM [User] u WHERE Username='cashier';
INSERT INTO Timesheet(BranchId,UserId,CheckInUtc,Method)
SELECT 1, u.UserId, DATEADD(hour,-2,SYSUTCDATETIME()), 'QR' FROM [User] u WHERE Username='cashier';
UPDATE Timesheet SET CheckOutUtc = SYSUTCDATETIME() WHERE UserId = (SELECT UserId FROM [User] WHERE Username='cashier');

-- Devices
INSERT INTO DevicePrinter(BranchId,Type,Ip) VALUES (1,'KDS','192.168.1.101'),(1,'TICKET','192.168.1.102');

-- View
GO
CREATE VIEW v_SalesDaily AS
SELECT
  CAST(b.CreatedUtc AT TIME ZONE 'SE Asia Standard Time' AS DATE) AS BizDate,
  br.Code AS BranchCode,
  SUM(b.Total) TotalSales,
  COUNT(*) Bills
FROM Bill b
JOIN OrderHeader o ON o.OrderId=b.OrderId
JOIN Branch br ON br.BranchId=o.BranchId
WHERE b.Status='PAID'
GROUP BY CAST(b.CreatedUtc AT TIME ZONE 'Se Asia Standard Time' AS DATE), br.Code;
GO
