CREATE DATABASE loyalty_db;

CREATE TABLE loyalty_admin.users (
    -- Dùng unique_rowid() của CockroachDB để tạo ID duy nhất, phân tán
    user_id          INT PRIMARY KEY DEFAULT unique_rowid(), 
    username         VARCHAR(50) NOT NULL UNIQUE,
    password_hash    VARCHAR(255) NOT NULL,
    user_role        VARCHAR(20) NOT NULL CHECK (user_role IN ('Customer', 'Admin')), -- Phân loại vai trò
    created_at       TIMESTAMP DEFAULT now()
);

-- Tạo Index trên vai trò để truy vấn nhanh các tài khoản Admin
CREATE INDEX idx_user_role ON loyalty_admin.users (user_role);


CREATE TABLE loyalty_admin.customers (
    customer_id      INT PRIMARY KEY DEFAULT unique_rowid(),
    user_id          INT UNIQUE REFERENCES loyalty_admin.users(user_id) ON DELETE CASCADE, -- Liên kết với bảng users
    cif_number       VARCHAR(50) NOT NULL UNIQUE, -- Mã CIF không trùng
    full_name        VARCHAR(100) NOT NULL,
    current_points   BIGINT NOT NULL DEFAULT 0 CHECK (current_points >= 0), -- Số điểm hiện tại, không âm
    phone_number     VARCHAR(15),
    updated_at       TIMESTAMP DEFAULT now()
);

-- Index trên CIF để truy vấn nhanh chóng, quan trọng cho việc tra cứu
CREATE INDEX idx_cif_number ON loyalty_admin.customers (cif_number);


CREATE TABLE loyalty_admin.rewards (
    reward_id        INT PRIMARY KEY DEFAULT unique_rowid(),
    reward_name      VARCHAR(100) NOT NULL UNIQUE,
    description      TEXT,
    points_cost      BIGINT NOT NULL CHECK (points_cost > 0), -- Giá quy đổi điểm (phải dương)
    stock_quantity   INT NOT NULL CHECK (stock_quantity >= 0), -- Số lượng tồn kho (không âm)
    is_active        BOOLEAN DEFAULT TRUE,
    last_updated_by  INT REFERENCES loyalty_admin.users(user_id) ON DELETE SET NULL, -- Ai đã cập nhật lần cuối
    updated_at       TIMESTAMP DEFAULT now()
);

-- Index trên điểm và trạng thái để tối ưu hóa việc hiển thị quà cho KH
CREATE INDEX idx_reward_cost_active ON loyalty_admin.rewards (points_cost, is_active);


CREATE TABLE loyalty_admin.redemption_history (
    transaction_id   INT PRIMARY KEY DEFAULT unique_rowid(),
    customer_id      INT NOT NULL REFERENCES loyalty_admin.customers(customer_id), -- Khách hàng đổi quà
    reward_id        INT NOT NULL REFERENCES loyalty_admin.rewards(reward_id),     -- Quà được đổi
    quantity_redeemed INT NOT NULL CHECK (quantity_redeemed > 0),    -- Số lượng đổi
    points_spent     BIGINT NOT NULL CHECK (points_spent > 0),       -- Tổng điểm đã chi tiêu
    redemption_date  TIMESTAMP DEFAULT now(),
    
    -- Trạng thái giao dịch (ví dụ: Completed, Processing, Failed)
    transaction_status VARCHAR(20) NOT NULL DEFAULT 'Completed', 
    
    -- Dùng cho việc liên kết với Kafka (nếu cần theo dõi)
    kafka_offset     BIGINT, 
    kafka_topic      VARCHAR(50)
);

-- Index để tìm kiếm lịch sử theo Khách hàng (quan trọng cho màn hình lịch sử của KH)
CREATE INDEX idx_history_customer ON loyalty_admin.redemption_history (customer_id, redemption_date DESC);


INSERT INTO loyalty_admin.users (username, password_hash, user_role) VALUES
('admin_loan', '$2a$12$AR8fr6S1zkeDMQPAozx29O4w4hPVz6zxMpouVL1HKCoYa1l1FzFiW', 'Admin'),
('khachhang_a', '$2a$12$AR8fr6S1zkeDMQPAozx29O4w4hPVz6zxMpouVL1HKCoYa1l1FzFiW', 'Customer'),
('khachhang_b', '$2a$12$AR8fr6S1zkeDMQPAozx29O4w4hPVz6zxMpouVL1HKCoYa1l1FzFiW', 'Customer'),
('khachhang_c', '$2a$12$AR8fr6S1zkeDMQPAozx29O4w4hPVz6zxMpouVL1HKCoYa1l1FzFiW', 'Customer');


INSERT INTO loyalty_admin.customers (user_id, cif_number, full_name, current_points, phone_number) VALUES
-- Giả sử user_id 11 là của 'khachhang_a'
((SELECT user_id FROM loyalty_admin.users WHERE username = 'khachhang_a'), 'CIF000001', 'Nguyễn Văn A', 15000, '0901234567'),
-- Giả sử user_id 12 là của 'khachhang_b'
((SELECT user_id FROM loyalty_admin.users WHERE username = 'khachhang_b'), 'CIF000002', 'Trần Thị B', 4500, '0907654321'),
-- Giả sử user_id 13 là của 'khachhang_c'
((SELECT user_id FROM loyalty_admin.users WHERE username = 'khachhang_c'), 'CIF000003', 'Lê Văn C', 2000, '0912345678');
--select * from customers

INSERT INTO loyalty_admin.rewards (reward_name, description, points_cost, stock_quantity, last_updated_by) VALUES
('Voucher Điện Tử 100K', 'Voucher sử dụng tại siêu thị', 1000, 100, (SELECT user_id FROM loyalty_admin.users WHERE username = 'admin_loan')),
('Tai Nghe Bluetooth', 'Tai nghe không dây chất lượng cao', 5000, 25, (SELECT user_id FROM loyalty_admin.users WHERE username = 'admin_loan')),
('Bình Giữ Nhiệt Cao Cấp', 'Bình giữ nhiệt dung tích 500ml', 2500, 50, (SELECT user_id FROM loyalty_admin.users WHERE username = 'admin_loan'));


INSERT INTO loyalty_admin.redemption_history (customer_id, reward_id, quantity_redeemed, points_spent, transaction_status) VALUES
-- Giao dịch 1: Khách hàng A đổi 5 Voucher 100K
((SELECT customer_id FROM loyalty_admin.customers WHERE cif_number = 'CIF000001'), 
 (SELECT reward_id FROM loyalty_admin.rewards WHERE reward_name = 'Voucher Điện Tử 100K'), 
 5, 5000, 'Completed'),

-- Giao dịch 2: Khách hàng B đổi 1 Bình Giữ Nhiệt
((SELECT customer_id FROM loyalty_admin.customers WHERE cif_number = 'CIF000002'), 
 (SELECT reward_id FROM loyalty_admin.rewards WHERE reward_name = 'Bình Giữ Nhiệt Cao Cấp'), 
 1, 2500, 'Completed');

-- Cập nhật số điểm của Khách hàng A: 15000 - 5000 = 10000
UPDATE loyalty_admin.customers SET current_points = 10000 WHERE cif_number = 'CIF000001';

-- Cập nhật số điểm của Khách hàng B: 4500 - 2500 = 2000
UPDATE loyalty_admin.customers SET current_points = 2000 WHERE cif_number = 'CIF000002';

-- Cập nhật tồn kho Voucher: 100 - 5 = 95
UPDATE loyalty_admin.rewards SET stock_quantity = 95 WHERE reward_name = 'Voucher Điện Tử 100K';

-- Cập nhật tồn kho Bình Giữ Nhiệt: 50 - 1 = 49
UPDATE loyalty_admin.rewards SET stock_quantity = 49 WHERE reward_name = 'Bình Giữ Nhiệt Cao Cấp';


