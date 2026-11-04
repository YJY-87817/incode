namespace incode.MVC.Dtos.Contents
{
  public class MyInteractionsDto
  {
    public bool Like { get; set; }
    public bool Hide { get; set; }
    public bool Bookmark { get; set; }
    public bool Report { get; set; }
    public List<int> Categories { get; set; } = new();
    public Dictionary<string, bool> Map { get; set; } = new()
    {
      ["1"] = false,
      ["2"] = false,
      ["3"] = false,
      ["4"] = false
    };
  }
}
