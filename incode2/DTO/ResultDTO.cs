namespace incode2.DTO
{
    public class ResultDTO
    {
        public bool Ok { get; set; }
        public int Code { get; set; }
        public string? Message { get; set; }
        public object? Data { get; set; }  // 視需要可加入
    }
}