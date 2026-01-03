SHOW Users;

CREATE USER dev_user;

SHOW SCHEMAS;

CREATE SCHEMA IF NOT EXISTS loyalty_admin;

GRANT ALL ON ALL TABLES IN SCHEMA loyalty_admin TO dev_user;

GRANT USAGE ON SCHEMA loyalty_admin TO dev_user;
