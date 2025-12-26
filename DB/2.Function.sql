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




CREATE OR REPLACE FUNCTION loyalty_admin.authenticate_user(
    p_username STRING, 
    p_password_hash STRING
)
RETURNS TABLE (
    user_id INT,
    username STRING,
    user_role STRING
) 
AS $$
    SELECT user_id, username, user_role 
    FROM loyalty_admin.users 
    WHERE username = p_username 
      AND password_hash = p_password_hash;
$$ LANGUAGE SQL;

--SELECT * FROM loyalty_admin.authenticate_user('khachhang_a', 'hashed_pass_kh_a');
