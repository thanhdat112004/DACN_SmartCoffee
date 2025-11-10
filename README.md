
/* ==========================================================
 SMART BOOKING CAFE — FULL CLEAN SCHEMA (SAFE CREATE)
========================================================== */

IF DB_ID(N'SmartBookingCafe') IS NULL EXEC('CREATE DATABASE SmartBookingCafe');
GO
USE SmartBookingCafe;
GO

/* ===================== 1) SECURITY & GOVERNANCE ==================== */
IF OBJECT_ID('dbo.[User]','U') IS NULL
CREATE TABLE dbo.[User](
  UserId       INT IDENTITY(1,1) PRIMARY KEY,
  Username     VARCHAR(50) NOT NULL UNIQUE,
  DisplayName  NVARCHAR(200) NOT NULL,
  Email        VARCHAR(200) NULL,
  Phone        VARCHAR(30)  NULL,
  PasswordHash VARCHAR(200) NOT NULL, -- BCrypt
  IsActive     BIT NOT NULL DEFAULT 1,
  CreatedUtc   DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);

IF OBJECT_ID('dbo.Role','U') IS NULL
CREATE TABLE dbo.Role(
  RoleId INT IDENTITY(1,1) PRIMARY KEY,
  Code   VARCHAR(50) NOT NULL UNIQUE,     -- ADMIN/MANAGER/STAFF
  Name   NVARCHAR(100) NOT NULL
);

IF OBJECT_ID('dbo.Permission','U') IS NULL
CREATE TABLE dbo.Permission(
  PermId INT IDENTITY(1,1) PRIMARY KEY,
  Code   VARCHAR(100) NOT NULL UNIQUE,
  Name   NVARCHAR(200) NOT NULL
);

IF OBJECT_ID('dbo.UserRole','U') IS NULL
CREATE TABLE dbo.UserRole(
  UserId INT NOT NULL,
  RoleId INT NOT NULL,
  PRIMARY KEY (UserId, RoleId),
  FOREIGN KEY (UserId) REFERENCES dbo.[User](UserId),
  FOREIGN KEY (RoleId) REFERENCES dbo.Role(RoleId)
);

IF OBJECT_ID('dbo.RolePermission','U') IS NULL
CREATE TABLE dbo.RolePermission(
  RoleId INT NOT NULL,
  PermId INT NOT NULL,
  PRIMARY KEY (RoleId, PermId),
  FOREIGN KEY (RoleId) REFERENCES dbo.Role(RoleId),
  FOREIGN KEY (PermId) REFERENCES dbo.Permission(PermId)
);

IF OBJECT_ID('dbo.OtpCode','U') IS NULL
CREATE TABLE dbo.OtpCode(
  Id         INT IDENTITY(1,1) PRIMARY KEY,
  UserId     INT NOT NULL,              -- dùng OTP cho user nội bộ
  Code       VARCHAR(10) NOT NULL,      -- 6 số
  Purpose    VARCHAR(20) NOT NULL,      -- REGISTER/LOGIN/RESET
  ExpireUtc  DATETIME2 NOT NULL,
  IsUsed     BIT NOT NULL DEFAULT 0,
  CreatedUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
  FOREIGN KEY (UserId) REFERENCES dbo.[User](UserId)
);

IF OBJECT_ID('dbo.AuditLog','U') IS NULL
CREATE TABLE dbo.AuditLog(
  AuditId     BIGINT IDENTITY(1,1) PRIMARY KEY,
  ActorUserId INT NULL,
  Action      VARCHAR(50) NOT NULL,     -- price_override, void_bill, stock_adjust...
  Entity      VARCHAR(50) NOT NULL,     -- Bill/Order/Voucher...
  EntityId    VARCHAR(64) NULL,
  OldValue    NVARCHAR(MAX) NULL,
  NewValue    NVARCHAR(MAX) NULL,
  IpAddress   VARCHAR(64) NULL,
  CreatedUtc  DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);

/* ===================== 2) BRANCH · LAYOUT · BOOKING ================= */
IF OBJECT_ID('dbo.Branch','U') IS NULL
CREATE TABLE dbo.Branch(
  BranchId INT IDENTITY(1,1) PRIMARY KEY,
  Code     VARCHAR(20) NOT NULL UNIQUE,
  Name     NVARCHAR(200) NOT NULL,
  Timezone VARCHAR(64) NULL,
  IsActive BIT NOT NULL DEFAULT 1
);

IF OBJECT_ID('dbo.TableZone','U') IS NULL
CREATE TABLE dbo.TableZone(
  ZoneId     INT IDENTITY(1,1) PRIMARY KEY,
  BranchId   INT NOT NULL,
  Name       NVARCHAR(100) NOT NULL,  -- Indoor/Window/Balcony
  CapacityMin INT NULL,
  CapacityMax INT NULL,
  FOREIGN KEY (BranchId) REFERENCES dbo.Branch(BranchId)
);

IF OBJECT_ID('dbo.CafeTable','U') IS NULL
CREATE TABLE dbo.CafeTable(
  TableId  INT IDENTITY(1,1) PRIMARY KEY,
  BranchId INT NOT NULL,
  ZoneId   INT NULL,
  Code     VARCHAR(20) NOT NULL,
  Seats    INT NOT NULL,
  IsActive BIT NOT NULL DEFAULT 1,
  UNIQUE (BranchId, Code),
  FOREIGN KEY (BranchId) REFERENCES dbo.Branch(BranchId),
  FOREIGN KEY (ZoneId)   REFERENCES dbo.TableZone(ZoneId)
);

IF OBJECT_ID('dbo.TableQr','U') IS NULL
CREATE TABLE dbo.TableQr(
  QrId        INT IDENTITY(1,1) PRIMARY KEY,
  BranchId    INT NOT NULL,
  TableId     INT NOT NULL,
  QrCode      VARCHAR(120) NOT NULL,  -- token/URL
  IsActive    BIT NOT NULL DEFAULT 1,
  LastRotated DATETIME2 NULL,
  FOREIGN KEY (BranchId) REFERENCES dbo.Branch(BranchId),
  FOREIGN KEY (TableId)  REFERENCES dbo.CafeTable(TableId)
);

IF OBJECT_ID('dbo.WaitlistTicket','U') IS NULL
CREATE TABLE dbo.WaitlistTicket(
  TicketId   BIGINT IDENTITY(1,1) PRIMARY KEY,
  BranchId   INT NOT NULL,
  PartySize  INT NOT NULL,
  Area       NVARCHAR(100) NULL,
  Status     VARCHAR(20) NOT NULL,    -- WAITING/NOTIFIED/HOLDING/CHECKED_IN/NO_SHOW/CANCELED
  EstReadyAt DATETIME2 NULL,
  HoldUntil  DATETIME2 NULL,
  Note       NVARCHAR(200) NULL,
  CreatedUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
  FOREIGN KEY (BranchId) REFERENCES dbo.Branch(BranchId)
);

IF OBJECT_ID('dbo.Booking','U') IS NULL
CREATE TABLE dbo.Booking(
  BookingId     BIGINT IDENTITY(1,1) PRIMARY KEY,
  BranchId      INT NOT NULL,
  TableId       INT NOT NULL,
  CustomerName  NVARCHAR(200) NOT NULL,
  Phone         VARCHAR(30) NULL,
  People        INT NOT NULL,
  StartTimeUtc  DATETIME2 NOT NULL,
  EndTimeUtc    DATETIME2 NULL,
  Status        VARCHAR(20) NOT NULL,     -- BOOKED/CHECKED_IN/NO_SHOW/CANCELED
  DepositAmount DECIMAL(12,2) NULL,
  DepositStatus VARCHAR(20) NULL,         -- PENDING/PAID/REFUNDED
  Note          NVARCHAR(200) NULL,
  CreatedUtc    DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
  FOREIGN KEY (BranchId) REFERENCES dbo.Branch(BranchId),
  FOREIGN KEY (TableId)  REFERENCES dbo.CafeTable(TableId)
);

/* ===================== 3) MENU · OPTIONS · TASTE ==================== */
IF OBJECT_ID('dbo.Category','U') IS NULL
CREATE TABLE dbo.Category(
  CategoryId INT IDENTITY(1,1) PRIMARY KEY,
  BranchId   INT NULL,                    -- null=global
  Name       NVARCHAR(100) NOT NULL,
  IsActive   BIT NOT NULL DEFAULT 1,
  UNIQUE (BranchId, Name)
);

IF OBJECT_ID('dbo.MenuItem','U') IS NULL
CREATE TABLE dbo.MenuItem(
  ItemId      INT IDENTITY(1,1) PRIMARY KEY,
  CategoryId  INT NOT NULL,
  Code        VARCHAR(30) NOT NULL UNIQUE,
  Name        NVARCHAR(200) NOT NULL,
  Price       DECIMAL(12,2) NOT NULL,
  Description NVARCHAR(400) NULL,
  -- Thuộc tính cảm quan 0..3 (TasteQuery AI):
  Sweetness   TINYINT NULL,
  SpicyLevel  TINYINT NULL,
  Sourness    TINYINT NULL,
  Bitter      TINYINT NULL,
  RichCreamy  TINYINT NULL,
  Fruity      TINYINT NULL,
  Caffeine    TINYINT NULL,
  Coolness    TINYINT NULL,
  ImageUrl    NVARCHAR(400) NULL,         -- Ảnh đại diện
  IsActive    BIT NOT NULL DEFAULT 1,
  FOREIGN KEY (CategoryId) REFERENCES dbo.Category(CategoryId)
);

IF OBJECT_ID('dbo.MenuItemMedia','U') IS NULL
CREATE TABLE dbo.MenuItemMedia(
  MediaId    BIGINT IDENTITY(1,1) PRIMARY KEY,
  ItemId     INT NOT NULL,
  Url        NVARCHAR(400) NOT NULL,
  Caption    NVARCHAR(200) NULL,
  IsPrimary  BIT NOT NULL DEFAULT 0,
  CreatedUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
  FOREIGN KEY (ItemId) REFERENCES dbo.MenuItem(ItemId)
);

IF OBJECT_ID('dbo.OptionGroup','U') IS NULL
CREATE TABLE dbo.OptionGroup(
  GroupId   INT IDENTITY(1,1) PRIMARY KEY,
  Name      NVARCHAR(100) NOT NULL,       -- Size/Ice/Sugar/Topping
  IsRequired BIT NOT NULL DEFAULT 0
);

IF OBJECT_ID('dbo.OptionItem','U') IS NULL
CREATE TABLE dbo.OptionItem(
  OptionId INT IDENTITY(1,1) PRIMARY KEY,
  GroupId  INT NOT NULL,
  Name     NVARCHAR(100) NOT NULL,        -- Large/0% Ice/50% Sugar/Pearl...
  PriceAdj DECIMAL(12,2) NOT NULL DEFAULT 0,
  FOREIGN KEY (GroupId) REFERENCES dbo.OptionGroup(GroupId)
);

IF OBJECT_ID('dbo.MenuOption','U') IS NULL
CREATE TABLE dbo.MenuOption(
  ItemId   INT NOT NULL,
  OptionId INT NOT NULL,
  PRIMARY KEY (ItemId, OptionId),
  FOREIGN KEY (ItemId)   REFERENCES dbo.MenuItem(ItemId),
  FOREIGN KEY (OptionId) REFERENCES dbo.OptionItem(OptionId)
);

/* ===================== 4) ORDERING · KDS ============================ */
IF OBJECT_ID('dbo.OrderHeader','U') IS NULL
CREATE TABLE dbo.OrderHeader(
  OrderId    BIGINT IDENTITY(1,1) PRIMARY KEY,
  BranchId   INT NOT NULL,
  TableId    INT NULL,
  BookingId  BIGINT NULL,
  CustomerId INT NULL,                    -- nếu dùng CRM login
  UserId     INT NULL,                    -- người mở order
  Channel    VARCHAR(10) NOT NULL,        -- POS/QR/WEB
  Status     VARCHAR(20) NOT NULL,        -- DRAFT/CONFIRMED/IN_PREP/READY/SERVED/CLOSED/VOID
  ReadyAtUtc DATETIME2 NULL,
  CreatedUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
  FOREIGN KEY (BranchId)  REFERENCES dbo.Branch(BranchId),
  FOREIGN KEY (TableId)   REFERENCES dbo.CafeTable(TableId),
  FOREIGN KEY (BookingId) REFERENCES dbo.Booking(BookingId),
  FOREIGN KEY (UserId)    REFERENCES dbo.[User](UserId)
);

IF OBJECT_ID('dbo.OrderItem','U') IS NULL
CREATE TABLE dbo.OrderItem(
  ItemId      BIGINT IDENTITY(1,1) PRIMARY KEY,
  OrderId     BIGINT NOT NULL,
  MenuItemId  INT NOT NULL,
  Qty         DECIMAL(12,2) NOT NULL,
  UnitPrice   DECIMAL(12,2) NOT NULL,
  Note        NVARCHAR(200) NULL,
  FOREIGN KEY (OrderId)    REFERENCES dbo.OrderHeader(OrderId),
  FOREIGN KEY (MenuItemId) REFERENCES dbo.MenuItem(ItemId)
);

IF OBJECT_ID('dbo.OrderItemOption','U') IS NULL
CREATE TABLE dbo.OrderItemOption(
  Id       BIGINT IDENTITY(1,1) PRIMARY KEY,
  ItemId   BIGINT NOT NULL,
  OptionId INT NOT NULL,
  PriceAdj DECIMAL(12,2) NOT NULL DEFAULT 0,
  FOREIGN KEY (ItemId)   REFERENCES dbo.OrderItem(ItemId),
  FOREIGN KEY (OptionId) REFERENCES dbo.OptionItem(OptionId)
);

IF OBJECT_ID('dbo.KdsTicket','U') IS NULL
CREATE TABLE dbo.KdsTicket(
  TicketId   BIGINT IDENTITY(1,1) PRIMARY KEY,
  OrderId    BIGINT NOT NULL,
  Status     VARCHAR(20) NOT NULL,        -- IN_QUEUE/IN_PREP/READY/SERVED
  EtaUtc     DATETIME2 NULL,
  CreatedUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
  FOREIGN KEY (OrderId) REFERENCES dbo.OrderHeader(OrderId)
);

/* ===================== 5) BILL · PAYMENT · VOUCHER ================== */
IF OBJECT_ID('dbo.Bill','U') IS NULL
CREATE TABLE dbo.Bill(
  BillId     BIGINT IDENTITY(1,1) PRIMARY KEY,
  OrderId    BIGINT NOT NULL,
  Subtotal   DECIMAL(12,2) NOT NULL,
  Discount   DECIMAL(12,2) NOT NULL DEFAULT 0,
  Service    DECIMAL(12,2) NOT NULL DEFAULT 0,
  Vat        DECIMAL(12,2) NOT NULL DEFAULT 0,
  Total      DECIMAL(12,2) NOT NULL,
  Status     VARCHAR(20) NOT NULL,        -- OPEN/PAID/REFUNDED/VOID
  CreatedUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
  FOREIGN KEY (OrderId) REFERENCES dbo.OrderHeader(OrderId)
);

IF OBJECT_ID('dbo.PaymentMethod','U') IS NULL
CREATE TABLE dbo.PaymentMethod(
  MethodCode VARCHAR(20) PRIMARY KEY,     -- CASH/CARD/MOMO/VNPAY
  Name       NVARCHAR(100) NOT NULL
);

IF OBJECT_ID('dbo.Payment','U') IS NULL
CREATE TABLE dbo.Payment(
  PaymentId  BIGINT IDENTITY(1,1) PRIMARY KEY,
  BillId     BIGINT NOT NULL,
  MethodCode VARCHAR(20) NOT NULL,
  Amount     DECIMAL(12,2) NOT NULL,
  PaidUtc    DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
  FOREIGN KEY (BillId)     REFERENCES dbo.Bill(BillId),
  FOREIGN KEY (MethodCode) REFERENCES dbo.PaymentMethod(MethodCode)
);

IF OBJECT_ID('dbo.Voucher','U') IS NULL
CREATE TABLE dbo.Voucher(
  VoucherId    INT IDENTITY(1,1) PRIMARY KEY,
  Code         VARCHAR(30) NOT NULL UNIQUE,
  Name         NVARCHAR(200) NOT NULL,
  Scope        VARCHAR(10) NOT NULL DEFAULT 'ORDER', -- ORDER/SKU/CHANNEL
  PercentOff   DECIMAL(5,2) NULL,
  AmountOff    DECIMAL(12,2) NULL,
  MinSubtotal  DECIMAL(12,2) NULL,
  StartUtc     DATETIME2 NULL,
  EndUtc       DATETIME2 NULL,
  Priority     INT NOT NULL DEFAULT 100,
  LimitPerUser INT NULL,
  BranchId     INT NULL,
  IsActive     BIT NOT NULL DEFAULT 1,
  FOREIGN KEY (BranchId) REFERENCES dbo.Branch(BranchId)
);

IF OBJECT_ID('dbo.VoucherApply','U') IS NULL
CREATE TABLE dbo.VoucherApply(
  Id        BIGINT IDENTITY(1,1) PRIMARY KEY,
  BillId    BIGINT NOT NULL,
  VoucherId INT NOT NULL,
  Amount    DECIMAL(12,2) NOT NULL,
  FOREIGN KEY (BillId)    REFERENCES dbo.Bill(BillId),
  FOREIGN KEY (VoucherId) REFERENCES dbo.Voucher(VoucherId)
);

/* ===================== 6) CRM · LOYALTY ============================ */
IF OBJECT_ID('dbo.Customer','U') IS NULL
CREATE TABLE dbo.Customer(
  CustomerId  INT IDENTITY(1,1) PRIMARY KEY,
  Name        NVARCHAR(200) NOT NULL,
  Email       VARCHAR(200) NULL,
  Phone       VARCHAR(30)  NULL,
  CreatedUtc  DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);

IF OBJECT_ID('dbo.MembershipTier','U') IS NULL
CREATE TABLE dbo.MembershipTier(
  TierId INT IDENTITY(1,1) PRIMARY KEY,
  Code   VARCHAR(20) NOT NULL UNIQUE,     -- SILVER/GOLD/PLAT
  Name   NVARCHAR(100) NOT NULL,
  MinPoints INT NOT NULL
);

IF OBJECT_ID('dbo.PointsLedger','U') IS NULL
CREATE TABLE dbo.PointsLedger(
  Id         BIGINT IDENTITY(1,1) PRIMARY KEY,
  CustomerId INT NOT NULL,
  Points     INT NOT NULL,
  Reason     VARCHAR(50) NOT NULL,        -- EARN/REDEEM/ADJUST
  Ref        VARCHAR(64) NULL,            -- Bill/Order
  CreatedUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
  FOREIGN KEY (CustomerId) REFERENCES dbo.Customer(CustomerId)
);

IF OBJECT_ID('dbo.Feedback','U') IS NULL
CREATE TABLE dbo.Feedback(
  Id         BIGINT IDENTITY(1,1) PRIMARY KEY,
  OrderId    BIGINT NULL,
  CustomerId INT NULL,
  Rating     TINYINT NOT NULL,            -- 1..5
  Comment    NVARCHAR(500) NULL,
  CreatedUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
  FOREIGN KEY (OrderId)    REFERENCES dbo.OrderHeader(OrderId),
  FOREIGN KEY (CustomerId) REFERENCES dbo.Customer(CustomerId)
);

/* ===================== 7) CHAT REALTIME ============================ */
IF OBJECT_ID('dbo.ChatThread','U') IS NULL
CREATE TABLE dbo.ChatThread(
  ThreadId   BIGINT IDENTITY(1,1) PRIMARY KEY,
  BranchId   INT NOT NULL,
  TableId    INT NULL,
  BookingId  BIGINT NULL,
  OrderId    BIGINT NULL,
  Status     VARCHAR(20) NOT NULL DEFAULT 'OPEN', -- OPEN/CLOSED
  CreatedUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
  FOREIGN KEY (BranchId)  REFERENCES dbo.Branch(BranchId),
  FOREIGN KEY (TableId)   REFERENCES dbo.CafeTable(TableId),
  FOREIGN KEY (BookingId) REFERENCES dbo.Booking(BookingId),
  FOREIGN KEY (OrderId)   REFERENCES dbo.OrderHeader(OrderId)
);

IF OBJECT_ID('dbo.ChatMessage','U') IS NULL
CREATE TABLE dbo.ChatMessage(
  MessageId  BIGINT IDENTITY(1,1) PRIMARY KEY,
  ThreadId   BIGINT NOT NULL,
  FromType   VARCHAR(10) NOT NULL,        -- CUSTOMER/STAFF
  FromUserId INT NULL,                    -- nếu STAFF
  Text       NVARCHAR(1000) NULL,
  ImageUrl   NVARCHAR(400) NULL,
  SentUtc    DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
  SeenUtc    DATETIME2 NULL,
  FOREIGN KEY (ThreadId)  REFERENCES dbo.ChatThread(ThreadId),
  FOREIGN KEY (FromUserId) REFERENCES dbo.[User](UserId)
);

/* ===================== 8) AI LOGS / GOVERNANCE ===================== */
IF OBJECT_ID('dbo.AiLog','U') IS NULL
CREATE TABLE dbo.AiLog(
  Id         BIGINT IDENTITY(1,1) PRIMARY KEY,
  Model      VARCHAR(50) NOT NULL,        -- BERT/TasteRanker/Forecast...
  Task       VARCHAR(50) NOT NULL,        -- Intent/Sentiment/Recommend/Forecast/Anomaly
  InputText  NVARCHAR(MAX) NULL,
  InputJson  NVARCHAR(MAX) NULL,
  OutputJson NVARCHAR(MAX) NULL,
  Score      DECIMAL(9,4) NULL,
  CreatedUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);

/* ===================== 9) HR / STAFF & SHIFTS ====================== */
IF OBJECT_ID('dbo.StaffProfile','U') IS NULL
CREATE TABLE dbo.StaffProfile(
  UserId     INT PRIMARY KEY,             -- 1-1 với User
  FullName   NVARCHAR(200) NULL,
  HireDate   DATE NULL,
  Status     VARCHAR(20) NOT NULL DEFAULT 'ACTIVE', -- ACTIVE/INACTIVE/LEAVE
  Note       NVARCHAR(200) NULL,
  CreatedUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
  FOREIGN KEY (UserId) REFERENCES dbo.[User](UserId)
);

IF OBJECT_ID('dbo.UserBranch','U') IS NULL
CREATE TABLE dbo.UserBranch(
  UserId     INT NOT NULL,
  BranchId   INT NOT NULL,
  IsPrimary  BIT NOT NULL DEFAULT 0,
  ActiveFrom DATE NULL,
  ActiveTo   DATE NULL,
  PRIMARY KEY (UserId, BranchId),
  FOREIGN KEY (UserId)   REFERENCES dbo.[User](UserId),
  FOREIGN KEY (BranchId) REFERENCES dbo.Branch(BranchId)
);

IF OBJECT_ID('dbo.UserBranchRole','U') IS NULL
CREATE TABLE dbo.UserBranchRole(
  UserId   INT NOT NULL,
  BranchId INT NOT NULL,
  RoleId   INT NOT NULL,
  PRIMARY KEY (UserId, BranchId, RoleId),
  FOREIGN KEY (UserId)   REFERENCES dbo.[User](UserId),
  FOREIGN KEY (BranchId) REFERENCES dbo.Branch(BranchId),
  FOREIGN KEY (RoleId)   REFERENCES dbo.Role(RoleId)
);

IF OBJECT_ID('dbo.ShiftTemplate','U') IS NULL
CREATE TABLE dbo.ShiftTemplate(
  TemplateId INT IDENTITY(1,1) PRIMARY KEY,
  BranchId   INT NOT NULL,
  Code       VARCHAR(20) NOT NULL,        -- MORNING/EVENING...
  Name       NVARCHAR(100) NOT NULL,
  StartTime  TIME NOT NULL,
  EndTime    TIME NOT NULL,
  UNIQUE (BranchId, Code),
  FOREIGN KEY (BranchId) REFERENCES dbo.Branch(BranchId)
);

IF OBJECT_ID('dbo.ShiftPlan','U') IS NULL
CREATE TABLE dbo.ShiftPlan(
  PlanId    BIGINT IDENTITY(1,1) PRIMARY KEY,
  BranchId  INT NOT NULL,
  UserId    INT NOT NULL,
  WorkDate  DATE NOT NULL,
  StartUtc  DATETIME2 NOT NULL,
  EndUtc    DATETIME2 NULL,
  TemplateId INT NULL,
  Status    VARCHAR(20) NOT NULL DEFAULT 'PLANNED', -- PLANNED/CONFIRMED/CANCELED
  Note      NVARCHAR(200) NULL,
  CreatedUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
  FOREIGN KEY (BranchId)  REFERENCES dbo.Branch(BranchId),
  FOREIGN KEY (UserId)    REFERENCES dbo.[User](UserId),
  FOREIGN KEY (TemplateId)REFERENCES dbo.ShiftTemplate(TemplateId)
);

IF OBJECT_ID('dbo.EmployeeShift','U') IS NULL
CREATE TABLE dbo.EmployeeShift(
  ShiftId  BIGINT IDENTITY(1,1) PRIMARY KEY,
  BranchId INT NOT NULL,
  UserId   INT NOT NULL,
  StartUtc DATETIME2 NOT NULL,
  EndUtc   DATETIME2 NULL,
  RoleCode VARCHAR(20) NULL,
  FOREIGN KEY (BranchId) REFERENCES dbo.Branch(BranchId),
  FOREIGN KEY (UserId)   REFERENCES dbo.[User](UserId)
);

IF OBJECT_ID('dbo.Timesheet','U') IS NULL
CREATE TABLE dbo.Timesheet(
  Id         BIGINT IDENTITY(1,1) PRIMARY KEY,
  UserId     INT NOT NULL,
  BranchId   INT NOT NULL,
  CheckInUtc DATETIME2 NOT NULL,
  CheckOutUtc DATETIME2 NULL,
  Method     VARCHAR(10) NOT NULL,        -- QR/OTP/GEO
  PlanId     BIGINT NULL,                 -- link kế hoạch ca
  FOREIGN KEY (UserId)   REFERENCES dbo.[User](UserId),
  FOREIGN KEY (BranchId) REFERENCES dbo.Branch(BranchId),
  FOREIGN KEY (PlanId)   REFERENCES dbo.ShiftPlan(PlanId)
);

IF OBJECT_ID('dbo.TipLedger','U') IS NULL
CREATE TABLE dbo.TipLedger(
  Id       BIGINT IDENTITY(1,1) PRIMARY KEY,
  ShiftId  BIGINT NOT NULL,
  UserId   INT NOT NULL,
  Amount   DECIMAL(12,2) NOT NULL,
  Source   VARCHAR(10) NOT NULL,          -- BILL/POOL
  FOREIGN KEY (ShiftId) REFERENCES dbo.EmployeeShift(ShiftId),
  FOREIGN KEY (UserId)  REFERENCES dbo.[User](UserId)
);

/* ===================== 10) INVENTORY & P2P ========================= */
IF OBJECT_ID('dbo.Sku','U') IS NULL
CREATE TABLE dbo.Sku(
  SkuId    INT IDENTITY(1,1) PRIMARY KEY,
  Code     VARCHAR(30) NOT NULL UNIQUE,
  Name     NVARCHAR(200) NOT NULL,
  Unit     NVARCHAR(20) NULL,             -- g/ml/piece
  IsActive BIT NOT NULL DEFAULT 1
);

IF OBJECT_ID('dbo.RecipeBom','U') IS NULL
CREATE TABLE dbo.RecipeBom(
  SkuId        INT NOT NULL,               -- thành phẩm
  IngredientId INT NOT NULL,               -- nguyên liệu
  Qty          DECIMAL(12,4) NOT NULL,
  PRIMARY KEY (SkuId, IngredientId),
  FOREIGN KEY (SkuId)        REFERENCES dbo.Sku(SkuId),
  FOREIGN KEY (IngredientId) REFERENCES dbo.Sku(SkuId)
);

IF OBJECT_ID('dbo.StockOnHand','U') IS NULL
CREATE TABLE dbo.StockOnHand(
  BranchId INT NOT NULL,
  SkuId    INT NOT NULL,
  Qty      DECIMAL(14,4) NOT NULL DEFAULT 0,
  PRIMARY KEY (BranchId, SkuId),
  FOREIGN KEY (BranchId) REFERENCES dbo.Branch(BranchId),
  FOREIGN KEY (SkuId)    REFERENCES dbo.Sku(SkuId)
);

IF OBJECT_ID('dbo.InventoryTxn','U') IS NULL
CREATE TABLE dbo.InventoryTxn(
  TxnId     BIGINT IDENTITY(1,1) PRIMARY KEY,
  BranchId  INT NOT NULL,
  SkuId     INT NOT NULL,
  Qty       DECIMAL(14,4) NOT NULL,       -- + nhập, - xuất
  Reason    VARCHAR(30) NOT NULL,         -- SALE/GRN/ADJ/TRANSFER/WASTE
  Ref       VARCHAR(64) NULL,             -- Bill/GRN/Transfer
  CreatedUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
  FOREIGN KEY (BranchId) REFERENCES dbo.Branch(BranchId),
  FOREIGN KEY (SkuId)    REFERENCES dbo.Sku(SkuId)
);

IF OBJECT_ID('dbo.Supplier','U') IS NULL
CREATE TABLE dbo.Supplier(
  SupplierId INT IDENTITY(1,1) PRIMARY KEY,
  Code       VARCHAR(30) NOT NULL UNIQUE,
  Name       NVARCHAR(200) NOT NULL,
  Contact    NVARCHAR(200) NULL
);

IF OBJECT_ID('dbo.PurchaseOrder','U') IS NULL
CREATE TABLE dbo.PurchaseOrder(
  PoId       BIGINT IDENTITY(1,1) PRIMARY KEY,
  SupplierId INT NOT NULL,
  BranchId   INT NOT NULL,
  Status     VARCHAR(20) NOT NULL,        -- DRAFT/APPROVED/CLOSED/CANCELED
  ExpectedAt DATETIME2 NULL,
  CreatedUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
  FOREIGN KEY (SupplierId) REFERENCES dbo.Supplier(SupplierId),
  FOREIGN KEY (BranchId)   REFERENCES dbo.Branch(BranchId)
);

IF OBJECT_ID('dbo.PoLine','U') IS NULL
CREATE TABLE dbo.PoLine(
  LineId  BIGINT IDENTITY(1,1) PRIMARY KEY,
  PoId    BIGINT NOT NULL,
  SkuId   INT NOT NULL,
  Qty     DECIMAL(14,4) NOT NULL,
  CostEst DECIMAL(12,2) NULL,
  FOREIGN KEY (PoId)  REFERENCES dbo.PurchaseOrder(PoId),
  FOREIGN KEY (SkuId) REFERENCES dbo.Sku(SkuId)
);

IF OBJECT_ID('dbo.GoodsReceipt','U') IS NULL
CREATE TABLE dbo.GoodsReceipt(
  GrId       BIGINT IDENTITY(1,1) PRIMARY KEY,
  PoId       BIGINT NULL,
  BranchId   INT NOT NULL,
  ReceivedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
  Status     VARCHAR(20) NOT NULL,        -- PARTIAL/COMPLETE
  FOREIGN KEY (PoId)     REFERENCES dbo.PurchaseOrder(PoId),
  FOREIGN KEY (BranchId) REFERENCES dbo.Branch(BranchId)
);

IF OBJECT_ID('dbo.GrLine','U') IS NULL
CREATE TABLE dbo.GrLine(
  LineId    BIGINT IDENTITY(1,1) PRIMARY KEY,
  GrId      BIGINT NOT NULL,
  SkuId     INT NOT NULL,
  Qty       DECIMAL(14,4) NOT NULL,
  CostActual DECIMAL(12,2) NULL,
  Lot       VARCHAR(50) NULL,
  Expiry    DATE NULL,
  FOREIGN KEY (GrId)  REFERENCES dbo.GoodsReceipt(GrId),
  FOREIGN KEY (SkuId) REFERENCES dbo.Sku(SkuId)
);

IF OBJECT_ID('dbo.StockTransfer','U') IS NULL
CREATE TABLE dbo.StockTransfer(
  TransferId BIGINT IDENTITY(1,1) PRIMARY KEY,
  FromBranch INT NOT NULL,
  ToBranch   INT NOT NULL,
  Status     VARCHAR(20) NOT NULL,        -- REQUESTED/APPROVED/IN_TRANSIT/RECEIVED
  CreatedUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
  FOREIGN KEY (FromBranch) REFERENCES dbo.Branch(BranchId),
  FOREIGN KEY (ToBranch)   REFERENCES dbo.Branch(BranchId)
);

IF OBJECT_ID('dbo.StockTransferLine','U') IS NULL
CREATE TABLE dbo.StockTransferLine(
  LineId     BIGINT IDENTITY(1,1) PRIMARY KEY,
  TransferId BIGINT NOT NULL,
  SkuId      INT NOT NULL,
  Qty        DECIMAL(14,4) NOT NULL,
  FOREIGN KEY (TransferId) REFERENCES dbo.StockTransfer(TransferId),
  FOREIGN KEY (SkuId)      REFERENCES dbo.Sku(SkuId)
);

IF OBJECT_ID('dbo.WasteTicket','U') IS NULL
CREATE TABLE dbo.WasteTicket(
  TicketId  BIGINT IDENTITY(1,1) PRIMARY KEY,
  BranchId  INT NOT NULL,
  Reason    NVARCHAR(200) NULL,
  Status    VARCHAR(20) NOT NULL,         -- DRAFT/APPROVED
  CreatedBy INT NULL,
  ApprovedBy INT NULL,
  CreatedUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
  FOREIGN KEY (BranchId) REFERENCES dbo.Branch(BranchId)
);

IF OBJECT_ID('dbo.WasteLine','U') IS NULL
CREATE TABLE dbo.WasteLine(
  LineId   BIGINT IDENTITY(1,1) PRIMARY KEY,
  TicketId BIGINT NOT NULL,
  SkuId    INT NOT NULL,
  Qty      DECIMAL(14,4) NOT NULL,
  Note     NVARCHAR(200) NULL,
  FOREIGN KEY (TicketId) REFERENCES dbo.WasteTicket(TicketId),
  FOREIGN KEY (SkuId)    REFERENCES dbo.Sku(SkuId)
);

/* ===================== 11) REPORT VIEW ============================= */
IF OBJECT_ID('dbo.v_sales_daily','V') IS NULL
EXEC('CREATE VIEW dbo.v_sales_daily AS
SELECT
  CAST(b.CreatedUtc AT TIME ZONE ''SE Asia Standard Time'' AS DATE) AS BizDate,
  SUM(b.Total) AS TotalSales,
  COUNT(*) AS Bills
FROM dbo.Bill b
WHERE b.Status = ''PAID''
GROUP BY CAST(b.CreatedUtc AT TIME ZONE ''SE Asia Standard Time'' AS DATE);');

/* ===================== 12) SEED CƠ BẢN (CHẠY 1 LẦN) ================ */
IF NOT EXISTS(SELECT 1 FROM dbo.Role)
BEGIN
  INSERT INTO dbo.Role(Code, Name) VALUES
  ('ADMIN',N'Quản trị'),('MANAGER',N'Quản lý'),('STAFF',N'Nhân viên');
END

IF NOT EXISTS(SELECT 1 FROM dbo.PaymentMethod)
BEGIN
  INSERT INTO dbo.PaymentMethod(MethodCode, Name) VALUES
  ('CASH',N'Tiền mặt'),('CARD',N'Thẻ'),('MOMO',N'Momo'),('VNPAY',N'VNPay');
END

IF NOT EXISTS(SELECT 1 FROM dbo.Branch WHERE Code='CN1')
BEGIN
  INSERT INTO dbo.Branch(Code, Name, Timezone) VALUES ('CN1',N'Chi nhánh 1','SE Asia Standard Time');
END

IF NOT EXISTS(SELECT 1 FROM dbo.TableZone)
BEGIN
  INSERT INTO dbo.TableZone(BranchId, Name)
  SELECT b.BranchId, N'Indoor' FROM dbo.Branch b WHERE b.Code='CN1'
  UNION ALL
  SELECT b.BranchId, N'Window' FROM dbo.Branch b WHERE b.Code='CN1';
END

IF NOT EXISTS(SELECT 1 FROM dbo.CafeTable)
BEGIN
  INSERT INTO dbo.CafeTable(BranchId, ZoneId, Code, Seats)
  SELECT b.BranchId, z.ZoneId, 'T1', 2 FROM dbo.Branch b JOIN dbo.TableZone z ON z.BranchId=b.BranchId AND z.Name=N'Indoor' WHERE b.Code='CN1';
  INSERT INTO dbo.CafeTable(BranchId, ZoneId, Code, Seats)
  SELECT b.BranchId, z.ZoneId, 'T2', 4 FROM dbo.Branch b JOIN dbo.TableZone z ON z.BranchId=b.BranchId AND z.Name=N'Indoor' WHERE b.Code='CN1';
  INSERT INTO dbo.CafeTable(BranchId, ZoneId, Code, Seats)
  SELECT b.BranchId, z.ZoneId, 'T3', 4 FROM dbo.Branch b JOIN dbo.TableZone z ON z.BranchId=b.BranchId AND z.Name=N'Window' WHERE b.Code='CN1';
END

IF NOT EXISTS(SELECT 1 FROM dbo.Category)
BEGIN
  INSERT INTO dbo.Category(BranchId, Name) VALUES (NULL,N'Coffee'),(NULL,N'Tea'),(NULL,N'Bakery');
END

IF NOT EXISTS(SELECT 1 FROM dbo.MenuItem WHERE Code='CF_LATTE')
BEGIN
  INSERT INTO dbo.MenuItem(CategoryId, Code, Name, Price, Sweetness, Coolness, Caffeine, ImageUrl)
  SELECT c.CategoryId,'CF_LATTE',N'Cà phê Latte',45000,1,0,2,N'https://your.cdn/cf_latte.jpg'
  FROM dbo.Category c WHERE c.Name=N'Coffee';
END

IF NOT EXISTS(SELECT 1 FROM dbo.MenuItem WHERE Code='TEA_PEACH')
BEGIN
  INSERT INTO dbo.MenuItem(CategoryId, Code, Name, Price, Sweetness, Coolness, Caffeine, ImageUrl)
  SELECT c.CategoryId,'TEA_PEACH',N'Trà đào',39000,2,2,0,N'https://your.cdn/tea_peach.jpg'
  FROM dbo.Category c WHERE c.Name=N'Tea';
END

IF NOT EXISTS(SELECT 1 FROM dbo.[User] WHERE Username='admin')
BEGIN
  INSERT INTO dbo.[User](Username, DisplayName, Email, Phone, PasswordHash, IsActive)
  VALUES ('admin',N'Quản trị hệ thống','admin@cafe.local','0900000000','HASH_ADMIN',1);
END
IF NOT EXISTS(SELECT 1 FROM dbo.UserRole ur JOIN dbo.[User] u ON u.UserId=ur.UserId JOIN dbo.Role r ON r.RoleId=ur.RoleId WHERE u.Username='admin' AND r.Code='ADMIN')
BEGIN
  INSERT INTO dbo.UserRole(UserId, RoleId)
  SELECT u.UserId, r.RoleId FROM dbo.[User] u CROSS JOIN dbo.Role r WHERE u.Username='admin' AND r.Code='ADMIN';
END
IF NOT EXISTS(SELECT 1 FROM dbo.UserBranch ub JOIN dbo.[User] u ON u.UserId=ub.UserId JOIN dbo.Branch b ON b.BranchId=ub.BranchId WHERE u.Username='admin' AND b.Code='CN1')
BEGIN
  INSERT INTO dbo.UserBranch(UserId, BranchId, IsPrimary)
  SELECT u.UserId, b.BranchId, 1 FROM dbo.[User] u CROSS JOIN dbo.Branch b WHERE u.Username='admin' AND b.Code='CN1';
END
