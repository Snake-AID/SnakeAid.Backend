-- Shift assignment redesign migration
-- Move from Date + WorkShift.StartTime/EndTime to ShiftStartLocal/ShiftEndLocal.

BEGIN;

ALTER TABLE "SnakeAid"."ShiftAssignments"
    ADD COLUMN IF NOT EXISTS "ShiftStartLocal" timestamp without time zone,
    ADD COLUMN IF NOT EXISTS "ShiftEndLocal" timestamp without time zone,
    ADD COLUMN IF NOT EXISTS "CheckInAtUtc" timestamp with time zone,
    ADD COLUMN IF NOT EXISTS "CheckOutAtUtc" timestamp with time zone;

UPDATE "SnakeAid"."ShiftAssignments" sa
SET
    "ShiftStartLocal" = (sa."Date"::timestamp + ws."StartTime"),
    "ShiftEndLocal" = CASE
        WHEN ws."EndTime" <= ws."StartTime"
            THEN (sa."Date"::timestamp + ws."EndTime" + interval '1 day')
        ELSE (sa."Date"::timestamp + ws."EndTime")
    END,
    "CheckInAtUtc" = COALESCE(sa."CheckInAtUtc", sa."CheckInAt"),
    "CheckOutAtUtc" = COALESCE(sa."CheckOutAtUtc", sa."CheckOutAt")
FROM "SnakeAid"."WorkShifts" ws
WHERE ws."Id" = sa."ShiftId"
  AND (sa."ShiftStartLocal" IS NULL OR sa."ShiftEndLocal" IS NULL);

ALTER TABLE "SnakeAid"."ShiftAssignments"
    ALTER COLUMN "ShiftStartLocal" SET NOT NULL,
    ALTER COLUMN "ShiftEndLocal" SET NOT NULL;

DROP INDEX IF EXISTS "SnakeAid"."IX_ShiftAssignments_Date";
DROP INDEX IF EXISTS "SnakeAid"."UX_ShiftAssignments_Rescuer_Shift_Date";

CREATE INDEX IF NOT EXISTS "IX_ShiftAssignments_ShiftStartLocal"
    ON "SnakeAid"."ShiftAssignments" ("ShiftStartLocal");

CREATE INDEX IF NOT EXISTS "IX_ShiftAssignments_ShiftEndLocal"
    ON "SnakeAid"."ShiftAssignments" ("ShiftEndLocal");

CREATE UNIQUE INDEX IF NOT EXISTS "UX_ShiftAssignments_Rescuer_Shift_ShiftStartLocal"
    ON "SnakeAid"."ShiftAssignments" ("RescuerId", "ShiftId", "ShiftStartLocal");

COMMIT;
