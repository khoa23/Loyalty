-- Script để cập nhật function get_available_rewards
-- Chạy script này để thay thế function cũ bằng function mới với page và pageSize

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
        r.updated_at
    FROM loyalty_admin.rewards r
    WHERE r.is_active = TRUE 
      AND r.stock_quantity > 0
    ORDER BY r.updated_at DESC, r.reward_id DESC
    LIMIT _limit 
    OFFSET _offset;
END;
$$ LANGUAGE plpgsql;

-- Test function
-- Trang 1, 1 phần tử
SELECT * FROM loyalty_admin.get_available_rewards(1, 1);

-- Trang 2, 1 phần tử  
SELECT * FROM loyalty_admin.get_available_rewards(2, 1);

-- Trang 3, 1 phần tử
SELECT * FROM loyalty_admin.get_available_rewards(3, 1);

