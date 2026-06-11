-- Run as ITROHA on XEPDB1 (localhost:1521/XEPDB1)
ALTER TABLE upload_logs ADD uploaded_by VARCHAR2(256);
ALTER TABLE download_logs ADD downloaded_by VARCHAR2(256);
