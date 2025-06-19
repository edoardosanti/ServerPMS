--
-- File generated with SQLiteStudio v3.4.13 on Thu Jun 19 23:59:04 2025
--
-- Text encoding used: UTF-8
--
PRAGMA foreign_keys = off;
BEGIN TRANSACTION;

-- Table: prod_orders
CREATE TABLE IF NOT EXISTS prod_orders (ID INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL, ID_pms INTEGER NOT NULL UNIQUE, part_code TEXT NOT NULL, part_desc TEXT NOT NULL, qty INTEGER NOT NULL, customer_ord_ref TEXT, default_prod_unit INTEGER, mold_id TEXT NOT NULL, mold_location TEXT NOT NULL, mold_notes TEXT NOT NULL, customer_name TEXT NOT NULL, delivery_facility TEXT NOT NULL, delivery_date TEXT NOT NULL, order_status TEXT NOT NULL);

-- Table: prod_units
CREATE TABLE IF NOT EXISTS prod_units (ID INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL, name TEXT NOT NULL UNIQUE, type TEXT NOT NULL, status TEXT NOT NULL, notes TEXT, current_production_order INTEGER);

-- Table: settings
CREATE TABLE IF NOT EXISTS settings (ID INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL, key TEXT NOT NULL, value TEXT NOT NULL);

-- Table: units_queues
CREATE TABLE IF NOT EXISTS units_queues (ID INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL, unit_id INTEGER NOT NULL, prod_order_id INTEGER NOT NULL, queue_pos INTEGER NOT NULL);

COMMIT TRANSACTION;
PRAGMA foreign_keys = on;
