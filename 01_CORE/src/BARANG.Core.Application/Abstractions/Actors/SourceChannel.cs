namespace BARANG.Core.Application.Abstractions.Actors;

public enum SourceChannel
{
    Unknown = 0,
    Backoffice = 1,
    Web = 2,
    Mobile = 3,
    PartnerApi = 4,
    BackgroundJob = 5,
    System = 6
}
