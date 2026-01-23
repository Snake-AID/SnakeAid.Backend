namespace SnakeAid.Core.Domains
{
    public class AISnakeClassMapping
    {
        public Guid Id { get; set; }
        public Guid AIModelId { get; set; }  // FK to AI_Model
        public int SnakeSpeciesId { get; set; }  // FK to SnakeSpecies
        public string YoloClassName { get; set; }  // Tên class trong YOLO model (vd: "cobra", "viper", etc.)
        public int YoloClassId { get; set; }  // ID của class trong YOLO model
        public decimal Confidence { get; set; } = 0.8m;  // Ngưỡng confidence để accept
        public bool IsActive { get; set; } = true;

        // Navigation properties
        public AIModel AIModel { get; set; }
        public SnakeSpecies SnakeSpecies { get; set; }
    }
}