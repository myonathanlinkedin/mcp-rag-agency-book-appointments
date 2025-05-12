public class CommonModelConstants
{
    public class Common
    {
        public const int Zero = 0;
        public const int MinNameLength = 3;
        public const int MaxNameLength = 50;
        public const int MaxUrlLength = 2048;
    }
    
    public class Identity
    {
        public const int MinEmailLength = 3;
        public const int MaxEmailLength = 50;
        public const int MinPasswordLength = 6;
        public const int MaxPasswordLength = 32;
        public const int DefaultPasswordLength = 8;
    }

    public class Role
    {
        public const string Administrator = "Administrator";
        public const string Prompter = "Prompter";
        public const string Agency = "Agency";
    }

    public class Policy
    {
        public const string AdminAccess = "AdminAccess";
    }

    public class KafkaOperation
    {
        // Domain actions
        public const string Created = "Created";
        public const string Rescheduled = "Rescheduled";
        public const string NoShow = "NoShow";
        public const string Cancelled = "Cancelled";

        // CRUD operations
        public const string Insert = "INSERT";
        public const string Update = "UPDATE";
        public const string Delete = "DELETE";
    }
}