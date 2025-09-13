-- Script để sửa hình ảnh sản phẩm quần tây trơn (ID = 3)
-- Chạy script này trong SQL Server Management Studio hoặc Azure Data Studio

-- Kiểm tra dữ liệu hiện tại của sản phẩm ID 3
SELECT 
    i.ItemsId,
    i.ItemsName,
    pv.ProductVariantsId,
    pv.Size,
    pv.Color,
    pv.Image
FROM Items i
LEFT JOIN ProductVariants pv ON i.ItemsId = pv.ProductId
WHERE i.ItemsId = 3;

-- Cập nhật hình ảnh cho các variant của quần tây trơn
-- Thay thế bằng tên file hình ảnh quần thực tế

-- Ví dụ: Cập nhật hình ảnh cho variant màu đen, size 28
UPDATE ProductVariants 
SET Image = '2ff56c39-b524-4721-9be4-4eecb5ff069e_Quan den sidetab 2 ly.jpg'
WHERE ProductId = 3 AND Color = 'Đen' AND Size = '28';

-- Cập nhật hình ảnh cho variant màu đen, size 30
UPDATE ProductVariants 
SET Image = '3a323df6-b762-40ac-9b18-74985814703a_Quan den sidetab 2 ly.jpg'
WHERE ProductId = 3 AND Color = 'Đen' AND Size = '30';

-- Cập nhật hình ảnh cho variant màu đen, size 32
UPDATE ProductVariants 
SET Image = '84f819c0-0896-4b09-9079-5e4e3e778acd_Quan den sidetab 2 ly.jpg'
WHERE ProductId = 3 AND Color = 'Đen' AND Size = '32';

-- Kiểm tra kết quả sau khi cập nhật
SELECT 
    i.ItemsId,
    i.ItemsName,
    pv.ProductVariantsId,
    pv.Size,
    pv.Color,
    pv.Image
FROM Items i
LEFT JOIN ProductVariants pv ON i.ItemsId = pv.ProductId
WHERE i.ItemsId = 3;
