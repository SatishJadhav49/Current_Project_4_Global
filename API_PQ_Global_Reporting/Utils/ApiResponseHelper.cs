namespace API_PQ_Global_Reporting.Utils
{
    public interface IApiResponseHelper
    {
        ApiResponse<T> CreateSuccessResponse<T>(T data, string title = "Success", string detail = "Operation completed successfully");
        ApiResponse<T> CreateErrorResponse<T>(string title = "Error", string detail = "An error occurred", T? data = default);
        ApiResponse<T> CreateAlertResponse<T>(string title = "Alert", string detail = "Please note", T? data = default);
        ApiResponse<T> CreateDuplicateResponse<T>(string title = "Duplicate", string detail = "Record already exists", T? data = default);
        ApiResponse<object> CreateValidationErrorResponse(string title = "Validation Error", string detail = "Please check your input");
    }

    public class ApiResponseHelper : IApiResponseHelper
    {
        public ApiResponse<T> CreateSuccessResponse<T>(T data, string title = "Success", string detail = "Operation completed successfully")
        {
            return new ApiResponse<T>
            {
                res = new ResponseFlags
                {
                    isSuccessMessage = true,
                    messageTitle = title,
                    messageDetail = detail
                },
                datalist = data
            };
        }

        public ApiResponse<T> CreateErrorResponse<T>(string title = "Error", string detail = "An error occurred", T? data = default)
        {
            return new ApiResponse<T>
            {
                res = new ResponseFlags
                {
                    isErrorMessage = true,
                    messageTitle = title,
                    messageDetail = detail
                },
                datalist = data
            };
        }

        public ApiResponse<T> CreateAlertResponse<T>(string title = "Alert", string detail = "Please note", T? data = default)
        {
            return new ApiResponse<T>
            {
                res = new ResponseFlags
                {
                    isAlertMessage = true,
                    messageTitle = title,
                    messageDetail = detail
                },
                datalist = data
            };
        }

        public ApiResponse<T> CreateDuplicateResponse<T>(string title = "Duplicate", string detail = "Record already exists", T? data = default)
        {
            return new ApiResponse<T>
            {
                res = new ResponseFlags
                {
                    isDublicateMessage = true,
                    messageTitle = title,
                    messageDetail = detail
                },
                datalist = data
            };
        }

        public ApiResponse<object> CreateValidationErrorResponse(string title = "Validation Error", string detail = "Please check your input")
        {
            return new ApiResponse<object>
            {
                res = new ResponseFlags
                {
                    isErrorMessage = true,
                    messageTitle = title,
                    messageDetail = detail
                },
                datalist = null
            };
        }
    }
}
