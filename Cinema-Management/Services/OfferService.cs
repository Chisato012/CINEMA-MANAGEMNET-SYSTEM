using Cinema_Management.Models;

namespace Cinema_Management.Services;

public interface IOfferService
{
    IReadOnlyList<OfferViewModel> GetOffers(DateTime today);
    OfferValidationResult ValidateCode(string? code, DateTime today);
}

public sealed class MockOfferService : IOfferService
{
    public IReadOnlyList<OfferViewModel> GetOffers(DateTime today)
    {
        var currentMonthStart = new DateTime(today.Year, today.Month, 1);
        var nextMonthEnd = currentMonthStart.AddMonths(2).AddDays(-1);

        var rawOffers = new[]
        {
            Create(
                id: "happy45",
                title: "Thứ Tư đồng giá 45K",
                summary: "Xem phim thả ga, giá chỉ 45.000đ cho mọi suất chiếu 2D vào Thứ Tư hàng tuần.",
                description: "Áp dụng cho vé 2D đặt trực tuyến vào Thứ Tư. Số lượng ưu đãi có giới hạn theo từng cụm rạp.",
                code: "HAPPY45",
                category: "ticket",
                categoryLabel: "Giảm giá vé",
                badge: "ƯU ĐÃI NỔI BẬT",
                discountType: "special_price",
                discountValue: 45_000,
                displayValue: "45K",
                startDate: today.AddDays(-14),
                endDate: today.AddDays(2),
                isFeatured: true,
                isOnlineOnly: true,
                isMemberOnly: false,
                maximumDiscount: null,
                minimumOrder: null,
                terms:
                [
                    "Áp dụng cho suất chiếu 2D vào Thứ Tư.",
                    "Không áp dụng đồng thời với ưu đãi khác.",
                    "Không áp dụng cho ghế đôi và các suất chiếu đặc biệt."
                ]),
            Create(
                id: "new20",
                title: "Chào mừng thành viên mới",
                summary: "Giảm 20% cho đơn đặt vé đầu tiên khi đăng ký tài khoản.",
                description: "Ưu đãi dành riêng cho tài khoản mới tại COSMOS Cinema khi đặt vé trực tuyến lần đầu.",
                code: "NEW20",
                category: "member",
                categoryLabel: "Thành viên",
                badge: "ONLINE ONLY",
                discountType: "percentage",
                discountValue: 20,
                displayValue: "-20%",
                startDate: today.AddDays(-8),
                endDate: nextMonthEnd,
                isFeatured: true,
                isOnlineOnly: true,
                isMemberOnly: true,
                maximumDiscount: 50_000,
                minimumOrder: 100_000,
                terms:
                [
                    "Chỉ áp dụng cho tài khoản COSMOS mới.",
                    "Giảm tối đa 50.000đ cho đơn vé đầu tiên.",
                    "Không áp dụng đồng thời với ưu đãi khác."
                ]),
            Create(
                id: "cosmos99",
                title: "Combo Cosmos 99K",
                summary: "01 bắp lớn và 02 nước chỉ với 99.000đ.",
                description: "Combo bắp nước dành cho buổi xem phim cùng bạn bè hoặc gia đình tại COSMOS Cinema.",
                code: "COSMOS99",
                category: "combo",
                categoryLabel: "Combo",
                badge: "HOT",
                discountType: "special_price",
                discountValue: 99_000,
                displayValue: "99K",
                startDate: today.AddDays(-10),
                endDate: today.AddDays(9),
                isFeatured: true,
                isOnlineOnly: false,
                isMemberOnly: false,
                maximumDiscount: null,
                minimumOrder: null,
                terms:
                [
                    "Áp dụng khi mua kèm vé xem phim.",
                    "Có thể đổi sản phẩm tương đương tùy tình trạng quầy.",
                    "Không quy đổi thành tiền mặt."
                ]),
            Create(
                id: "group10",
                title: "Ưu đãi nhóm",
                summary: "Giảm 10% khi đặt từ 4 vé trở lên.",
                description: "Ưu đãi dành cho nhóm bạn, gia đình hoặc đồng nghiệp khi đặt nhiều vé trong cùng một giao dịch.",
                code: "GROUP10",
                category: "ticket",
                categoryLabel: "Giảm giá vé",
                badge: "NHÓM",
                discountType: "percentage",
                discountValue: 10,
                displayValue: "-10%",
                startDate: today.AddDays(-5),
                endDate: nextMonthEnd.AddMonths(1),
                isFeatured: false,
                isOnlineOnly: true,
                isMemberOnly: false,
                maximumDiscount: 80_000,
                minimumOrder: 240_000,
                terms:
                [
                    "Áp dụng khi đặt tối thiểu 4 vé.",
                    "Không áp dụng cho suất chiếu sớm, sự kiện đặc biệt.",
                    "Mỗi tài khoản dùng tối đa 2 lần trong tháng."
                ]),
            Create(
                id: "morning15",
                title: "Khung giờ buổi sáng",
                summary: "Giảm 15% cho các suất chiếu trước 12:00.",
                description: "Bắt đầu ngày mới với một suất chiếu nhẹ nhàng và mức giá dễ chịu tại COSMOS Cinema.",
                code: "MORNING15",
                category: "ticket",
                categoryLabel: "Giảm giá vé",
                badge: "BUỔI SÁNG",
                discountType: "percentage",
                discountValue: 15,
                displayValue: "-15%",
                startDate: today.AddDays(-6),
                endDate: today.AddDays(5),
                isFeatured: false,
                isOnlineOnly: true,
                isMemberOnly: false,
                maximumDiscount: 40_000,
                minimumOrder: null,
                terms:
                [
                    "Áp dụng cho suất chiếu bắt đầu trước 12:00.",
                    "Không áp dụng vào ngày lễ.",
                    "Không áp dụng đồng thời với ưu đãi khác."
                ]),
            Create(
                id: "online30",
                title: "Thanh toán online",
                summary: "Giảm tối đa 30.000đ khi thanh toán trực tuyến.",
                description: "Ưu đãi dành cho đơn hàng thanh toán qua ví điện tử hoặc thẻ ngân hàng trực tuyến.",
                code: "ONLINE30",
                category: "payment",
                categoryLabel: "Thanh toán",
                badge: "ONLINE ONLY",
                discountType: "fixed",
                discountValue: 30_000,
                displayValue: "-30K",
                startDate: today.AddDays(-3),
                endDate: today.AddDays(15),
                isFeatured: false,
                isOnlineOnly: true,
                isMemberOnly: false,
                maximumDiscount: 30_000,
                minimumOrder: 150_000,
                terms:
                [
                    "Áp dụng cho thanh toán trực tuyến.",
                    "Giảm tối đa 30.000đ mỗi đơn.",
                    "Không áp dụng cho đơn thanh toán tại quầy."
                ]),
            Create(
                id: "member-cosmos",
                title: "Thành viên Cosmos",
                summary: "Tích điểm đổi quà, ưu đãi sinh nhật và nhiều đặc quyền khác.",
                description: "Gia nhập COSMOS Member để nhận ưu đãi sinh nhật, tích điểm, đổi quà và nhận lịch chiếu sớm.",
                code: null,
                category: "member",
                categoryLabel: "Thành viên",
                badge: "MEMBER",
                discountType: "benefit",
                discountValue: null,
                displayValue: "MEMBER",
                startDate: today.AddDays(-30),
                endDate: null,
                isFeatured: false,
                isOnlineOnly: false,
                isMemberOnly: true,
                maximumDiscount: null,
                minimumOrder: null,
                terms:
                [
                    "Đăng nhập tài khoản COSMOS để tích điểm.",
                    "Ưu đãi thành viên thay đổi theo từng hạng.",
                    "Không quy đổi điểm thưởng thành tiền mặt."
                ]),
            Create(
                id: "birthday30",
                title: "Sinh nhật rộn ràng",
                summary: "Giảm 30% cho vé 2D trong tháng sinh nhật.",
                description: "COSMOS gửi lời chúc sinh nhật bằng ưu đãi dành cho tài khoản thành viên đã cập nhật ngày sinh.",
                code: "BDAY30",
                category: "member",
                categoryLabel: "Thành viên",
                badge: "SINH NHẬT",
                discountType: "percentage",
                discountValue: 30,
                displayValue: "-30%",
                startDate: today.AddDays(-2),
                endDate: today.AddDays(1),
                isFeatured: false,
                isOnlineOnly: true,
                isMemberOnly: true,
                maximumDiscount: 60_000,
                minimumOrder: null,
                terms:
                [
                    "Áp dụng trong tháng sinh nhật của thành viên.",
                    "Cần cập nhật ngày sinh trong hồ sơ tài khoản.",
                    "Mỗi thành viên dùng 1 lần trong tháng sinh nhật."
                ]),
            Create(
                id: "early-week",
                title: "Đầu tuần nhẹ giá",
                summary: "Ưu đãi sẽ mở cho các suất chiếu đầu tuần trong thời gian tới.",
                description: "Một ưu đãi mới cho các suất chiếu đầu tuần, dự kiến mở trong vài ngày tới.",
                code: "EARLYWEEK",
                category: "ticket",
                categoryLabel: "Giảm giá vé",
                badge: "SẮP DIỄN RA",
                discountType: "percentage",
                discountValue: 12,
                displayValue: "-12%",
                startDate: today.AddDays(4),
                endDate: today.AddDays(24),
                isFeatured: false,
                isOnlineOnly: true,
                isMemberOnly: false,
                maximumDiscount: 35_000,
                minimumOrder: null,
                terms:
                [
                    "Chỉ áp dụng sau ngày bắt đầu chương trình.",
                    "Không áp dụng đồng thời với ưu đãi khác."
                ]),
            Create(
                id: "archive59",
                title: "Happy Day 59K",
                summary: "Chương trình đã kết thúc.",
                description: "Ưu đãi vé 2D Happy Day đã kết thúc và chỉ còn hiển thị để tham khảo điều kiện.",
                code: "HAPPY59",
                category: "ticket",
                categoryLabel: "Giảm giá vé",
                badge: "ĐÃ HẾT HẠN",
                discountType: "special_price",
                discountValue: 59_000,
                displayValue: "59K",
                startDate: today.AddDays(-30),
                endDate: today.AddDays(-1),
                isFeatured: false,
                isOnlineOnly: true,
                isMemberOnly: false,
                maximumDiscount: null,
                minimumOrder: null,
                terms:
                [
                    "Chương trình đã hết hạn.",
                    "Không thể áp dụng cho đơn hàng mới."
                ])
        };

        return rawOffers
            .Select(offer => Enrich(offer, today))
            .ToList();
    }

    public OfferValidationResult ValidateCode(string? code, DateTime today)
    {
        var normalizedCode = (code ?? string.Empty).Trim().ToUpperInvariant();

        if (string.IsNullOrWhiteSpace(normalizedCode))
        {
            return new OfferValidationResult
            {
                Status = "invalid",
                Message = "Vui lòng nhập mã ưu đãi."
            };
        }

        var offer = GetOffers(today)
            .FirstOrDefault(item => string.Equals(item.Code, normalizedCode, StringComparison.OrdinalIgnoreCase));

        if (offer == null)
        {
            return new OfferValidationResult
            {
                Status = "invalid",
                Message = "Mã không tồn tại, đã hết hạn hoặc không đủ điều kiện."
            };
        }

        if (offer.Status == "expired")
        {
            return new OfferValidationResult
            {
                Status = "expired",
                Offer = offer,
                Message = "Mã ưu đãi đã hết hạn."
            };
        }

        if (offer.Status == "upcoming")
        {
            return new OfferValidationResult
            {
                Status = "upcoming",
                Offer = offer,
                Message = $"Mã ưu đãi bắt đầu từ {offer.StartDate:dd/MM/yyyy}."
            };
        }

        return new OfferValidationResult
        {
            IsValid = true,
            Status = "active",
            Offer = offer,
            Message = $"Mã hợp lệ: {offer.Summary}"
        };
    }

    private static OfferViewModel Create(
        string id,
        string title,
        string summary,
        string description,
        string? code,
        string category,
        string categoryLabel,
        string? badge,
        string discountType,
        decimal? discountValue,
        string displayValue,
        DateTime startDate,
        DateTime? endDate,
        bool isFeatured,
        bool isOnlineOnly,
        bool isMemberOnly,
        decimal? maximumDiscount,
        decimal? minimumOrder,
        IReadOnlyList<string> terms)
    {
        return new OfferViewModel
        {
            Id = id,
            Title = title,
            Slug = id,
            Summary = summary,
            Description = description,
            Code = code,
            Category = category,
            CategoryLabel = categoryLabel,
            Badge = badge,
            DiscountType = discountType,
            DiscountValue = discountValue,
            MaximumDiscount = maximumDiscount,
            MinimumOrder = minimumOrder,
            DisplayValue = displayValue,
            StartDate = startDate.Date,
            EndDate = endDate?.Date,
            IsFeatured = isFeatured,
            IsOnlineOnly = isOnlineOnly,
            IsMemberOnly = isMemberOnly,
            IsStackable = false,
            Terms = terms
        };
    }

    private static OfferViewModel Enrich(OfferViewModel offer, DateTime today)
    {
        var status = GetStatus(offer, today.Date);
        var remainingLabel = GetRemainingLabel(offer, today.Date);
        var isExpiringSoon = status == "active"
            && offer.EndDate.HasValue
            && offer.EndDate.Value.Date <= today.Date.AddDays(7);

        return offer with
        {
            Status = status,
            StatusLabel = status switch
            {
                "expired" => "Đã hết hạn",
                "upcoming" => "Sắp diễn ra",
                _ => offer.Badge ?? "Đang áp dụng"
            },
            ValidityLabel = offer.EndDate.HasValue
                ? $"HSD: {offer.EndDate.Value:dd/MM/yyyy}"
                : "Không thời hạn",
            RemainingLabel = remainingLabel,
            IsExpiringSoon = isExpiringSoon
        };
    }

    private static string GetStatus(OfferViewModel offer, DateTime today)
    {
        if (offer.StartDate.Date > today)
        {
            return "upcoming";
        }

        if (offer.EndDate.HasValue && offer.EndDate.Value.Date < today)
        {
            return "expired";
        }

        return "active";
    }

    private static string GetRemainingLabel(OfferViewModel offer, DateTime today)
    {
        if (!offer.EndDate.HasValue)
        {
            return "Không thời hạn";
        }

        var remainingDays = (offer.EndDate.Value.Date - today).Days;

        if (remainingDays < 0)
        {
            return "Đã hết hạn";
        }

        if (remainingDays == 0)
        {
            return "Còn dưới 24 giờ";
        }

        return $"Còn {remainingDays:00} ngày";
    }
}
