-- Run as ITROHA on XEPDB1 (localhost:1521/XEPDB1)
ALTER TABLE upload_logs ADD deleted_at TIMESTAMP;
ALTER TABLE upload_logs ADD deleted_by VARCHAR2(256);
