namespace BARANG.Core.Application.Abstractions.Companies;

public interface ICompanyContext
{
    Guid? CompanyId { get; }

    bool IsAvailable { get; }
}
