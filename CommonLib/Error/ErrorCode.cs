namespace CommonLib.Error;

using System.Net;

/// <summary>
/// Canonical error codes exposed across services.
/// Stable machine-readable identifiers that clients can branch on without parsing human-readable messages.
/// Naming convention: &lt;DOMAIN&gt;-&lt;NNNN&gt;.
/// </summary>
public static class ErrorCode
{
    // ── Generic (COMMON) ────────────────────────────────────────────────
    public const string ValidationFailed   = "COMMON-400";
    public const string Unauthorized       = "COMMON-401";
    public const string Forbidden          = "COMMON-403";
    public const string NotFound           = "COMMON-404";
    public const string Conflict           = "COMMON-409";
    public const string InternalError      = "COMMON-500";
    public const string ServiceUnavailable = "COMMON-503";

    // ── App-level fallbacks emitted by GlobalExceptionHandler ──────────
    public const string AppBadRequest    = "APP-400";
    public const string AppUnauthorized  = "APP-401";
    public const string AppForbidden     = "APP-403";
    public const string AppConflict      = "APP-409";
    public const string AppUnprocessable = "APP-422";
    public const string AppInternal      = "APP-500";

    // ── Auth domain (AUTH-1xxx) ─────────────────────────────────────────
    public const string InvalidCredentials     = "AUTH-1001";
    public const string UserAlreadyExists      = "AUTH-1002";
    public const string UserNotFound           = "AUTH-1003";
    public const string TokenExpired           = "AUTH-1004";
    public const string TokenInvalid           = "AUTH-1005";
    public const string AccountLocked          = "AUTH-1006";
    public const string AccountDisabled        = "AUTH-1007";
    public const string RefreshTokenInvalid    = "AUTH-1008";
    public const string RoleNotFound           = "AUTH-1009";
    public const string PermissionNotFound     = "AUTH-1010";
    public const string TokenRevoked           = "AUTH-1011";
    public const string TokenReuseDetected     = "AUTH-1012";
    public const string TokenOwnershipFail     = "AUTH-1013";
    public const string AuthRateLimited        = "AUTH-1014";
    public const string AuthBlacklisted        = "AUTH-1015";
    public const string AccountExpired         = "AUTH-1016";
    public const string CredentialsExpired     = "AUTH-1017";
    public const string OAuthTokenInvalid      = "AUTH-1018";
    public const string OAuthEmailUnverified   = "AUTH-1019";
    public const string OAuthAccountLinked     = "AUTH-1020";

    // ── Catalog domain (CATALOG-2xxx) ──────────────────────────────────
    public const string ProductNotFound          = "CATALOG-2001";
    public const string ProductAlreadyExists     = "CATALOG-2002";
    public const string ProductInvalidState      = "CATALOG-2003";
    public const string VariantNotFound          = "CATALOG-2101";
    public const string VariantSkuAlreadyExists  = "CATALOG-2102";
    public const string VariantInvalid           = "CATALOG-2103";
    public const string CategoryNotFound         = "CATALOG-2201";
    public const string CategoryAlreadyExists    = "CATALOG-2202";
    public const string CategoryCycle            = "CATALOG-2203";
    public const string AttrNotFound             = "CATALOG-2301";
    public const string AttrAlreadyExists        = "CATALOG-2302";
    public const string AttrValNotFound          = "CATALOG-2303";
    public const string AttrInvalidType          = "CATALOG-2304";

    // ── Order domain (ORDER-3xxx) ──────────────────────────────────────
    public const string OrderNotFound            = "ORDER-3001";
    public const string OrderAlreadyExists       = "ORDER-3002";
    public const string OrderInvalidState        = "ORDER-3003";
    public const string OrderInsufficientStock   = "ORDER-3004";
    public const string OrderDuplicate           = "ORDER-3005";
    public const string OrderServiceUnavailable  = "ORDER-3006";
    public const string OrderCancelled           = "ORDER-3007";
    public const string OrderEmptyCart           = "ORDER-3008";
    public const string ReturnRequestNotFound    = "ORDER-3101";
    public const string ReturnRequestInvalid     = "ORDER-3102";

    // ── Voucher domain (VOUCHER-4xxx) ──────────────────────────────────
    public const string VoucherNotFound     = "VOUCHER-4004";
    public const string VoucherLimitReached = "VOUCHER-4009";
    public const string VoucherRejected     = "VOUCHER-4022";

    // ── Cart domain (CART-5xxx) ────────────────────────────────────────
    public const string CartValidation   = "CART-5400";
    public const string CartUnauthorized = "CART-5401";
    public const string CartForbidden    = "CART-5403";
    public const string CartNotFound     = "CART-5404";
    public const string CartConflict     = "CART-5409";
    public const string CartInternal     = "CART-5500";

    // ── Inventory domain (INVENTORY-6xxx) ──────────────────────────────
    public const string InventoryNotFound     = "INVENTORY-6004";
    public const string InventoryInsufficient = "INVENTORY-6009";

    // ── Shipping domain (SHIPPING-7xxx) ────────────────────────────────
    public const string ShippingForbidden     = "SHIPPING-7403";
    public const string ShipmentNotFound      = "SHIPPING-7404";
    public const string ShipmentDuplicate     = "SHIPPING-7409";
    public const string ShipmentInvalidState  = "SHIPPING-7422";

    // ── Gateway domain (GATEWAY-5xxx) ──────────────────────────────────
    public const string GatewayTimeout       = "GATEWAY-504";
    public const string GatewayBadGateway    = "GATEWAY-502";
    public const string GatewayInternalError = "GATEWAY-500";

    /// <summary>
    /// Translates canonical error code to standard HttpStatusCode.
    /// </summary>
    public static HttpStatusCode ToHttpStatusCode(string code) => (HttpStatusCode)ToStatusCode(code);

    /// <summary>
    /// Translates canonical error code to integer HTTP status code.
    /// </summary>
    public static int ToStatusCode(string code) => code switch
    {
        // 400 Bad Request
        ValidationFailed or AppBadRequest or CartValidation => 400,

        // 401 Unauthorized
        Unauthorized or AppUnauthorized or InvalidCredentials or TokenExpired or TokenInvalid or
        RefreshTokenInvalid or TokenRevoked or TokenReuseDetected or AuthBlacklisted or
        CredentialsExpired or OAuthTokenInvalid or CartUnauthorized => 401,

        // 403 Forbidden
        Forbidden or AppForbidden or AccountLocked or AccountDisabled or TokenOwnershipFail or
        AccountExpired or OAuthEmailUnverified or CartForbidden or ShippingForbidden => 403,

        // 404 Not Found
        NotFound or UserNotFound or RoleNotFound or PermissionNotFound or ProductNotFound or
        VariantNotFound or CategoryNotFound or AttrNotFound or AttrValNotFound or
        OrderNotFound or ReturnRequestNotFound or VoucherNotFound or CartNotFound or
        InventoryNotFound or ShipmentNotFound => 404,

        // 409 Conflict
        Conflict or AppConflict or UserAlreadyExists or OAuthAccountLinked or
        ProductAlreadyExists or VariantSkuAlreadyExists or CategoryAlreadyExists or
        AttrAlreadyExists or OrderAlreadyExists or OrderInsufficientStock or
        OrderDuplicate or OrderCancelled or VoucherLimitReached or CartConflict or
        InventoryInsufficient or ShipmentDuplicate => 409,

        // 422 Unprocessable Entity
        AppUnprocessable or ProductInvalidState or VariantInvalid or CategoryCycle or
        AttrInvalidType or OrderInvalidState or OrderEmptyCart or ReturnRequestInvalid or
        VoucherRejected or ShipmentInvalidState => 422,

        // 429 Too Many Requests
        AuthRateLimited => 429,

        // 502 Bad Gateway
        GatewayBadGateway => 502,

        // 503 Service Unavailable
        ServiceUnavailable or OrderServiceUnavailable => 503,

        // 504 Gateway Timeout
        GatewayTimeout => 504,

        // 500 Internal Server Error (default)
        _ => 500
    };
}
