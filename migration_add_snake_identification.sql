-- Migration: Add Snake Identification fields to SnakebiteIncident
-- Description: Thêm các field để lưu thông tin xác định loài rắn cho incident

-- Add new columns to snakebite_incidents table
ALTER TABLE "snakebite_incidents" 
ADD COLUMN "identified_snake_species_id" INTEGER NULL,
ADD COLUMN "identification_method" INTEGER NOT NULL DEFAULT 0,
ADD COLUMN "ai_recognition_result_id" UUID NULL,
ADD COLUMN "filter_answers" JSONB NULL,
ADD COLUMN "identified_at" TIMESTAMP NULL;

-- Add foreign key constraints
ALTER TABLE "snakebite_incidents"
ADD CONSTRAINT "fk_snakebite_incidents_snake_species" 
    FOREIGN KEY ("identified_snake_species_id") 
    REFERENCES "snake_species"("id") 
    ON DELETE SET NULL;

ALTER TABLE "snakebite_incidents"
ADD CONSTRAINT "fk_snakebite_incidents_ai_recognition_result" 
    FOREIGN KEY ("ai_recognition_result_id") 
    REFERENCES "snake_ai_recognition_results"("id") 
    ON DELETE SET NULL;

-- Add index for better query performance
CREATE INDEX "idx_snakebite_incidents_identified_snake_species" 
    ON "snakebite_incidents"("identified_snake_species_id");

CREATE INDEX "idx_snakebite_incidents_identification_method" 
    ON "snakebite_incidents"("identification_method");

-- Comment on columns
COMMENT ON COLUMN "snakebite_incidents"."identified_snake_species_id" IS 'Loài rắn đã được xác định';
COMMENT ON COLUMN "snakebite_incidents"."identification_method" IS '0=None, 1=AIDetection, 2=FilterQuestions, 3=ManualByRescuer, 4=ExpertVerified';
COMMENT ON COLUMN "snakebite_incidents"."ai_recognition_result_id" IS 'ID của kết quả AI recognition nếu xác định bằng AI';
COMMENT ON COLUMN "snakebite_incidents"."filter_answers" IS 'Dữ liệu câu trả lời filter questions nếu xác định bằng filter';
COMMENT ON COLUMN "snakebite_incidents"."identified_at" IS 'Thời điểm xác định được loài rắn';
