# 🔎 Root Cause Analysis – ScenarioData NULL on Mobile (IL2CPP)

## 1. Tổng quan vấn đề

Trên mobile build (IL2CPP), khi người dùng bấm **Start**, hệ thống crash với lỗi:

```
FATAL ERROR: Bien 'data' (ScenarioData) dang bi NULL tren Prefab!
```

Log trước đó cho thấy:

* `scenarioDataToAssign bị NULL`
* `Resources.Load thất bại`
* `The referenced script on this Behaviour is missing!`
* `m_Script: {fileID: 0}` trong file `TrafficSafe.asset`

Điều này xác nhận: **Unity không thể deserialize ScriptableObject ScenarioData trên mobile build.**

---

## 2. Chuỗi sự kiện chính xác

### Khi ARCitySpawner khởi tạo:

1. `scenarioDataToAssign == null`
2. Thử `Resources.Load<TrafficSafe>()`
3. Unity báo:

   ```
   The referenced script on this Behaviour is missing!
   ```
4. Load thất bại → data vẫn NULL

### Khi user bấm Start:

1. `controller.StartScenario()` được gọi
2. `data == null`
3. FATAL ERROR

---

## 3. Nguyên nhân gốc rễ

Mở file `TrafficSafe.asset` thấy:

```yaml
m_Script: {fileID: 0}
```

Điều này nghĩa là:

* Asset đã **mất reference tới class ScenarioData**
* Unity không biết script nào deserialize nó
* Trên mobile (IL2CPP), việc strip code làm vấn đề rõ ràng hơn
* Dù có thêm `[Preserve]`, asset vẫn serialize theo layout cũ
* Kết quả: ScriptableObject không thể load ở runtime

Quan trọng:

Đây không phải lỗi Resources.
Không phải lỗi Inspector.
Không phải lỗi Preserve.
Mà là **asset đã hỏng reference m_Script**.

---

## 4. Vì sao IL2CPP làm lỗi nghiêm trọng hơn?

IL2CPP + Code Stripping sẽ:

* Loại bỏ class không được reference trực tiếp
* Không giữ metadata reflection nếu không có link.xml
* Khi asset đã có m_Script lỗi → không có cách nào deserialize

Trên Editor có thể vẫn chạy do:

* Mono runtime linh hoạt hơn
* Không strip code

---

## 5. Kết luận kỹ thuật

ScriptableObject `ScenarioData` hiện tại:

* Không deserialize được
* Không load được từ Resources
* Không hoạt động trên mobile
* Asset YAML đã mất script reference
* Không thể sửa bằng Preserve

Đây là lỗi cấu trúc asset, không phải runtime logic.

---

## 6. Giải pháp triệt để đề xuất

### Phương án A – Hardcode data trong ScenarioController (nhanh nhất)

Nếu `data == null`:

* Tạo new ScenarioData bằng code
* Gán step list trực tiếp
* Không dùng AudioClip reference trước
* Chỉ cần đảm bảo visual actions chạy được

Ví dụ logic:

```csharp
if (data == null)
{
    data = new ScenarioData();
    data.steps = new List<ScenarioStep>()
    {
        new ScenarioStep
        {
            title = "Bước 1",
            description = "Quan sát đèn giao thông"
        }
    };
}
```

Ưu điểm:

* Không phụ thuộc asset
* Không bị strip
* Không phụ thuộc Resources

---

### Phương án B – Dùng JSON thay ScriptableObject

* Lưu dữ liệu dưới dạng JSON
* Load bằng TextAsset
* Deserialize bằng JsonUtility

Ưu điểm:

* Không phụ thuộc m_Script
* Không bị mất reference
* Hoạt động ổn định trên IL2CPP

---

### Phương án C – Recreate Asset hoàn toàn (rủi ro)

* Tạo lại class ScenarioData
* Tạo lại ScriptableObject mới
* Recreate asset từ đầu
* Đảm bảo script GUID không thay đổi

Nhưng nếu GUID đã thay đổi trước đó, khả năng vẫn lỗi.

---

## 7. Yêu cầu cho Claude

Hãy sửa hệ thống theo hướng:

1. Nếu `ScenarioData` load thất bại → fallback sang hardcoded data
2. Đảm bảo `StartScenario()` không bao giờ crash khi data null
3. Không sử dụng ScriptableObject cho runtime critical data
4. Cho phép build mobile IL2CPP hoạt động ổn định

---

## 8. Mục tiêu sau sửa

* Không còn phụ thuộc TrafficSafe.asset
* Không còn lỗi m_Script
* Không còn FATAL NULL data
* Scenario chạy được trên Android build

---

## 9. Kết luận cuối

Vấn đề không phải do logic controller.
Không phải do Resources.
Không phải do UI.

Mà là:

**ScriptableObject asset đã mất script reference (m_Script = 0), nên Unity không thể deserialize trên mobile.**

Giải pháp an toàn nhất hiện tại: bỏ ScriptableObject và chuyển sang hardcoded hoặc JSON.
