using UnityEngine;
using UnityEngine.Scripting;

namespace Assets.MobileARTemplateAssets.Scripts
{
    [Preserve]
    public static class TransformExtensions
    {
        // Hàm mở rộng để tìm con ở mọi cấp độ
        public static Transform FindDeepChild(this Transform parent, string name)
        {
            // Kiểm tra xem có phải là con trực tiếp không
            Transform result = parent.Find(name);
            if (result != null) return result;

            // Nếu không thấy, duyệt qua từng con để tìm sâu hơn
            foreach (Transform child in parent)
            {
                result = child.FindDeepChild(name);
                if (result != null) return result;
            }

            return null;
        }
    }
}
