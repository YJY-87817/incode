namespace incode2.Dtos
{
  public class ContentListResponseDto
  {
    public List<ContentListItemDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public bool ReplyStatsIncluded { get; set; }
  }
}
