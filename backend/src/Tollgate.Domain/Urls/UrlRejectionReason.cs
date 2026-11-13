// Why a user-supplied string was refused as a scannable URL.
namespace Tollgate.Domain.Urls;

public enum UrlRejectionReason
{
    Empty,
    TooLong,
    InvalidFormat,
    UnsupportedScheme,
}
