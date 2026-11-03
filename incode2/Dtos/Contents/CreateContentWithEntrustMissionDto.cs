using System.ComponentModel.DataAnnotations;

namespace incode2.Dtos.Contents
{
  public class CreateEntrustMissionDto
  {
    [Required]
    [StringLength(150)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    public string Detail { get; set; } = string.Empty;

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "賞金必須大於 0")]
    public int RewardPoints { get; set; }

    [StringLength(50)]
    public string Category { get; set; } = "其他";

    public DateTime? Deadline { get; set; }

    public bool IsUrgent { get; set; }
  }

  public class CreateContentWithEntrustMissionDto
  {
    public int? ParentContentId { get; set; }
    public int? CategoryId { get; set; }

    [Required]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Body { get; set; } = string.Empty;

    public int Type { get; set; }

    public CreateEntrustMissionDto? EntrustMission { get; set; }
  }

  public class CreateContentWithEntrustMissionResultDto
  {
    public ContentDto Content { get; set; } = null!;
    public ResultDTO EntrustMission { get; set; } = null!;
  }
}
