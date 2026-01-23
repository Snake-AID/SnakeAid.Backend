# SnakeAid.Backend

Note:

## về booking consultation và expert time slot:

3 giai đoạn với 3 method service khác nhau:
1) lock time slot db và booking db với transaction và xmin (check concurrency)

````csharp
public async Task<string> CreateBookingOrder(BookingRequest request)
{
    // Bước này dùng Transaction của bạn
    var booking = await ExecuteInTransactionAsync(async () => {
        // IValidatableObject sẽ kiểm tra Slot này còn trống hay không ở đây
        var newBooking = new Booking {
            SlotId = request.SlotId,
            Status = BookingStatus.Pending,
            ExpirationTime = DateTime.UtcNow.AddMinutes(15) // Giữ chỗ trong 15p
        };
        Context.Bookings.Add(newBooking);
        return newBooking;
    });

    // Sau khi Transaction của DB đã đóng, mới gọi Gateway lấy Link thanh toán
    var paymentUrl = await _paymentService.GetPaymentUrl(booking.Id, booking.Amount);
    return paymentUrl;
}
````

2) giai đoạn thanh toán gateway hay trừ tiền (Momo/VnPay)

3) thay đổi status time slot và boooking với transaction và xmin (Webhook / IPN)
    Gateway sẽ gọi về một API Callback. Dùng ExecuteInTransactionAsync để cập nhật kết quả cuối cùng.

````csharp
[HttpPost("payment-callback")]
public async Task<IActionResult> HandleCallback([FromBody] PaymentResult result)
{
    await ExecuteInTransactionAsync(async () => {
        var booking = await Context.Bookings.FindAsync(result.OrderId);
        
        if (result.IsSuccess) {
            booking.Status = BookingStatus.Confirmed;
            // Thực hiện thêm logic khác như gửi Email, xuất hóa đơn...
        } else {
            booking.Status = BookingStatus.Cancelled;
            await RollbackAsync(); // Nếu cần thiết
        }
    });
    
    return Ok();
}
````