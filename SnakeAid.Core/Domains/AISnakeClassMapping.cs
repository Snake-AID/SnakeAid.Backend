using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace SnakeAid.Core.Domains
{
    public class AISnakeClassMapping
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey(nameof(AIModel))]
        public int AIModelId { get; set; }  // FK to AI_Model

        [Required]
        [ForeignKey(nameof(SnakeSpecies))]
        public int SnakeSpeciesId { get; set; }  // FK to SnakeSpecies

        public string YoloClassName { get; set; }  // Tên class trong YOLO model (vd: "cobra", "viper", etc.)

        public int YoloClassId { get; set; }  // ID của class trong YOLO model

        public decimal Confidence { get; set; } = 0.8m;  // Ngưỡng confidence để accept

        [Required]
        public bool IsActive { get; set; } = true;


        // Navigation properties
        public AIModel AIModel { get; set; }
        public SnakeSpecies SnakeSpecies { get; set; }
    }
}