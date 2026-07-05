
DECLARE @sql NVARCHAR(MAX) = N'';
SELECT @sql += 'ALTER TABLE ' + QUOTENAME(s.name) + '.' + QUOTENAME(t.name) +
  ' ALTER COLUMN ' + QUOTENAME(c.name) + ' ' + tp.name +
  CASE WHEN tp.name IN ('nvarchar','nchar','varchar','char') THEN
     '(' + CASE WHEN c.max_length = -1 THEN 'max'
                WHEN tp.name IN ('nvarchar','nchar') THEN CAST(c.max_length/2 AS VARCHAR)
                ELSE CAST(c.max_length AS VARCHAR) END + ')'
     ELSE '' END + ' NULL;' + CHAR(13)
FROM sys.columns c
JOIN sys.tables t  ON c.object_id = t.object_id
JOIN sys.schemas s ON t.schema_id = s.schema_id
JOIN sys.types tp  ON c.user_type_id = tp.user_type_id
WHERE c.name = 'BranchId' AND c.is_nullable = 0;
PRINT @sql;              -- review first
EXEC sp_executesql @sql; -- then run