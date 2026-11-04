using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace incode.MVC.ViewModels.Contents
{
  public class ContentEditViewModel
  {
    [Display(Name="內文ID")]
    public int ContentId { get; set; }

    public int? ParentContentId { get; set; }

    public int? CategoryId { get; set; }

    public int UserId { get; set; }

    public string Title { get; set; }

    public string Body { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool? Ishidden { get; set; }

    public int ContentHistoryId { get; set; }

    public string OldTitle { get; set; }

    public string OldBody { get; set; }

    public bool? HistoryIshidden { get; set; }

    public DateTime? EditedAt { get; set; }
  }
}
