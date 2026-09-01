using BARANG.Core.Application.Logistics;using BARANG.Core.Domain.Common;using BARANG.Core.Domain.Logistics;
namespace BARANG.Core.Infrastructure.Logistics;
public sealed class LocalCarrierService:ICarrierService{public Result<string>NormalizeTracking(string carrierCode,string trackingCode){string?c=LogisticsText.Code(carrierCode,50),t=LogisticsText.Code(trackingCode,100);return c is null||t is null?Result.Failure<string>(LogisticsErrors.Invalid):Result.Success(t);}}
