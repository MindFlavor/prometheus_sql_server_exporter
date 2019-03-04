SELECT 
	--scheduler_address  
	parent_node_id 
	,scheduler_id 
	,cpu_id      
	--,[status]                                                       
	,is_online 
	,is_idle 
	,preemptive_switches_count 
	,context_switches_count 
	,idle_switches_count 
	,current_tasks_count 
	,runnable_tasks_count 
	,current_workers_count 
	,active_workers_count 
	,work_queue_count     
	,pending_disk_io_count 
	,load_factor yield_count 
	,last_timer_activity  
	,failed_to_create_worker 
	--,active_worker_address 
	--,memory_object_address 
	--,task_memory_object_address 
	,quantum_length_us    
	,total_cpu_usage_ms   
	,total_cpu_idle_capped_ms 
	,total_scheduler_delay_ms
FROM sys.dm_os_schedulers
WHERE status = 'VISIBLE ONLINE';
