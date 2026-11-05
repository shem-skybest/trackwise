-- Runs once, only when the postgres container's data volume is empty (first boot).
-- POSTGRES_DB in docker-compose.yml already creates master_trackwise; this adds the
-- tenant database(s). Add one CREATE DATABASE line per tenant you provision.
CREATE DATABASE skybest_trackwise;
