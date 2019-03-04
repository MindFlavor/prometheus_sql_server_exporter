;WITH 
a AS (
SELECT 
	--*,
	TRIM(object_name) AS object_name,
	TRIM(counter_name) AS counter_name,
	--TRIM(instance_name) AS instance_name,
	CASE WHEN counter_name LIKE '%/sec%' 
	THEN 'counter'
	ELSE 'gauge'
	END AS grafana_counter_type,
	REPLACE(object_name, ' ', '') + N'_' + REPLACE(counter_name, ' ', '') AS grafana_name 
		
FROM sys.dm_os_performance_counters
)
-- strip leading instance name (before :)
,j AS (SELECT CASE WHEN CHARINDEX(':', object_name) > 0 THEN
		SUBSTRING(object_name, CHARINDEX(':', object_name) + 1, LEN(object_name))
	ELSE object_name 
	END AS object_name
	,counter_name, grafana_counter_type, grafana_name
	FROM a)
,b AS (SELECT object_name, counter_name, grafana_counter_type, TRANSLATE(grafana_name, '().-', '____') AS grafana_name FROM j)
,g20 AS (SELECT object_name, counter_name, grafana_counter_type, REPLACE(grafana_name, '&', '') AS grafana_name FROM b)
,c AS (SELECT object_name, counter_name, grafana_counter_type, REPLACE(grafana_name, '/', '_over_') AS grafana_name FROM g20)
,d AS (SELECT object_name, counter_name, grafana_counter_type, REPLACE(grafana_name, '>=', '_ge_') AS grafana_name FROM c)
,e AS (SELECT object_name, counter_name, grafana_counter_type, REPLACE(grafana_name, '>', '_gt_') AS grafana_name FROM d)
,f AS (SELECT object_name, counter_name, grafana_counter_type, REPLACE(grafana_name, '<=', '_le_') AS grafana_name FROM e)
,g AS (SELECT object_name, counter_name, grafana_counter_type, REPLACE(grafana_name, '<', '_lt_') AS grafana_name FROM f)
,h AS (SELECT object_name, counter_name, grafana_counter_type, REPLACE(grafana_name, '%', '_perc_') AS grafana_name FROM g)
-- remove trailing underscore
,i AS (SELECT object_name, counter_name, grafana_counter_type, 
	CASE WHEN SUBSTRING(REVERSE(grafana_name), 1, 1) = '_' THEN
		SUBSTRING(grafana_name, 0, LEN(grafana_name))
	ELSE
		grafana_name
	END AS grafana_name FROM h)
-- strip leading instance name (before :)
,r AS (SELECT object_name, counter_name, grafana_counter_type, 
	CASE WHEN CHARINDEX(':', grafana_name) > 0 THEN
		SUBSTRING(grafana_name, CHARINDEX(':', grafana_name) + 1, LEN(grafana_name))
	ELSE grafana_name 
	END AS grafana_name FROM i)
SELECT object_name, counter_name, grafana_counter_type, grafana_name FROM r
GROUP BY object_name, counter_name, grafana_counter_type, grafana_name;
