namespace API_PQ_Global_Reporting.Utils
{
    public class ApiResponse<T>
    {
        public ResponseFlags res { get; set; }
        public T? datalist { get; set; }

        public ApiResponse()
        {
            res = new ResponseFlags();
        }

        public ApiResponse(T data)
        {
            res = new ResponseFlags();
            datalist = data;
        }

        public ApiResponse(bool isSuccess, string title, string detail, T? data = default)
        {
            res = new ResponseFlags
            {
                isSuccessMessage = isSuccess,
                isErrorMessage = !isSuccess,
                messageTitle = title,
                messageDetail = detail
            };
            datalist = data;
        }
    }

    public class ResponseFlags
    {
        public bool isSuccessMessage { get; set; } = false;
        public bool isErrorMessage { get; set; } = false;
        public bool isAlertMessage { get; set; } = false;
        public bool isDublicateMessage { get; set; } = false;
        public string messageTitle { get; set; } = string.Empty;
        public string messageDetail { get; set; } = string.Empty;
    }
}
