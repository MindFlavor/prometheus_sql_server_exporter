SELECT 
  wait_type, waiting_tasks_count, wait_time_ms
FROM sys.dm_os_wait_stats
WHERE [wait_type] NOT IN (
    N'BROKER_EVENTHANDLER', 
    N'BROKER_RECEIVE_WAITFOR',
    N'BROKER_TASK_STOP', 
    N'BROKER_TO_FLUSH', 
    N'BROKER_TRANSMITTER', 
    N'CHECKPOINT_QUEUE',
    N'CHKPT', 
    N'CLR_AUTO_EVENT', 
    N'CLR_MANUAL_EVENT', 
    N'CLR_SEMAPHORE', 
    N'CXCONSUMER'
);
