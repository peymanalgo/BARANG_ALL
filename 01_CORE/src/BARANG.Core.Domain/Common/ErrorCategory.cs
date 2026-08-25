namespace BARANG.Core.Domain.Common;

public enum ErrorCategory
{
    Validation = 1,
    BusinessRule = 2,
    NotFound = 3,
    Conflict = 4,
    Authorization = 5,
    Authentication = 6,
    Concurrency = 7,
    Infrastructure = 8,
    Unexpected = 9
}
