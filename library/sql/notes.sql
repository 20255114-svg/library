-- notes.sql : the notepad table for the Midterm Practical
 
-- Run this once in the Supabase SQL Editor. It removes any existing note table and rebuilds it.
 
 
 
DROP TABLE IF EXISTS lending.note;
 
 
 
CREATE TABLE lending.note (
 
    note_id     BIGINT        GENERATED ALWAYS AS IDENTITY,
 
    title       VARCHAR(100)  NOT NULL,
 
    body        TEXT,
 
    created_on  DATE          NOT NULL DEFAULT CURRENT_DATE,
 
    CONSTRAINT pk_note PRIMARY KEY (note_id)
 
);
 
 
 
INSERT INTO lending.note (title, body, created_on) VALUES
 
    ('Order new textbooks',    'Ask the dean''s office for the budget before Friday.', '2026-10-01'),
 
    ('Shelf C is full',        NULL,                                                   '2026-10-03'),
 
    ('Library week poster',    'Theme: read local. Use the Benguet photos.',           '2026-10-05'),
 
    ('Return the projector',   NULL,                                                   '2026-10-06'),
 
    ('Check overdue loans',    'Members with books out longer than 14 days.',          '2026-10-08'),
 
    ('Wi-Fi password changed', NULL,                                                   '2026-10-09'),
 
    ('Inventory count',        'Count the Classic section first.',                     '2026-10-12');
 
 
 
SELECT COUNT(*) AS notes, COUNT(body) AS with_body FROM lending.note;