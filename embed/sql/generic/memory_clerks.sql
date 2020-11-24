SELECT 
	[type]
	,SUM(pages_kb)					   AS sum_pages_kb
	,SUM(virtual_memory_reserved_kb)   AS sum_virtual_memory_reserved_kb
	,SUM(virtual_memory_committed_kb)  AS sum_virtual_memory_committed_kb
	,SUM(shared_memory_reserved_kb)    AS sum_shared_memory_reserved_kb
	,SUM(shared_memory_committed_kb)   AS sum_shared_memory_committed_kb
FROM sys.dm_os_memory_clerks
GROUP BY [type]