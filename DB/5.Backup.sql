--Backup:
BACKUP DATABASE loyalty_db
INTO 'nodelocal://self/cockroach/backup';
--khi chạy xong thì file backup sẽ ở thư mục: /cockroach/cockroach-data/extern/cockroach/backup

--Kiểm tra backup sau khi chạy:
SHOW BACKUPS IN 'nodelocal://self/cockroach/backup';
--Restore:
RESTORE DATABASE loyalty_db
FROM LATEST IN 'nodelocal://self/cockroach/backup';
Copy backup ra Windows: copy các file backup ra ổ D:\cockroach-backup
docker cp roach2:/cockroach/cockroach-data/extern/cockroach/backup D:/cockroach-backup
