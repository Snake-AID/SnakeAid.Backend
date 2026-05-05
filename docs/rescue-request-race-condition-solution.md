# Checklist triển khai: giải pháp race-condition cho rescue request

**Mục tiêu:** ngăn operator dispatch/assign cùng một rescuer nhiều lần khi rescuer chưa accept.
**Phạm vi quick win:** chỉ sửa service logic + Hub fallback, chưa thêm migration.
**Định hướng phase sau:** hardening bằng `ExpiresAt` và cleanup event-driven.

---

## 1. Mục tiêu nghiệp vụ

- [ ] Không cho operator dispatch cùng một rescuer nhiều lần khi request trước đó vẫn đang pending.
- [ ] Không cho operator assign cùng một rescuer nhiều lần khi request trước đó vẫn đang pending.
- [ ] Khi rescuer đang bị điều phối hoặc assign, `IsOnline` vẫn phải giữ nguyên để phản ánh rescuer còn hoạt động trong hệ thống.
- [ ] Khi rescuer decline hoặc operator cancel, rescuer phải được trả về trạng thái `available`.
- [ ] Operator dashboard phải nhìn thấy trạng thái `busy` hoặc `available` của rescuer theo realtime.

---

## 2. Vấn đề hiện tại

- [ ] `IsAvailable` chưa được cập nhật ngay lúc tạo dispatch/assign request.
- [ ] `IsOnline` không nên bị dùng thay cho trạng thái busy, vì đây là trạng thái hoạt động trong hệ thống, không phải trạng thái sẵn sàng nhận request.
- [ ] UI operator chỉ thấy request đã gửi, nhưng chưa có tín hiệu rõ ràng rằng rescuer đã bị lock.
- [ ] Chưa có event riêng để báo rescuer “available again” sau khi được release.

---

## 3. Checklist phase 1: quick win, không migration

### 3.1 `SnakebiteIncidentService.DispatchIncidentAsync`

- [ ] Đổi điều kiện kiểm tra rescuer sang `!rescuer.IsOnline || !rescuer.IsAvailable`.
- [ ] Giữ nguyên logic kiểm tra rescuer tồn tại, chưa bị decline trước đó và đang on shift.
- [ ] Chỉ set `rescuer.IsAvailable = false`, không chạm vào `IsOnline`.
- [ ] Sau khi insert `RescuerRequest`, set `rescuer.IsAvailable = false`.
- [ ] Update `RescuerProfile` trong cùng transaction.
- [ ] Đảm bảo rescuer vừa được dispatch sẽ không còn xuất hiện như một lựa chọn khả dụng cho request khác.

### 3.2 `SnakebiteIncidentService.DeclineDispatchRequestAsync`

- [ ] Sau khi set request sang `Declined`, load lại `RescuerProfile` theo `request.RescuerId`.
- [ ] Set `rescuer.IsAvailable = true` khi decline thành công.
- [ ] Giữ nguyên `IsOnline`, không bật/tắt lại trạng thái hoạt động của rescuer.
- [ ] Update `RescuerProfile` trong transaction.
- [ ] Giữ nguyên các notification hiện có cho rescuer và operator.

### 3.3 `SnakebiteIncidentService.OperatorCancelDispatchRequestAsync`

- [ ] Sau khi set request sang `Cancelled`, load lại `RescuerProfile` theo `request.RescuerId`.
- [ ] Set `rescuer.IsAvailable = true` khi operator hủy request pending.
- [ ] Giữ nguyên `IsOnline`, không dùng cancel để làm rescuer offline.
- [ ] Update `RescuerProfile` trong transaction.
- [ ] Giữ nguyên notification cancel cho rescuer và operator.

### 3.4 `SnakeCatchingRequestService.AssignSnakeCatchingRequestAsync`

- [ ] Đổi điều kiện kiểm tra rescuer sang `!existingAccount.RescuerProfile.IsOnline || !existingAccount.RescuerProfile.IsAvailable`.
- [ ] Giữ nguyên các kiểm tra request status, assigned rescuer và active mission.
- [ ] Chỉ set `rescuerProfile.IsAvailable = false`, không đổi `IsOnline`.
- [ ] Sau khi insert `SnakeCatchingMission`, set `rescuerProfile.IsAvailable = false`.
- [ ] Update `RescuerProfile` trong transaction.
- [ ] Đảm bảo một rescuer không bị assign trùng khi mission mới vừa được tạo.

### 3.5 `RescuerHub.SetRescuerAvailable`

- [ ] Thêm public method `SetRescuerAvailable(string userId)` trong `RescuerHub`.
- [ ] Xác thực caller đúng là rescuer của `userId` đó.
- [ ] Load `RescuerProfile` và set `IsAvailable = true`.
- [ ] Không đổi `IsOnline`, vì rescuer vẫn có thể đang hoạt động trong hệ thống.
- [ ] Commit thay đổi sau khi cập nhật availability.
- [ ] Broadcast event `RescuerAvailableAgain` tới `OperatorGroup`.
- [ ] Gửi phản hồi xác nhận về cho rescuer caller.

### 3.6 Event realtime cho operator

- [ ] Dùng `RescuerOnlineStatus` cho trạng thái join và disconnect như hiện tại.
- [ ] Bổ sung event `RescuerAvailableAgain` khi rescuer được release.
- [ ] Nếu cần nhất quán hơn, có thể cân nhắc một event trạng thái chung ở phase sau, nhưng không bắt buộc cho quick win.

---

## 4. Checklist UI operator

- [ ] Trạng thái `IsOnline = false` phải được hiển thị là rescuer offline, không chọn được.
- [ ] Trạng thái `IsOnline = true && IsAvailable = false` phải được hiển thị là rescuer đang bận hoặc bị lock.
- [ ] Trạng thái `IsOnline = true && IsAvailable = true` phải được hiển thị là rescuer sẵn sàng nhận request.
- [ ] UI phải cập nhật từ realtime event, không suy luận trạng thái chỉ dựa trên request lifecycle.

---

## 5. Checklist phase 2: hardening sau quick win

### 5.1 Thêm metadata hết hạn

- [ ] Thêm field `ExpiresAt` cho `RescuerRequest`.
- [ ] Thêm field `ExpiresAt` cho `SnakeCatchingMission`.
- [ ] Dùng `ExpiresAt` để biết request nào đã quá hạn.

### 5.2 Cơ chế cleanup event-driven

- [ ] Tạo cleanup chạy theo timer, không polling liên tục.
- [ ] Khi có pending request gần hết hạn, set timer theo `ExpiresAt`.
- [ ] Khi timer fire, release rescuer và set lại `IsAvailable = true`.
- [ ] Không đổi `IsOnline` khi release, vì đây không phải trạng thái online/offline.
- [ ] Broadcast `RescuerAvailableAgain` sau khi release.
- [ ] Reschedule timer cho pending request tiếp theo nếu còn.

### 5.3 Nâng cấp filter operator

- [ ] Khi lấy danh sách rescuer cho dispatch, lọc theo `online = true` và `available = true`.
- [ ] Loại bỏ rescuer đang có pending request chưa hết hạn.
- [ ] Chỉ đưa rescuer thật sự sẵn sàng vào danh sách điều phối.

### 5.4 Chuẩn hóa event trạng thái

- [ ] Nếu sau này cần đơn giản hóa client, cân nhắc thêm event chung như `RescuerStateChanged`.
- [ ] Payload đề xuất gồm `RescuerId`, `IsOnline`, `IsAvailable`, `UpdatedAt`.
- [ ] Chỉ làm bước này sau khi phase 1 đã ổn định.

---

## 6. Thứ tự triển khai đề xuất

- [ ] Bước 1: sửa `DispatchIncidentAsync`.
- [ ] Bước 2: sửa `AssignSnakeCatchingRequestAsync`.
- [ ] Bước 3: sửa `DeclineDispatchRequestAsync` và `OperatorCancelDispatchRequestAsync`.
- [ ] Bước 4: thêm `RescuerHub.SetRescuerAvailable`.
- [ ] Bước 5: đồng bộ event realtime cho operator.
- [ ] Bước 6: build và test luồng realtime.
- [ ] Bước 7: sau khi ổn định mới làm `ExpiresAt` và cleanup service.

---

## 7. Tiêu chí nghiệm thu

### Phase 1 hoàn thành khi

- [ ] Một rescuer không thể nhận 2 dispatch hoặc assign song song.
- [ ] Decline hoặc cancel sẽ trả rescuer về available.
- [ ] `IsOnline` vẫn giữ nguyên trong suốt vòng đời dispatch/assign, chỉ `IsAvailable` đổi trạng thái busy/rảnh.
- [ ] Operator nhận được event để cập nhật map/dashboard realtime.
- [ ] Build thành công sau khi đổi logic.

### Phase 2 hoàn thành khi

- [ ] Request có thời hạn hết hạn rõ ràng.
- [ ] Backend tự release offer khi quá hạn.
- [ ] UI operator luôn đồng bộ với trạng thái rescuer.

---

## 8. Ghi chú cho phase sau

- [ ] Không thêm field vào `RescuerProfile` trong quick win.
- [ ] `IsOnline` dùng để phản ánh rescuer đang hoạt động trong hệ thống, không dùng để biểu diễn busy/available.
- [ ] Giữ `RescuerAvailableAgain` như event cầu nối để UI cập nhật nhanh.
- [ ] Chỉ bắt đầu phần `ExpiresAt` và cleanup service sau khi phase 1 đã được build và verify trên UI.
