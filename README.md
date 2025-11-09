
-- ======================================================================
-- SMART BOOKING CAFÉ – Schema & Seed (V2, safe DROP)
-- SQL Server / T-SQL
-- ======================================================================

IF DB_ID(N'SmartBookingCafe') IS NULL
BEGIN
    EXEC('CREATE DATABASE SmartBookingCafe');
END
GO

USE SmartBookingCafe;
GO

/* Create schema if missing */
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'app')
    EXEC('CREATE SCHEMA app');
GO

/* ---------- SAFE DROP (idempotent) ---------- */
-- 1) Drop all FOREIGN KEYS under schema 'app'
DECLARE @sql NVARCHAR(MAX) = N'';
SELECT @sql = STRING_AGG('ALTER TABLE ' + QUOTENAME(OBJECT_SCHEMA_NAME(parent_object_id)) + '.' + QUOTENAME(OBJECT_NAME(parent_object_id)) +
                         ' DROP CONSTRAINT ' + QUOTENAME(name) + ';', CHAR(10))
FROM sys.foreign_keys
WHERE OBJECT_SCHEMA_NAME(parent_object_id) = 'app';
IF @sql IS NOT NULL AND LEN(@sql) > 0 EXEC sp_executesql @sql;

-- 2) Drop all TABLES under schema 'app'
SET @sql = N'';
SELECT @sql = STRING_AGG('DROP TABLE ' + QUOTENAME(OBJECT_SCHEMA_NAME(object_id)) + '.' + QUOTENAME(name) + ';', CHAR(10))
FROM sys.tables
WHERE OBJECT_SCHEMA_NAME(object_id) = 'app';
IF @sql IS NOT NULL AND LEN(@sql) > 0 EXEC sp_executesql @sql;
GO

/* ---------- CREATE TABLES & SEED ---------- */
-- Paste of the original V1 script without the old DROP section
-- (Shortened header; full objects retained)

/* REFERENCE TABLES */
CREATE TABLE app.branch (
    branch_id       INT IDENTITY(1,1) PRIMARY KEY,
    code            VARCHAR(20) NOT NULL UNIQUE,
    name            NVARCHAR(200) NOT NULL,
    address         NVARCHAR(500) NULL,
    tz              VARCHAR(64) NOT NULL DEFAULT 'Asia/Ho_Chi_Minh',
    is_active       BIT NOT NULL DEFAULT 1,
    created_at_utc  DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);
CREATE TABLE app.app_user (
    user_id         INT IDENTITY(1,1) PRIMARY KEY,
    username        VARCHAR(50) NOT NULL UNIQUE,
    display_name    NVARCHAR(200) NOT NULL,
    email           VARCHAR(200) NULL,
    phone           VARCHAR(50) NULL,
    password_hash   VARCHAR(200) NOT NULL,
    is_active       BIT NOT NULL DEFAULT 1,
    created_at_utc  DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);
CREATE TABLE app.role ( role_id INT IDENTITY(1,1) PRIMARY KEY, code VARCHAR(50) NOT NULL UNIQUE, name NVARCHAR(100) NOT NULL );
CREATE TABLE app.permission ( perm_id INT IDENTITY(1,1) PRIMARY KEY, code VARCHAR(100) NOT NULL UNIQUE, name NVARCHAR(200) NOT NULL );
CREATE TABLE app.user_role ( user_id INT NOT NULL, role_id INT NOT NULL, PRIMARY KEY (user_id, role_id),
    CONSTRAINT FK_user_role_user FOREIGN KEY (user_id) REFERENCES app.app_user(user_id),
    CONSTRAINT FK_user_role_role FOREIGN KEY (role_id) REFERENCES app.role(role_id));
CREATE TABLE app.role_permission ( role_id INT NOT NULL, perm_id INT NOT NULL, PRIMARY KEY (role_id, perm_id),
    CONSTRAINT FK_role_permission_role FOREIGN KEY (role_id) REFERENCES app.role(role_id),
    CONSTRAINT FK_role_permission_perm FOREIGN KEY (perm_id) REFERENCES app.permission(perm_id));

/* CUSTOMER & LOYALTY */
CREATE TABLE app.customer (
    customer_id INT IDENTITY(1,1) PRIMARY KEY,
    phone       VARCHAR(30) UNIQUE,
    name        NVARCHAR(200) NOT NULL,
    email       VARCHAR(200) NULL,
    dob         DATE NULL,
    tier_code   VARCHAR(20) NULL,
    created_at_utc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);
CREATE TABLE app.membership_tier (
    tier_code   VARCHAR(20) PRIMARY KEY,
    name        NVARCHAR(100) NOT NULL,
    min_points  INT NOT NULL DEFAULT 0,
    perks_json  NVARCHAR(MAX) NULL
);
CREATE TABLE app.points_ledger (
    id          BIGINT IDENTITY(1,1) PRIMARY KEY,
    customer_id INT NOT NULL,
    points      INT NOT NULL,
    reason      NVARCHAR(200) NULL,
    ref_type    VARCHAR(50) NULL,
    ref_id      VARCHAR(50) NULL,
    created_at_utc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_points_customer FOREIGN KEY (customer_id) REFERENCES app.customer(customer_id)
);

/* RESTAURANT LAYOUT */
CREATE TABLE app.restaurant_table (
    table_id    INT IDENTITY(1,1) PRIMARY KEY,
    branch_id   INT NOT NULL,
    code        VARCHAR(20) NOT NULL,
    seats       INT NOT NULL,
    zone        NVARCHAR(100) NULL,
    UNIQUE (branch_id, code),
    CONSTRAINT FK_rtable_branch FOREIGN KEY (branch_id) REFERENCES app.branch(branch_id)
);
CREATE TABLE app.table_qr (
    id          INT IDENTITY(1,1) PRIMARY KEY,
    table_id    INT NOT NULL,
    qr_token    VARCHAR(64) NOT NULL UNIQUE,
    is_active   BIT NOT NULL DEFAULT 1,
    last_rotated_at DATETIME2 NULL,
    CONSTRAINT FK_tableqr_table FOREIGN KEY (table_id) REFERENCES app.restaurant_table(table_id)
);
CREATE TABLE app.waitlist_ticket (
    ticket_id   BIGINT IDENTITY(1,1) PRIMARY KEY,
    branch_id   INT NOT NULL,
    customer_id INT NULL,
    party_size  INT NOT NULL,
    area        NVARCHAR(100) NULL,
    status      VARCHAR(20) NOT NULL,
    priority    INT NOT NULL DEFAULT 0,
    est_ready_at DATETIME2 NULL,
    hold_until  DATETIME2 NULL,
    note        NVARCHAR(200) NULL,
    created_at_utc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_waitlist_branch FOREIGN KEY (branch_id) REFERENCES app.branch(branch_id),
    CONSTRAINT FK_waitlist_customer FOREIGN KEY (customer_id) REFERENCES app.customer(customer_id)
);

/* MENU & INVENTORY */
CREATE TABLE app.sku (
    sku_id      INT IDENTITY(1,1) PRIMARY KEY,
    code        VARCHAR(30) UNIQUE NOT NULL,
    name        NVARCHAR(200) NOT NULL,
    category    NVARCHAR(100) NOT NULL,
    price       DECIMAL(12,2) NOT NULL,
    is_ingredient BIT NOT NULL DEFAULT 0,
    unit        NVARCHAR(20) NULL,
    is_active   BIT NOT NULL DEFAULT 1
);
CREATE TABLE app.recipe_bom ( sku_id INT NOT NULL, ingredient_id INT NOT NULL, qty DECIMAL(12,4) NOT NULL,
    PRIMARY KEY (sku_id, ingredient_id),
    CONSTRAINT FK_recipe_bom_sku FOREIGN KEY (sku_id) REFERENCES app.sku(sku_id),
    CONSTRAINT FK_recipe_bom_ingredient FOREIGN KEY (ingredient_id) REFERENCES app.sku(sku_id) );
CREATE TABLE app.stock_on_hand ( branch_id INT NOT NULL, sku_id INT NOT NULL, qty DECIMAL(14,4) NOT NULL DEFAULT 0,
    PRIMARY KEY (branch_id, sku_id),
    CONSTRAINT FK_soh_branch FOREIGN KEY (branch_id) REFERENCES app.branch(branch_id),
    CONSTRAINT FK_soh_sku FOREIGN KEY (sku_id) REFERENCES app.sku(sku_id) );
CREATE TABLE app.inventory_txn (
    txn_id      BIGINT IDENTITY(1,1) PRIMARY KEY,
    branch_id   INT NOT NULL,
    sku_id      INT NOT NULL,
    qty_change  DECIMAL(14,4) NOT NULL,
    reason      VARCHAR(30) NOT NULL,
    ref_type    VARCHAR(30) NULL,
    ref_id      VARCHAR(50) NULL,
    created_at_utc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_inventory_txn_branch FOREIGN KEY (branch_id) REFERENCES app.branch(branch_id),
    CONSTRAINT FK_inventory_txn_sku FOREIGN KEY (sku_id) REFERENCES app.sku(sku_id)
);

/* PURCHASING */
CREATE TABLE app.supplier (
    supplier_id INT IDENTITY(1,1) PRIMARY KEY,
    code        VARCHAR(30) UNIQUE NOT NULL,
    name        NVARCHAR(200) NOT NULL,
    contact     NVARCHAR(200) NULL,
    payment_term NVARCHAR(50) NULL
);
CREATE TABLE app.purchase_order (
    po_id       INT IDENTITY(1,1) PRIMARY KEY,
    supplier_id INT NOT NULL,
    branch_id   INT NOT NULL,
    status      VARCHAR(20) NOT NULL,
    expected_at DATETIME2 NULL,
    total_est   DECIMAL(14,2) NULL,
    created_at_utc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_po_supplier FOREIGN KEY (supplier_id) REFERENCES app.supplier(supplier_id)
);
CREATE TABLE app.po_line (
    line_id INT IDENTITY(1,1) PRIMARY KEY,
    po_id   INT NOT NULL,
    sku_id  INT NOT NULL,
    qty     DECIMAL(14,4) NOT NULL,
    cost_est DECIMAL(14,4) NOT NULL,
    CONSTRAINT FK_po_line_po FOREIGN KEY (po_id) REFERENCES app.purchase_order(po_id),
    CONSTRAINT FK_po_line_sku FOREIGN KEY (sku_id) REFERENCES app.sku(sku_id)
);
CREATE TABLE app.goods_receipt (
    gr_id INT IDENTITY(1,1) PRIMARY KEY,
    po_id INT NULL,
    branch_id INT NOT NULL,
    received_at DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    status VARCHAR(20) NOT NULL,
    CONSTRAINT FK_gr_po FOREIGN KEY (po_id) REFERENCES app.purchase_order(po_id)
);
CREATE TABLE app.gr_line (
    line_id INT IDENTITY(1,1) PRIMARY KEY,
    gr_id   INT NOT NULL,
    sku_id  INT NOT NULL,
    qty     DECIMAL(14,4) NOT NULL,
    cost_actual DECIMAL(14,4) NOT NULL,
    lot     NVARCHAR(50) NULL,
    expiry  DATE NULL,
    CONSTRAINT FK_gr_line_gr FOREIGN KEY (gr_id) REFERENCES app.goods_receipt(gr_id),
    CONSTRAINT FK_gr_line_sku FOREIGN KEY (sku_id) REFERENCES app.sku(sku_id)
);

/* STOCK TRANSFER & WASTE */
CREATE TABLE app.stock_transfer (
    st_id INT IDENTITY(1,1) PRIMARY KEY,
    from_branch INT NOT NULL,
    to_branch   INT NOT NULL,
    status      VARCHAR(20) NOT NULL,
    created_at_utc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_st_from FOREIGN KEY (from_branch) REFERENCES app.branch(branch_id),
    CONSTRAINT FK_st_to   FOREIGN KEY (to_branch)   REFERENCES app.branch(branch_id)
);
CREATE TABLE app.stock_transfer_line (
    line_id INT IDENTITY(1,1) PRIMARY KEY,
    st_id   INT NOT NULL,
    sku_id  INT NOT NULL,
    qty     DECIMAL(14,4) NOT NULL,
    CONSTRAINT FK_stl_st FOREIGN KEY (st_id) REFERENCES app.stock_transfer(st_id),
    CONSTRAINT FK_stl_sku FOREIGN KEY (sku_id) REFERENCES app.sku(sku_id)
);
CREATE TABLE app.waste_ticket (
    ticket_id INT IDENTITY(1,1) PRIMARY KEY,
    branch_id INT NOT NULL,
    reason    NVARCHAR(200) NOT NULL,
    status    VARCHAR(20) NOT NULL DEFAULT 'OPEN',
    created_by INT NOT NULL,
    approved_by INT NULL,
    created_at_utc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);
CREATE TABLE app.waste_line ( line_id INT IDENTITY(1,1) PRIMARY KEY, ticket_id INT NOT NULL, sku_id INT NOT NULL, qty DECIMAL(14,4) NOT NULL );

/* ORDERS / POS */
CREATE TABLE app.order_header (
    order_id    BIGINT IDENTITY(1,1) PRIMARY KEY,
    branch_id   INT NOT NULL,
    table_id    INT NULL,
    customer_id INT NULL,
    channel     VARCHAR(10) NOT NULL,
    status      VARCHAR(20) NOT NULL,
    created_at_utc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_order_header_branch FOREIGN KEY (branch_id) REFERENCES app.branch(branch_id),
    CONSTRAINT FK_order_header_table FOREIGN KEY (table_id) REFERENCES app.restaurant_table(table_id),
    CONSTRAINT FK_order_header_customer FOREIGN KEY (customer_id) REFERENCES app.customer(customer_id)
);
CREATE TABLE app.order_item (
    item_id BIGINT IDENTITY(1,1) PRIMARY KEY,
    order_id BIGINT NOT NULL,
    sku_id   INT NOT NULL,
    qty      DECIMAL(12,2) NOT NULL,
    unit_price DECIMAL(12,2) NOT NULL,
    note     NVARCHAR(200) NULL,
    CONSTRAINT FK_order_item_order FOREIGN KEY (order_id) REFERENCES app.order_header(order_id),
    CONSTRAINT FK_order_item_sku FOREIGN KEY (sku_id) REFERENCES app.sku(sku_id)
);
CREATE TABLE app.bill (
    bill_id BIGINT IDENTITY(1,1) PRIMARY KEY,
    order_id BIGINT NOT NULL,
    subtotal DECIMAL(12,2) NOT NULL,
    discount DECIMAL(12,2) NOT NULL DEFAULT 0,
    service_charge DECIMAL(12,2) NOT NULL DEFAULT 0,
    vat      DECIMAL(12,2) NOT NULL DEFAULT 0,
    total    DECIMAL(12,2) NOT NULL,
    status   VARCHAR(20) NOT NULL,
    created_at_utc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_bill_order FOREIGN KEY (order_id) REFERENCES app.order_header(order_id)
);
CREATE TABLE app.payment_method ( method_code VARCHAR(20) PRIMARY KEY, name NVARCHAR(100) NOT NULL );
CREATE TABLE app.order_payment (
    payment_id BIGINT IDENTITY(1,1) PRIMARY KEY,
    bill_id    BIGINT NOT NULL,
    method_code VARCHAR(20) NOT NULL,
    amount     DECIMAL(12,2) NOT NULL,
    paid_at_utc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_order_payment_bill FOREIGN KEY (bill_id) REFERENCES app.bill(bill_id),
    CONSTRAINT FK_order_payment_method FOREIGN KEY (method_code) REFERENCES app.payment_method(method_code)
);

/* VOUCHERS */
CREATE TABLE app.voucher (
    voucher_id  INT IDENTITY(1,1) PRIMARY KEY,
    code        VARCHAR(30) UNIQUE NOT NULL,
    name        NVARCHAR(200) NOT NULL,
    scope       VARCHAR(20) NOT NULL,
    priority    INT NOT NULL DEFAULT 100,
    combinable_group VARCHAR(30) NULL,
    channel     VARCHAR(20) NULL,
    branch_id   INT NULL,
    percent_off DECIMAL(5,2) NULL,
    amount_off  DECIMAL(12,2) NULL,
    min_subtotal DECIMAL(12,2) NULL,
    start_at_utc DATETIME2 NULL,
    end_at_utc   DATETIME2 NULL,
    max_usage   INT NULL,
    usage_count INT NOT NULL DEFAULT 0,
    is_active   BIT NOT NULL DEFAULT 1
);
CREATE TABLE app.voucher_application (
    id      BIGINT IDENTITY(1,1) PRIMARY KEY,
    bill_id BIGINT NOT NULL,
    voucher_id INT NOT NULL,
    amount_applied DECIMAL(12,2) NOT NULL,
    CONSTRAINT FK_vapp_bill FOREIGN KEY (bill_id) REFERENCES app.bill(bill_id),
    CONSTRAINT FK_vapp_voucher FOREIGN KEY (voucher_id) REFERENCES app.voucher(voucher_id)
);

/* FINANCE */
CREATE TABLE app.payment_reconciliation (
    recon_id   INT IDENTITY(1,1) PRIMARY KEY,
    branch_id  INT NOT NULL,
    business_date DATE NOT NULL,
    method_code VARCHAR(20) NOT NULL,
    system_amount DECIMAL(12,2) NOT NULL,
    counted_amount DECIMAL(12,2) NOT NULL,
    diff       DECIMAL(12,2) NOT NULL,
    note       NVARCHAR(200) NULL,
    created_at_utc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_recon_branch FOREIGN KEY (branch_id) REFERENCES app.branch(branch_id)
);
CREATE TABLE app.cash_movement (
    mov_id     INT IDENTITY(1,1) PRIMARY KEY,
    branch_id  INT NOT NULL,
    shift_id   INT NULL,
    type       VARCHAR(10) NOT NULL,
    amount     DECIMAL(12,2) NOT NULL,
    reason     NVARCHAR(200) NULL,
    ref        NVARCHAR(100) NULL,
    created_at_utc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_cashmov_branch FOREIGN KEY (branch_id) REFERENCES app.branch(branch_id)
);

/* HR */
CREATE TABLE app.employee_shift (
    shift_id INT IDENTITY(1,1) PRIMARY KEY,
    branch_id INT NOT NULL,
    user_id  INT NOT NULL,
    role_code VARCHAR(50) NOT NULL,
    start_at_utc DATETIME2 NOT NULL,
    end_at_utc   DATETIME2 NULL,
    CONSTRAINT FK_eshift_branch FOREIGN KEY (branch_id) REFERENCES app.branch(branch_id),
    CONSTRAINT FK_eshift_user FOREIGN KEY (user_id) REFERENCES app.app_user(user_id)
);
CREATE TABLE app.timesheet (
    timesheet_id INT IDENTITY(1,1) PRIMARY KEY,
    branch_id INT NOT NULL,
    user_id  INT NOT NULL,
    check_in_utc DATETIME2 NOT NULL,
    check_out_utc DATETIME2 NULL,
    method   VARCHAR(10) NOT NULL,
    CONSTRAINT FK_timesheet_branch FOREIGN KEY (branch_id) REFERENCES app.branch(branch_id),
    CONSTRAINT FK_timesheet_user FOREIGN KEY (user_id) REFERENCES app.app_user(user_id)
);
CREATE TABLE app.tip_ledger (
    id       INT IDENTITY(1,1) PRIMARY KEY,
    shift_id INT NOT NULL,
    user_id  INT NOT NULL,
    amount   DECIMAL(12,2) NOT NULL,
    source   VARCHAR(10) NOT NULL,
    created_at_utc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_tip_shift FOREIGN KEY (shift_id) REFERENCES app.employee_shift(shift_id),
    CONSTRAINT FK_tip_user FOREIGN KEY (user_id) REFERENCES app.app_user(user_id)
);

/* DEVICES & OBSERVABILITY */
CREATE TABLE app.device_printer (
    printer_id INT IDENTITY(1,1) PRIMARY KEY,
    branch_id INT NOT NULL,
    type      VARCHAR(20) NOT NULL,
    ip        VARCHAR(50) NOT NULL,
    is_active BIT NOT NULL DEFAULT 1,
    CONSTRAINT FK_printer_branch FOREIGN KEY (branch_id) REFERENCES app.branch(branch_id)
);
CREATE TABLE app.audit_log (
    audit_id BIGINT IDENTITY(1,1) PRIMARY KEY,
    actor_id INT NULL,
    action   VARCHAR(50) NOT NULL,
    entity   VARCHAR(50) NOT NULL,
    entity_id VARCHAR(50) NOT NULL,
    old_value NVARCHAR(MAX) NULL,
    new_value NVARCHAR(MAX) NULL,
    ip        VARCHAR(64) NULL,
    created_at_utc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);
CREATE TABLE app.sync_queue (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    entity  VARCHAR(50) NOT NULL,
    payload NVARCHAR(MAX) NOT NULL,
    status  VARCHAR(20) NOT NULL DEFAULT 'PENDING',
    retry_count INT NOT NULL DEFAULT 0,
    created_at_utc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);
CREATE TABLE app.report_snapshot (
    id INT IDENTITY(1,1) PRIMARY KEY,
    branch_id INT NOT NULL,
    shift_id  INT NULL,
    type      VARCHAR(5) NOT NULL,
    payload_json NVARCHAR(MAX) NOT NULL,
    generated_at_utc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);

/* PAYMENT METHODS */
INSERT INTO app.payment_method(method_code, name) VALUES
('CASH','Tiền mặt'),('CARD','Thẻ'),('MOMO','Momo'),('VNPAY','VNPay');

/* SEED: branches, users (5), roles, perms, mappings, customers, tables, menu, BOM, stock, suppliers, PO/GRN, POS flow, vouchers, loyalty, HR, devices, waitlist, report */
-- (Reuse the same seed block from V1)
-- For brevity: pull in original seed block exactly as is
-- BEGIN SEED COPY
INSERT INTO app.branch(code, name, address)
VALUES ('HCM01', N'Chi nhánh Quận 1', N'123 Đồng Khởi, Q1, TP.HCM'),
       ('HN01',  N'Chi nhánh Hoàn Kiếm', N'45 Tràng Tiền, Hoàn Kiếm, HN');

INSERT INTO app.app_user(username, display_name, email, phone, password_hash) VALUES
('admin',N'Quản trị hệ thống','admin@cafe.local','0900000000','HASH_ADMIN'),
('manager',N'Quản lý cửa hàng','manager@cafe.local','0900000001','HASH_MANAGER'),
('cashier',N'Thu ngân','cashier@cafe.local','0900000002','HASH_CASHIER'),
('barista',N'Pha chế','barista@cafe.local','0900000003','HASH_BARISTA'),
('waiter',N'Phục vụ','waiter@cafe.local','0900000004','HASH_WAITER');

INSERT INTO app.role(code, name) VALUES
('ADMIN',N'Quản trị'),('MANAGER',N'Quản lý'),('CASHIER',N'Thu ngân'),('BARISTA',N'Pha chế'),('WAITER',N'Phục vụ');

INSERT INTO app.permission(code, name) VALUES
('POS_OPEN',N'Mở đơn tại POS'),('POS_PAY',N'Thanh toán hóa đơn'),('ORDER_VOID',N'Void đơn'),
('PRICE_OVERRIDE',N'Đổi giá'),('DISCOUNT_MANUAL',N'Giảm giá thủ công'),('VIEW_REPORT',N'Xem báo cáo'),
('INVENTORY_EDIT',N'Điều chỉnh tồn kho'),('PO_APPROVE',N'Duyệt PO');

-- Role-Permission
INSERT INTO app.role_permission(role_id, perm_id)
SELECT r.role_id, p.perm_id FROM app.role r CROSS JOIN app.permission p WHERE r.code='ADMIN';
INSERT INTO app.role_permission(role_id, perm_id)
SELECT r.role_id, p.perm_id FROM app.role r 
JOIN app.permission p ON p.code IN ('POS_OPEN','POS_PAY','ORDER_VOID','VIEW_REPORT','INVENTORY_EDIT','PO_APPROVE')
WHERE r.code='MANAGER';
INSERT INTO app.role_permission(role_id, perm_id)
SELECT r.role_id, p.perm_id FROM app.role r 
JOIN app.permission p ON p.code IN ('POS_OPEN','POS_PAY','VIEW_REPORT')
WHERE r.code='CASHIER';
INSERT INTO app.role_permission(role_id, perm_id)
SELECT r.role_id, p.perm_id FROM app.role r 
JOIN app.permission p ON p.code IN ('VIEW_REPORT')
WHERE r.code='BARISTA';
INSERT INTO app.role_permission(role_id, perm_id)
SELECT r.role_id, p.perm_id FROM app.role r 
JOIN app.permission p ON p.code IN ('POS_OPEN')
WHERE r.code='WAITER';

-- User-Role
INSERT INTO app.user_role(user_id, role_id)
SELECT u.user_id, r.role_id FROM app.app_user u JOIN app.role r ON u.username='admin' AND r.code='ADMIN';
INSERT INTO app.user_role(user_id, role_id)
SELECT u.user_id, r.role_id FROM app.app_user u JOIN app.role r ON u.username='manager' AND r.code='MANAGER';
INSERT INTO app.user_role(user_id, role_id)
SELECT u.user_id, r.role_id FROM app.app_user u JOIN app.role r ON u.username='cashier' AND r.code='CASHIER';
INSERT INTO app.user_role(user_id, role_id)
SELECT u.user_id, r.role_id FROM app.app_user u JOIN app.role r ON u.username='barista' AND r.code='BARISTA';
INSERT INTO app.user_role(user_id, role_id)
SELECT u.user_id, r.role_id FROM app.app_user u JOIN app.role r ON u.username='waiter' AND r.code='WAITER';

-- Customers & Tiers
INSERT INTO app.membership_tier(tier_code, name, min_points) VALUES ('SILVER',N'Silver',0),('GOLD',N'Gold',500),('PLAT',N'Platinum',1500);
INSERT INTO app.customer(phone, name, email, tier_code) VALUES ('0901111111',N'Nguyễn An','an@example.com','SILVER'),
                                                               ('0902222222',N'Trần Bình','binh@example.com','GOLD');

-- Tables + QR
INSERT INTO app.restaurant_table(branch_id, code, seats, zone) VALUES
(1,'T1',2,N'Indoor'),(1,'T2',4,N'Indoor'),(1,'T3',4,N'Window'),(2,'H1',2,N'Indoor'),(2,'H2',4,N'Balcony');
INSERT INTO app.table_qr(table_id, qr_token, is_active) VALUES
(1,'QR_T1_TOKEN',1),(2,'QR_T2_TOKEN',1),(3,'QR_T3_TOKEN',1),(4,'QR_H1_TOKEN',1),(5,'QR_H2_TOKEN',1);

-- SKU & BOM
INSERT INTO app.sku(code,name,category,price,is_ingredient,unit) VALUES
('CF_LATTE',N'Cà phê Latte',N'Coffee',45000,0,N'ly'),
('CF_CAPPU',N'Cappuccino',N'Coffee',49000,0,N'ly'),
('TEA_PEACH',N'Trà đào',N'Trà',39000,0,N'ly'),
('ADD_SYRUP',N'Siro Vani',N'Phụ gia',6000,0,N'ml'),
('ING_ESP',N'Espresso Shot',N'Nguyên liệu',0,1,N'shot'),
('ING_MILK',N'Sữa tươi',N'Nguyên liệu',0,1,N'ml'),
('ING_TEA',N'Trà đen',N'Nguyên liệu',0,1,N'ml'),
('ING_PEACH',N'Mứt đào',N'Nguyên liệu',0,1,N'g'),
('ING_SUGAR',N'Đường',N'Nguyên liệu',0,1,N'g');

INSERT INTO app.recipe_bom(sku_id,ingredient_id,qty)
SELECT s1.sku_id,s2.sku_id,1 FROM app.sku s1 JOIN app.sku s2 ON s1.code='CF_CAPPU' AND s2.code='ING_ESP'
UNION ALL SELECT s1.sku_id,s2.sku_id,150 FROM app.sku s1 JOIN app.sku s2 ON s1.code='CF_CAPPU' AND s2.code='ING_MILK';
INSERT INTO app.recipe_bom(sku_id,ingredient_id,qty)
SELECT s1.sku_id,s2.sku_id,1 FROM app.sku s1 JOIN app.sku s2 ON s1.code='CF_LATTE' AND s2.code='ING_ESP'
UNION ALL SELECT s1.sku_id,s2.sku_id,180 FROM app.sku s1 JOIN app.sku s2 ON s1.code='CF_LATTE' AND s2.code='ING_MILK';
INSERT INTO app.recipe_bom(sku_id,ingredient_id,qty)
SELECT s1.sku_id,s2.sku_id,200 FROM app.sku s1 JOIN app.sku s2 ON s1.code='TEA_PEACH' AND s2.code='ING_TEA'
UNION ALL SELECT s1.sku_id,s2.sku_id,30 FROM app.sku s1 JOIN app.sku s2 ON s1.code='TEA_PEACH' AND s2.code='ING_PEACH'
UNION ALL SELECT s1.sku_id,s2.sku_id,15 FROM app.sku s1 JOIN app.sku s2 ON s1.code='TEA_PEACH' AND s2.code='ING_SUGAR';

-- Initial stock (ingredients only)
INSERT INTO app.stock_on_hand(branch_id, sku_id, qty) SELECT 1, sku_id, 1000 FROM app.sku WHERE is_ingredient=1;
INSERT INTO app.stock_on_hand(branch_id, sku_id, qty) SELECT 2, sku_id, 800  FROM app.sku WHERE is_ingredient=1;

-- Suppliers, PO/GRN & inventory txns
INSERT INTO app.supplier(code,name,contact,payment_term) VALUES ('SUP01',N'Nhà cung cấp A',N'0938 000 111',N'Net 15'),
                                                                ('SUP02',N'Nhà cung cấp B',N'0938 000 222',N'COD');
INSERT INTO app.purchase_order(supplier_id, branch_id, status, expected_at, total_est)
VALUES (1,1,'APPROVED', DATEADD(day,1,SYSUTCDATETIME()), 500000);
INSERT INTO app.po_line(po_id, sku_id, qty, cost_est)
SELECT 1, s.sku_id, 500, 3000 FROM app.sku s WHERE s.code='ING_MILK'
UNION ALL SELECT 1, s.sku_id, 400, 2500 FROM app.sku s WHERE s.code='ING_TEA';
INSERT INTO app.goods_receipt(po_id, branch_id, status) VALUES (1,1,'COMPLETE');
INSERT INTO app.gr_line(gr_id, sku_id, qty, cost_actual, lot, expiry)
SELECT 1, s.sku_id, 500, 2950, 'LOT-MILK-01', DATEADD(day,10,GETUTCDATE()) FROM app.sku s WHERE s.code='ING_MILK'
UNION ALL SELECT 1, s.sku_id, 400, 2450, 'LOT-TEA-01', DATEADD(day,180,GETUTCDATE()) FROM app.sku s WHERE s.code='ING_TEA';
INSERT INTO app.inventory_txn(branch_id, sku_id, qty_change, reason, ref_type, ref_id)
SELECT 1, s.sku_id, 500, 'GRN','GR','1' FROM app.sku s WHERE s.code='ING_MILK'
UNION ALL SELECT 1, s.sku_id, 400, 'GRN','GR','1' FROM app.sku s WHERE s.code='ING_TEA';

-- Transfer
INSERT INTO app.stock_transfer(from_branch, to_branch, status) VALUES (1,2,'RECEIVED');
INSERT INTO app.stock_transfer_line(st_id, sku_id, qty) SELECT 1, s.sku_id, 100 FROM app.sku s WHERE s.code='ING_TEA';
INSERT INTO app.inventory_txn(branch_id, sku_id, qty_change, reason, ref_type, ref_id)
SELECT 1, s.sku_id, -100, 'TRANSFER_OUT','ST','1' FROM app.sku s WHERE s.code='ING_TEA';
INSERT INTO app.inventory_txn(branch_id, sku_id, qty_change, reason, ref_type, ref_id)
SELECT 2, s.sku_id, 100, 'TRANSFER_IN','ST','1' FROM app.sku s WHERE s.code='ING_TEA';

-- Waste
INSERT INTO app.waste_ticket(branch_id, reason, status, created_by) VALUES (1,N'Hao hụt sữa cuối ngày','APPROVED',2);
INSERT INTO app.waste_line(ticket_id, sku_id, qty) SELECT 1, s.sku_id, 50 FROM app.sku s WHERE s.code='ING_MILK';
INSERT INTO app.inventory_txn(branch_id, sku_id, qty_change, reason, ref_type, ref_id)
SELECT 1, s.sku_id, -50, 'WASTE','WT','1' FROM app.sku s WHERE s.code='ING_MILK';

-- Vouchers & loyalty
INSERT INTO app.voucher(code,name,scope,priority,percent_off,min_subtotal,start_at_utc,end_at_utc,max_usage)
VALUES ('WELCOME10',N'Giảm 10% đơn đầu','ORDER',10,10.00,50000,DATEADD(day,-1,SYSUTCDATETIME()),DATEADD(day,30,SYSUTCDATETIME()),1000),
       ('LATTE5K',N'Giảm 5,000 cho Latte','SKU',20,NULL,NULL,DATEADD(day,-1,SYSUTCDATETIME()),DATEADD(day,30,SYSUTCDATETIME()),1000);
INSERT INTO app.points_ledger(customer_id, points, reason, ref_type, ref_id)
VALUES (1,120,N'Welcome bonus','SYS','WBONUS');

-- Payment methods already seeded above
-- Sample POS flow
DECLARE @order_id BIGINT;
INSERT INTO app.order_header(branch_id, table_id, customer_id, channel, status)
SELECT 1, t.table_id, 1, 'POS', 'CONFIRMED' FROM app.restaurant_table t WHERE t.code='T1' AND t.branch_id=1;
SET @order_id = SCOPE_IDENTITY();
INSERT INTO app.order_item(order_id, sku_id, qty, unit_price)
SELECT @order_id, sku_id, 1, price FROM app.sku WHERE code='CF_LATTE';
INSERT INTO app.order_item(order_id, sku_id, qty, unit_price)
SELECT @order_id, sku_id, 1, price FROM app.sku WHERE code='TEA_PEACH';
INSERT INTO app.inventory_txn(branch_id, sku_id, qty_change, reason, ref_type, ref_id)
SELECT 1, s2.sku_id, -rb.qty, 'ORDER_CONSUME','ORDER', CAST(@order_id AS VARCHAR(50))
FROM app.sku s1 JOIN app.recipe_bom rb ON rb.sku_id=s1.sku_id JOIN app.sku s2 ON s2.sku_id=rb.ingredient_id
WHERE s1.code IN ('CF_LATTE','TEA_PEACH');
DECLARE @subtotal DECIMAL(12,2) = (SELECT SUM(qty*unit_price) FROM app.order_item WHERE order_id=@order_id);
DECLARE @discount DECIMAL(12,2) = ROUND(@subtotal*0.10, 0);
DECLARE @service DECIMAL(12,2) = ROUND(@subtotal*0.05, 0);
DECLARE @vat DECIMAL(12,2) = ROUND((@subtotal-@discount+@service)*0.08, 0);
DECLARE @total DECIMAL(12,2) = @subtotal-@discount+@service+@vat;
INSERT INTO app.bill(order_id, subtotal, discount, service_charge, vat, total, status)
VALUES (@order_id, @subtotal, @discount, @service, @vat, @total, 'OPEN');
DECLARE @bill_id BIGINT = SCOPE_IDENTITY();
INSERT INTO app.voucher_application(bill_id, voucher_id, amount_applied)
SELECT @bill_id, v.voucher_id, @discount FROM app.voucher v WHERE v.code='WELCOME10';
INSERT INTO app.order_payment(bill_id, method_code, amount) VALUES (@bill_id, 'CASH', @total);
UPDATE app.bill SET status='PAID' WHERE bill_id=@bill_id;
UPDATE app.order_header SET status='CLOSED' WHERE order_id=@order_id;
INSERT INTO app.payment_reconciliation(branch_id, business_date, method_code, system_amount, counted_amount, diff, note)
VALUES (1, CAST(GETUTCDATE() AS DATE), 'CASH', @total, @total, 0, N'Khớp tiền mặt');

-- HR
INSERT INTO app.employee_shift(branch_id, user_id, role_code, start_at_utc)
SELECT 1, u.user_id, 'CASHIER', DATEADD(hour,-2,SYSUTCDATETIME()) FROM app.app_user u WHERE username='cashier';
INSERT INTO app.timesheet(branch_id, user_id, check_in_utc, method)
SELECT 1, u.user_id, DATEADD(hour,-2,SYSUTCDATETIME()), 'QR' FROM app.app_user u WHERE username='cashier';
UPDATE app.timesheet SET check_out_utc = SYSUTCDATETIME() WHERE user_id = (SELECT user_id FROM app.app_user WHERE username='cashier');
INSERT INTO app.tip_ledger(shift_id, user_id, amount, source)
SELECT s.shift_id, u.user_id, 50000, 'POOL' FROM app.employee_shift s JOIN app.app_user u ON u.username='waiter' WHERE s.role_code='CASHIER';

-- Devices
INSERT INTO app.device_printer(branch_id, type, ip) VALUES
(1,'KDS','192.168.1.101'),(1,'TICKET','192.168.1.102'),(1,'BAR','192.168.1.103');

-- Waitlist
INSERT INTO app.waitlist_ticket(branch_id, customer_id, party_size, area, status, priority, est_ready_at, hold_until, note)
VALUES (1, 2, 3, N'Indoor', 'WAITING', 0, DATEADD(minute,15,SYSUTCDATETIME()), NULL, N'Có trẻ em');

-- Reports
INSERT INTO app.report_snapshot(branch_id, type, payload_json)
VALUES (1,'X',N'{"sales":100000,"orders":5,"avg_ticket":20000}');

-- END SEED COPY
GO

/* ---------- View ---------- */
IF OBJECT_ID('app.v_sales_daily') IS NOT NULL DROP VIEW app.v_sales_daily;
GO
CREATE VIEW app.v_sales_daily AS
SELECT
    CAST(b.created_at_utc AT TIME ZONE 'SE Asia Standard Time' AS DATE) AS business_date,
    br.code AS branch_code,
    SUM(b.total) AS total_sales,
    COUNT(*) AS bills
FROM app.bill b
JOIN app.order_header o ON o.order_id=b.order_id
JOIN app.branch br ON br.branch_id = o.branch_id
WHERE b.status='PAID'
GROUP BY CAST(b.created_at_utc AT TIME ZONE 'SE Asia Standard Time' AS DATE), br.code;
GO

-- Done




dotnet tool install --global dotnet-ef
dotnet add package Microsoft.EntityFrameworkCore
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet add package Microsoft.Data.SqlClient

:: (tùy chọn, khuyến nghị cho dự án của bạn)
dotnet add package Microsoft.AspNetCore.SignalR              :: realtime (server)
dotnet add package Microsoft.AspNetCore.SignalR.Client       :: nếu client .NET cần subscribe
dotnet add package Swashbuckle.AspNetCore                    :: Swagger UI cho API
dotnet add package AutoMapper.Extensions.Microsoft.DependencyInjection
dotnet add package FluentValidation.AspNetCore
dotnet add package Microsoft.Extensions.Caching.StackExchangeRedis
dotnet add package Microsoft.Extensions.Diagnostics.HealthChecks
dotnet add package Serilog.AspNetCore                        :: logging đẹp + file/console
dotnet add package Serilog.Sinks.Console
dotnet add package Serilog.Sinks.File

