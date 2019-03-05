SELECT 
SUM(total_page_count *1.0/128) AS Total_space_MB , 
SUM(unallocated_extent_page_count*1.0/128) AS Unallocated_Space_MB, 
SUM(user_object_reserved_page_count*1.0/128) AS User_Obj_Allocated_Space_MB, 
SUM(internal_object_reserved_page_count*1.0/128) AS Internal_Obj_Allocated_Space_MB,
(SUM(total_page_count)-SUM(unallocated_extent_page_count)-SUM(user_object_reserved_page_count)- SUM(internal_object_reserved_page_count) )*1.0/128 AS Other_Obj_Space_MB

FROM tempdb.sys.dm_db_file_space_usage


