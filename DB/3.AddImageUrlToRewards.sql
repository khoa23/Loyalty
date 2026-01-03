-- Thêm cột image_url vào bảng rewards
ALTER TABLE loyalty_admin.rewards 
ADD COLUMN IF NOT EXISTS image_url VARCHAR(500);

-- Cập nhật comment cho cột
COMMENT ON COLUMN loyalty_admin.rewards.image_url IS 'Đường dẫn ảnh của quà tặng';

