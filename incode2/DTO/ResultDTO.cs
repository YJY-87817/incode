namespace incode2.DTO
{
    public class ResultDTO<T>
    {
        public bool Ok { get; set; }
        public int Code { get; set; }
        public string Message { get; set; }
        public T Data { get; set; }

        // 靜態方法：成功時調用
        public static ResultDTO<T> Success(T data, string message = "操作成功", int code = 200)
        {
            return new ResultDTO<T> { Ok = true, Code = code, Message = message, Data = data };
        }

        // 靜態方法：失敗時調用
        public static ResultDTO<T> Failure(string message, int code = 400)
        {
            return new ResultDTO<T> { Ok = false, Code = code, Message = message, Data = default };
        }
    }

    // 為了相容不帶資料的回傳，可以再定義一個非泛型版本或繼承
    public class ResultDTO : ResultDTO<object> { }
}
