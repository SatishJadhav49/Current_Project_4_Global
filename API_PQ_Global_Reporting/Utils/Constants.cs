namespace API_PQ_Global_Reporting.Utils
{
    public static class Constants
    {
        // API Response Messages
        public static class Messages
        {
            public const string SUCCESS = "Success";
            public const string ERROR = "Error";
            public const string ALERT = "Alert";
            public const string DUPLICATE = "Duplicate Record";
            public const string VALIDATION_ERROR = "Validation Error";
            public const string NOT_FOUND = "Record Not Found";
            public const string UNAUTHORIZED = "Unauthorized Access";
            public const string INTERNAL_ERROR = "Internal Server Error";
        }

        // API Response Details
        public static class Details
        {
            public const string OPERATION_SUCCESS = "Operation completed successfully";
            public const string OPERATION_FAILED = "Operation failed";
            public const string RECORD_CREATED = "Record created successfully";
            public const string RECORD_UPDATED = "Record updated successfully";
            public const string RECORD_DELETED = "Record deleted successfully";
            public const string RECORD_NOT_FOUND = "The requested record was not found";
            public const string DUPLICATE_RECORD =
                "A record with the same information already exists";
            public const string INVALID_INPUT = "Please check your input data";
            public const string SERVER_ERROR = "An unexpected error occurred on the server";
        }

        // Database Constants
        public static class Database
        {
            public const int DEFAULT_PAGE_SIZE = 10;
            public const int MAX_PAGE_SIZE = 100;
            public const int COMMAND_TIMEOUT = 30;
        }

        // Status Codes
        public static class StatusCodes
        {
            public const int SUCCESS = 200;
            public const int CREATED = 201;
            public const int BAD_REQUEST = 400;
            public const int UNAUTHORIZED = 401;
            public const int NOT_FOUND = 404;
            public const int CONFLICT = 409;
            public const int INTERNAL_ERROR = 500;
        }


        // Processes
        public static int ICA_Process_ID = 1;
        public static int PCA_Process_ID = 2;
        public static int Standardization = 3;
        public static int Submission = 4;
        public static int MfgApproval = 5;
        public static int EffectivenessMonitoring = 6;
        public static int QualityApproval = 7;
        public static int Closed = 8;

        public static string getStatusLogic(string Status)
        {
            return Status switch
            {
                "Open" => string.Join(",", OPENConcern),
                "Closed" => string.Join(",", CLOSEDConcern),
                _ => "0"
            };
        }

        public static string[] OPENConcern = { ICA_Process_ID.ToString(), PCA_Process_ID.ToString(), Standardization.ToString(), Submission.ToString(), MfgApproval.ToString(), EffectivenessMonitoring.ToString() };

        public static string[] CLOSEDConcern = { QualityApproval.ToString(), Closed.ToString() };


        public static int MASTER_USER = 1;
        public static int PU_HEAD = 2;
        public static int QUALITY_MANAGER = 3; // Quality Manager ID
        public static int MANUFACTURING_MANAGER = 4; // Manufacturing Manager ID
        public static int QUALITY_OFFICER = 5; // Quality Officer ID
        public static int MANUFACTURING_OFFICER = 6; // Manufacturing Officer ID


        //Process stages timeline from Concern Inserted date

        public static int ICA_Days = 1; // exact 24 hours
        public static int PCA_Days = 2; // 2 days - exact hours
        public static int Standardization_Days = 7; // 7 days - same for submission

        public static int SUBMISSION_REVIEW_Days = 2; // 2 days - same for submission



        // Dictionary with Mail Users
        public static Dictionary<string, int[]> MailUsers = new Dictionary<string, int[]>
        {
            { "CREATE_CONCERN_CC",new [] { PU_HEAD,QUALITY_MANAGER,MANUFACTURING_MANAGER,QUALITY_OFFICER } },
        };


        // Effectiveness Monitoring 
        public static int EFFECTIVNESS_DONE_AFTER_DAYS = 7;
    }


}
