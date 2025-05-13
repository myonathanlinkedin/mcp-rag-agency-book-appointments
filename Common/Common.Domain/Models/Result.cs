public class Result
{
    protected Result(bool succeeded, IEnumerable<string> errors)
    {
        Succeeded = succeeded;
        Errors = errors.ToArray();
    }

    public bool Succeeded { get; }

    public string[] Errors { get; }

    public static Result Success => new Result(true, Array.Empty<string>());

    public static Result Failure(IEnumerable<string> errors) => new Result(false, errors);

    public static implicit operator Result(string error)
        => Failure(new List<string> { error });

    public static implicit operator Result(List<string> errors)
        => Failure(errors.ToList());

    public static implicit operator Result(bool success)
        => success ? Success : Failure(new[] { "Unsuccessful operation." });

    public static implicit operator bool(Result result)
        => result.Succeeded;
}

public class Result<T> : Result
{
    private readonly T? data;

    protected internal Result(T? data, bool succeeded, IEnumerable<string> errors)
        : base(succeeded, errors)
    {
        this.data = data;
    }

    public T Data
    {
        get
        {
            if (!Succeeded || data == null)
            {
                throw new InvalidOperationException($"Data is not available with a failed result. Use {typeof(T).Name} instead.");
            }

            return data;
        }
    }

    public T? DataOrDefault => Succeeded ? data : default;

    public static Result<T> SuccessWith(T data) => new Result<T>(data, true, Array.Empty<string>());

    public static new Result<T> Failure(IEnumerable<string> errors) => new Result<T>(default, false, errors);

    public static implicit operator Result<T>(string error)
        => Failure(new List<string> { error });

    public static implicit operator Result<T>(List<string> errors)
        => Failure(errors);

    public static implicit operator Result<T>(T data)
        => SuccessWith(data);

    public static implicit operator bool(Result<T> result)
        => result.Succeeded;
} 