CREATE OR REPLACE FUNCTION loyalty_admin.get_customer_info(p_user_id INT)
RETURNS TABLE (
    cif_number STRING,
    full_name STRING,
    current_points INT 
) 
AS $$
    SELECT cif_number, full_name, current_points 
    FROM loyalty_admin.customers 
    WHERE user_id = p_user_id;
$$ LANGUAGE SQL;


--SELECT * FROM loyalty_admin.get_customer_info('1134039809002799105');




CREATE OR REPLACE FUNCTION loyalty_admin.get_user_by_username(
    p_username STRING
)
RETURNS TABLE (
    user_id INT,
    username STRING,
    password_hash STRING,
    user_role STRING
) 
AS $$
    SELECT user_id, username, password_hash, user_role 
    FROM loyalty_admin.users 
    WHERE username = p_username;
$$ LANGUAGE SQL;

--SELECT * FROM loyalty_admin.authenticate_user('khachhang_a', 'hashed_pass_kh_a');



-- Xóa function cũ (nếu có) với signature cũ (_limit, _offset)
DROP FUNCTION IF EXISTS loyalty_admin.get_available_rewards(INT, INT);

-- Tạo lại function với signature mới (_page, _page_size)
CREATE OR REPLACE FUNCTION loyalty_admin.get_available_rewards(_page INT, _page_size INT)
RETURNS TABLE (
    reward_id INT,
    reward_name VARCHAR(100),
    description TEXT,
    points_cost BIGINT,
    stock_quantity INT,
    image_url VARCHAR(500),
    updated_at TIMESTAMP
) AS $$
DECLARE
    _limit INT;
    _offset INT;
BEGIN
    -- Validate và tính toán limit, offset từ page và pageSize
    IF _page < 1 THEN
        _page := 1;
    END IF;
    
    IF _page_size <= 0 THEN
        _page_size := 10;
    END IF;
    
    IF _page_size > 100 THEN
        _page_size := 100;
    END IF;
    
    -- Tính toán limit và offset
    _limit := _page_size;
    _offset := (_page - 1) * _page_size;
    
    RETURN QUERY
    SELECT 
        r.reward_id, 
        r.reward_name, 
        r.description, 
        r.points_cost, 
        r.stock_quantity, 
        r.image_url,
        r.updated_at
    FROM loyalty_admin.rewards r
    WHERE r.is_active = TRUE 
      AND r.stock_quantity > 0
    ORDER BY r.updated_at DESC, r.reward_id DESC
    LIMIT _limit 
    OFFSET _offset;
END;
$$ LANGUAGE plpgsql;
--SELECT * FROM loyalty_admin.get_available_rewards(1, 10);


-- Function: Đổi quà (Redeem Reward)
-- Kiểm tra điểm, tồn kho, trừ điểm, trừ tồn kho, tạo lịch sử
CREATE OR REPLACE FUNCTION loyalty_admin.redeem_reward(
    p_customer_id INT,
    p_reward_id INT,
    p_quantity INT
)
RETURNS TABLE (
    transaction_id TEXT,
    customer_id TEXT,
    reward_id TEXT,
    reward_name VARCHAR(100),
    quantity_redeemed INT,
    points_spent BIGINT,
    remaining_points BIGINT,
    redemption_date TIMESTAMP,
    transaction_status VARCHAR(20)
) AS $$
DECLARE
    v_customer_points BIGINT;
    v_reward_cost BIGINT;
    v_stock_quantity INT;
    v_total_cost BIGINT;
    v_new_transaction_id INT;
BEGIN
    -- Kiểm tra customer có tồn tại và lấy điểm hiện tại
    SELECT current_points INTO v_customer_points
    FROM loyalty_admin.customers
    WHERE customer_id = p_customer_id;
    
    IF v_customer_points IS NULL THEN
        RAISE EXCEPTION 'Không tìm thấy khách hàng với ID: %', p_customer_id;
    END IF;
    
    -- Kiểm tra reward có tồn tại, đang active và lấy thông tin
    SELECT points_cost, stock_quantity INTO v_reward_cost, v_stock_quantity
    FROM loyalty_admin.rewards
    WHERE reward_id = p_reward_id AND is_active = TRUE;
    
    IF v_reward_cost IS NULL THEN
        RAISE EXCEPTION 'Không tìm thấy quà hoặc quà không còn hoạt động với ID: %', p_reward_id;
    END IF;
    
    -- Kiểm tra số lượng tồn kho
    IF v_stock_quantity < p_quantity THEN
        RAISE EXCEPTION 'Số lượng tồn kho không đủ. Tồn kho hiện tại: %, yêu cầu: %', v_stock_quantity, p_quantity;
    END IF;
    
    -- Tính tổng điểm cần chi
    v_total_cost := v_reward_cost * p_quantity;
    
    -- Kiểm tra điểm có đủ không
    IF v_customer_points < v_total_cost THEN
        RAISE EXCEPTION 'Điểm không đủ. Điểm hiện tại: %, cần: %', v_customer_points, v_total_cost;
    END IF;
    
    -- Bắt đầu transaction: Trừ điểm, trừ tồn kho, tạo lịch sử
    -- 1. Trừ điểm của customer
    UPDATE loyalty_admin.customers
    SET current_points = current_points - v_total_cost,
        updated_at = now()
    WHERE customer_id = p_customer_id;
    
    -- 2. Trừ tồn kho của reward
    UPDATE loyalty_admin.rewards
    SET stock_quantity = stock_quantity - p_quantity,
        updated_at = now()
    WHERE reward_id = p_reward_id;
    
    -- 3. Tạo lịch sử đổi quà
    INSERT INTO loyalty_admin.redemption_history (
        customer_id, 
        reward_id, 
        quantity_redeemed, 
        points_spent, 
        transaction_status
    )
    VALUES (
        p_customer_id,
        p_reward_id,
        p_quantity,
        v_total_cost,
        'Completed'
    )
    RETURNING transaction_id INTO v_new_transaction_id;
    
    -- Trả về thông tin giao dịch
    RETURN QUERY
    SELECT 
        rh.transaction_id::text,
        rh.customer_id::text,
        rh.reward_id::text,
        r.reward_name,
        rh.quantity_redeemed,
        rh.points_spent,
        c.current_points as remaining_points,
        rh.redemption_date,
        rh.transaction_status
    FROM loyalty_admin.redemption_history rh
    INNER JOIN loyalty_admin.rewards r ON rh.reward_id = r.reward_id
    INNER JOIN loyalty_admin.customers c ON rh.customer_id = c.customer_id
    WHERE rh.transaction_id = v_new_transaction_id;
END;
$$ LANGUAGE plpgsql;

--SELECT * FROM loyalty_admin.redeem_reward(1, 1, 2);


-- Function: Lấy lịch sử đổi quà của customer (có phân trang)
CREATE OR REPLACE FUNCTION loyalty_admin.get_redemption_history(
    p_customer_id INT,
    p_page INT,
    p_page_size INT
)
RETURNS TABLE (
    transaction_id TEXT,
    reward_id TEXT,
    reward_name VARCHAR(100),
    description TEXT,
    quantity_redeemed INT,
    points_spent BIGINT,
    redemption_date TIMESTAMP,
    transaction_status VARCHAR(20)
) AS $$
DECLARE
    v_limit INT;
    v_offset INT;
BEGIN
    -- Validate và tính toán limit, offset
    IF p_page < 1 THEN
        p_page := 1;
    END IF;
    
    IF p_page_size <= 0 THEN
        p_page_size := 10;
    END IF;
    
    IF p_page_size > 100 THEN
        p_page_size := 100;
    END IF;
    
    v_limit := p_page_size;
    v_offset := (p_page - 1) * p_page_size;
    
    RETURN QUERY
    SELECT 
        rh.transaction_id::text,
        rh.reward_id::text,
        r.reward_name,
        r.description,
        rh.quantity_redeemed,
        rh.points_spent,
        rh.redemption_date,
        rh.transaction_status
    FROM loyalty_admin.redemption_history rh
    INNER JOIN loyalty_admin.rewards r ON rh.reward_id = r.reward_id
    WHERE rh.customer_id = p_customer_id
    ORDER BY rh.redemption_date DESC, rh.transaction_id DESC
    LIMIT v_limit
    OFFSET v_offset;
END;
$$ LANGUAGE plpgsql;

--SELECT * FROM loyalty_admin.get_redemption_history(1, 1, 10);