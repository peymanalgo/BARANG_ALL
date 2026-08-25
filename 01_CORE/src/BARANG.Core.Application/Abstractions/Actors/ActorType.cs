namespace BARANG.Core.Application.Abstractions.Actors;

public enum ActorType
{
    Anonymous = 0,
    InternalUser = 1,
    B2CUser = 2,
    MobileUser = 3,
    BusinessPartner = 4,
    ApiClient = 5,
    BackgroundJob = 6,
    System = 7
}
