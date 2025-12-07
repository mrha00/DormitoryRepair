/**
 * 图片压缩工具
 * 使用Canvas进行前端图片压缩，减少服务器压力
 * 
 * @param {File} file - 原始图片文件
 * @param {number} maxSizeMB - 压缩后的最大大小（MB），默认0.2MB（200KB）
 * @returns {Promise<Blob>} 压缩后的图片Blob对象
 */
export function compressImage(file, maxSizeMB = 0.2) {
  return new Promise((resolve, reject) => {
    // 验证文件类型
    if (!file.type.startsWith('image/')) {
      reject(new Error('请选择图片文件'));
      return;
    }

    const reader = new FileReader();
    
    reader.onload = (e) => {
      const img = new Image();
      
      img.onload = () => {
        const canvas = document.createElement('canvas');
        const ctx = canvas.getContext('2d');
        
        // 计算压缩比例
        let width = img.width;
        let height = img.height;
        const maxDimension = 1200; // 最大宽高限制
        
        // 如果图片尺寸超过限制，等比例缩放
        if (width > maxDimension || height > maxDimension) {
          const ratio = Math.min(maxDimension / width, maxDimension / height);
          width *= ratio;
          height *= ratio;
        }
        
        canvas.width = width;
        canvas.height = height;
        
        // 绘制图片到Canvas
        ctx.drawImage(img, 0, 0, width, height);
        
        // 转换为Blob，质量0.7（70%）
        canvas.toBlob(
          (blob) => {
            if (!blob) {
              reject(new Error('图片压缩失败'));
              return;
            }

            // 验证压缩后大小
            const sizeMB = blob.size / (1024 * 1024);
            if (sizeMB > maxSizeMB) {
              reject(new Error(`压缩后图片大小为${(sizeMB * 1024).toFixed(2)}KB，仍超过${maxSizeMB * 1024}KB限制`));
            } else {
              console.log(`✅ 图片压缩成功：${(file.size / 1024).toFixed(2)}KB → ${(blob.size / 1024).toFixed(2)}KB`);
              resolve(blob);
            }
          },
          'image/jpeg',
          0.7 // JPEG质量参数，范围0-1
        );
      };
      
      img.onerror = () => {
        reject(new Error('图片加载失败'));
      };
      
      img.src = e.target.result;
    };
    
    reader.onerror = () => {
      reject(new Error('文件读取失败'));
    };
    
    reader.readAsDataURL(file);
  });
}
